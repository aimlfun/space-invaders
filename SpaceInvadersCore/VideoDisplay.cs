using SpaceInvadersCore.Game;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace SpaceInvadersCore;

/// <summary>
/// Class for rendering Space Invaders.
/// 
/// With most of my fun apps, I modify a bitmap using the Graphics class. This is usually performant, despite being a wrapper around GDI+.
/// However, this app even in "quiet" mode requires a video display unless I resort to some really messy logic... It was all fine until I
/// added the "shields"... The ship destroys 1x4 pixels, and the Space Invaders destroy a splat pattern. To mimic that I would need an array
/// per shield, and complex logic to enable individual parts of it to be removed. In the interest of diminishing returns, I simply made a custom
/// video display class that is reasonably fast. 
/// 
/// It is rendering 55 aliens + 4 bullets + 1 player ship x 100 games each frame!
/// 
/// PLEASE NOTE: 
/// It is using "unsafe" and manipulating memory directly. This is not for the faint of heart.
/// I intentionally have not performed bounds checking of x/y, because each check is a performance hit.
/// 
/// For RADAR to work, it has to set the ALPHA channel. Alas that impacts the performance slightly of non RADAR mode.
/// </summary>
public class VideoDisplay
{
    //  █   █    ███    ████    █████    ███            ████     ███     ███    ████    █         █     █   █
    //  █   █     █     █   █   █       █   █           █   █     █     █   █   █   █   █        █ █    █   █
    //  █   █     █     █   █   █       █   █           █   █     █     █       █   █   █       █   █    █ █
    //  █   █     █     █   █   ████    █   █           █   █     █      ███    ████    █       █   █     █
    //  █   █     █     █   █   █       █   █           █   █     █         █   █       █       █████     █
    //   █ █      █     █   █   █       █   █           █   █     █     █   █   █       █       █   █     █
    //    █      ███    ████    █████    ███            ████     ███     ███    █       █████   █   █     █

    #region DEBUGGING
    // W A R N I N G :  DO NOT LEAVE THIS SET TO TRUE, IT WILL SLOW THE GAME DOWN HUGELY, AND FILL YOUR DISK WITH FRAME BY FRAME IMAGES!
    /// <summary>
    /// When true, it will save the bitmap to disk.
    /// </summary>
    private static readonly bool c_debugDrawEveryFrameAsAnImage = false;

    /// <summary>
    /// If debugging is on it, it will output every frame sequentially to this file.
    /// Note the path below is for my machine, you will need to change it. c:\temp\frames\ 
    /// </summary>
    private const string c_debuggingFileName = @"c:\temp\frames\SpaceInvader-video-debug-frame-{{debuggingFrameNumberForFileName}}.png";

    /// <summary>
    /// Used to number the debugging frames images sequentially.
    /// </summary>
    private int debuggingFrameNumberForFileName = 1;

    /// <summary>
    /// When true, it will draw a box around each sprite.
    /// </summary>
    private static readonly bool c_drawBoxesAroundSprites = false;

    /// <summary>
    /// This is the brush used to draw a pixel overlay on top of the video display. It indicates what chunky pixels the AI sees.
    /// </summary>
    private static readonly SolidBrush brushForOverlay = new(Color.FromArgb(220, 255, 100, 100));
    #endregion

    #region CONSTANTS - DO NOT CHANGE THESE VALUES!
    /// <summary>
    /// Pixels are 32bit ARGB, stored BGRA in terms of offset.
    /// This is the offset for BLUE.
    /// </summary>
    private const int c_offsetBlueChannel = 0;

    /// <summary>
    /// Pixels are 32bit ARGB, stored BGRA in terms of offset.
    /// This is the offset for GREEN.
    /// </summary>
    private const int c_offsetGreenChannel = 1;

    /// <summary>
    /// Pixels are 32bit ARGB, stored BGRA in terms of offset.
    /// This is the offset for RED.
    /// </summary>
    private const int c_offsetRedChannel = 2;

    /// <summary>
    /// Pixels are 32bit ARGB, stored BGRA in terms of offset.
    /// This is the offset for ALPHA.
    /// </summary>
    private const int c_offsetAlphaChannel = 3;

    /// <summary>
    /// This is the width of the bitmap in pixels, matching the original Space Invaders game.
    /// </summary>
    internal const int c_widthOfBitmapInPX = 224;

    /// <summary>
    /// This is the height of the bitmap in pixel, matching the original Space Invaders game.
    /// </summary>
    internal const int c_heightOfBitmapInPX = 256;

    /// <summary>
    /// This is how many bytes each pixel occupies in the bitmap.
    /// DO NOT CHANGE THIS! ALL THE FAST DRAWING METHODS EXPECT ARGB (4 BYTE) PIXELS.
    /// </summary>
    private const int c_bytesPerPixel = 4; // PixelFormat.Format32bppArgb = Bitmap.GetPixelFormatSize(srcBitMapData.PixelFormat) / 8;

    /// <summary>
    /// This is how many bytes the bitmap is. 
    /// </summary>
    private const int c_totalSizeOfBitmapInBytes = 229376; // = Math.Abs( (Bitmap).Stride ) * height; = 256 lines of 896 bytes each = 256 lines x 224 pixels x 4 bytes per pixel = 229376 bytes

    /// <summary>
    /// This is how many bytes per row of the bitmap image (used to multiply "y" by to get to the correct data).
    /// </summary>
    private const int c_offsetToMoveToNextRasterLine = c_widthOfBitmapInPX * 4; // (Bitmap).Stride; = 224 x 4 bytes per pixel = 896
    #endregion

    /// <summary>
    /// LockBits requires a rectangle to lock. We use this one for the entire bitmap.
    /// </summary>
    private static Rectangle s_videoScreenRectangle = new(0, 0, c_widthOfBitmapInPX, c_heightOfBitmapInPX);

    /// <summary>
    /// This is the pixels in the bitmap.
    /// Back buffer size is equal to the number of pixels to draw on screen: Width x Height x * 4 (4=R,G,B & Alpha values). 
    /// </summary>
    private readonly byte[] BackBuffer;

    /// <summary>
    /// Attempt to warn developers that they have debugging turned on, and turn it off if not being debugged.
    /// </summary>
    static VideoDisplay()
    {
        // is the debug on?
        if (!c_debugDrawEveryFrameAsAnImage) return;

        if (!Debugger.IsAttached) // no debugger, we need to turn it off for performance, and to avoid filling the disk.
        {
            c_debugDrawEveryFrameAsAnImage = false;
            c_drawBoxesAroundSprites = false;
        }
        else
        {
            Debug.WriteLine("WARNING: DEBUG IS TURNED ON. THIS WILL WRITE FRAME BY FRAME TO DISK, AND FILL IT.");
            Debug.WriteLine($"Debug frame-by-frame will be written to \"{c_debuggingFileName}\".");
            Debugger.Break(); // last chance saloon.
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public VideoDisplay()
    {
        // we create what is known as a back-buffer, where we draw all the pixels. 
        BackBuffer = new byte[c_totalSizeOfBitmapInBytes];
    }

    /// <summary>
    /// Returns the game video screen (a back-buffer of byte[]) rendered as a bitmap.
    /// </summary>
    public Bitmap GetVideoDisplayContent()
    {
        // we create a bitmap to hold the pixels. We could simply return the back buffer, but at some point the caller would need to render it.
        Bitmap bitmapBeingModifiedDirectly = new(c_widthOfBitmapInPX, c_heightOfBitmapInPX, PixelFormat.Format32bppArgb);

        // lock the region of the bitmap we want to modify
        BitmapData bitmapBeingModifiedDirectlyData = bitmapBeingModifiedDirectly.LockBits(s_videoScreenRectangle, ImageLockMode.ReadWrite, bitmapBeingModifiedDirectly.PixelFormat);

        // copy the back-buffer array containing our video screen into the Bitmap
        System.Runtime.InteropServices.Marshal.Copy(BackBuffer, 0, bitmapBeingModifiedDirectlyData.Scan0, c_totalSizeOfBitmapInBytes);

        // unlock, now we are finished writing
        bitmapBeingModifiedDirectly.UnlockBits(bitmapBeingModifiedDirectlyData);

        // frame by frame recording
        if (c_debugDrawEveryFrameAsAnImage) bitmapBeingModifiedDirectly.Save(c_debuggingFileName.Replace("{{debuggingFrameNumberForFileName}}", (debuggingFrameNumberForFileName++).ToString().PadLeft(10, '0')), ImageFormat.Png);

        return bitmapBeingModifiedDirectly;
    }

    /// <summary>
    /// This method clears the back buffer with a specific colour.
    /// </summary>
    /// <param name="colour"></param>
    public void ClearDisplay(Color colour)
    {
        for (var index = 0; index < BackBuffer.Length; index += c_bytesPerPixel)
        {
            BackBuffer[index /*+ c_offsetBlueChannel*/] = colour.B;  // blue offset is always 0, so we don't need to include it +0.
            BackBuffer[index + c_offsetGreenChannel] = colour.G;
            BackBuffer[index + c_offsetRedChannel] = colour.R;
            BackBuffer[index + c_offsetAlphaChannel] = 255;
        }
    }

    /// <summary>
    /// Draws a rectangle on the back buffer, in the specified colour.
    /// </summary>
    /// <param name="colour"></param>
    /// <param name="rectangle"></param>
    public void DrawRectangle(Color colour, Rectangle rectangle)
    {
        int left = rectangle.Left * c_bytesPerPixel;
        int right = (rectangle.Right - 1) * c_bytesPerPixel;
        int top = rectangle.Top * c_offsetToMoveToNextRasterLine;
        int bottom = (rectangle.Bottom - 1) * c_offsetToMoveToNextRasterLine;

        // horizontal edges
        for (int x = rectangle.Left; x < rectangle.Right; x++)
        {
            int index1 = x * c_bytesPerPixel + top; // find first pixel in the row

            BackBuffer[index1 /*+ c_offsetBlueChannel*/] = colour.B;  // blue offset is always 0, so we don't need to include it +0.
            BackBuffer[index1 + c_offsetGreenChannel] = colour.G;
            BackBuffer[index1 + c_offsetRedChannel] = colour.R;
            BackBuffer[index1 + c_offsetAlphaChannel] = colour.A; // alpha is always 255, set by .ClearDisplay()
            index1 = x * c_bytesPerPixel + bottom; // find first pixel in the row

            BackBuffer[index1 /*+ c_offsetBlueChannel*/] = colour.B; // blue offset is always 0, so we don't need to include it +0.
            BackBuffer[index1 + c_offsetGreenChannel] = colour.G;
            BackBuffer[index1 + c_offsetRedChannel] = colour.R;
            BackBuffer[index1 + c_offsetAlphaChannel] = colour.A; // alpha is always 255, set by .ClearDisplay()
        }

        // vertical edges
        for (int y = rectangle.Top; y < rectangle.Bottom; y++)
        {
            int index4 = left + y * c_offsetToMoveToNextRasterLine; // find first pixel in the row

            BackBuffer[index4 /*+ c_offsetBlueChannel*/] = colour.B; // blue offset is always 0, so we don't need to include it +0.
            BackBuffer[index4 + c_offsetGreenChannel] = colour.G;
            BackBuffer[index4 + c_offsetRedChannel] = colour.R;
            BackBuffer[index4 + c_offsetAlphaChannel] = colour.A; // alpha is always 255, set by .ClearDisplay()

            index4 = right + y * c_offsetToMoveToNextRasterLine; // find first pixel in the row

            BackBuffer[index4 /*+ c_offsetBlueChannel*/] = colour.B; // blue offset is always 0, so we don't need to include it +0.
            BackBuffer[index4 + c_offsetGreenChannel] = colour.G;
            BackBuffer[index4 + c_offsetRedChannel] = colour.R;
            BackBuffer[index4 + c_offsetAlphaChannel] = colour.A; // alpha is always 255, set by .ClearDisplay()
        }
    }

    /// <summary>
    /// Fills a rectangle on the back buffer, in the specified colour.
    /// </summary>
    /// <param name="colour"></param>
    /// <param name="rectangle"></param>
    public unsafe void FillRectangle(Color colour, Rectangle rectangle)
    {
        fixed (byte* pBackBuffer = BackBuffer)
        {
            byte* pBuffer = pBackBuffer + rectangle.Top * c_offsetToMoveToNextRasterLine + rectangle.Left * c_bytesPerPixel;

            // for each raster line
            for (int y = rectangle.Top; y < rectangle.Bottom; y++)
            {
                byte* pPixel = pBuffer;

                // for each pixel in the raster line
                for (int x = rectangle.Left; x < rectangle.Right; x++)
                {
                    *(pPixel /*+ c_offsetBlueChannel*/) = colour.B;  // blue offset is always 0, so we don't need to include it +0.
                    *(pPixel + c_offsetGreenChannel) = colour.G;
                    *(pPixel + c_offsetRedChannel) = colour.R;
                    *(pPixel + c_offsetAlphaChannel) = 255; // colour.A; // alpha is always 255, set by .ClearDisplay()
                    pPixel += c_bytesPerPixel; // move to the next pixel
                }

                pBuffer += c_offsetToMoveToNextRasterLine; // move to the next row
            }
        }
    }

    /// <summary>
    /// Pixel colour is based on a plastic films, not because they had coloured pixels which is why bullets change colour as they get lower!
    /// See https://tobiasvl.github.io/blog/space-invaders/ for a picture of what it looks like.
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    private static Color ColourBasedOnFilm(int y)
    {
        //  +----------------------------------------------------------+ _ 8
        //  | SCORE<1>               HIGH-SCORE               SCORE<2> | _ 16
        //  |                                                         .| _ 24
        //  |   0010                    1000                           |
        //  |..........................................................| } y=34
        //  |.<ooo>....................................................| } red transparent film 
        //  |..........................................................| } y=55
        //  | /o\ /o\ /o\ /o\ /o\ /o\ /o\ /o\ /o\ /o\ /o\              |
        //  |                                                          |
        //  | \#/ \#/ \#/ \#/ \#/ \#/ \#/ \#/ \#/ \#/ \#/              |
        //  |                                                          |
        //  | \#/ \#/ \#/ \#/ \#/ \#/ \#/ \#/ \#/ \#/ \#/              |
        //  |                                                          |
        //  | {o} {o} {o} {o} {o} {o} {o} {o} {o} {o} {o}              | 
        //  |                                                          |
        //  | {o} {o} {o} {o} {o} {o} {o} {o} {o} {o} {o}              |
        //  |                                                          |
        //  |                                                          |
        //  |.....##.............##..............##.............##.....| } y=c_greenLineIndicatingFloorPX-60
        //  |....####...........####............####...........####....| }
        //  |... #..#...........#..#............#..#...........#..#....| }
        //  |..........................................................| } green transparent film
        //  |..<#|#>...................................................| }
        //  |..........................................................| }
        //  |----------------------------------------------------------| }
        //  |  3 .<#|#>.<#|#>................               CREDITS 00 | } also green film indicated by dots (level + credits is white)
        //  +----------------------------------------------------------+
        //       |<----- green film -------->|   this is so the extra lives are drawn green
        //       16                          136

        if (y > 33 && y < 56) return Color.FromArgb(255, 255, 25, 0); // magenta film.
        if (y > OriginalDataFrom1978.c_greenLineIndicatingFloorPX - 60 && y < 242) return OriginalDataFrom1978.s_playerColour; // green film.

        return Color.White; // non coloured
    }

    /// <summary>
    /// Draws a sprite on the screen at the specified location.
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public unsafe void DrawSprite(Sprite sprite, int x, int y)
    {
        // debugging? Show a box around the sprite (before drawing it)
        if (c_drawBoxesAroundSprites) DrawRectangle(Color.Blue, new Rectangle(x, y, sprite.WidthInPX, sprite.HeightInPX));

        int bufferOffset = c_offsetToMoveToNextRasterLine * y + x * c_bytesPerPixel;

        fixed (byte* pBuffer = BackBuffer)
        {
            for (int yOffset = 0; yOffset < sprite.HeightInPX; yOffset++)
            {
                byte* yRaster = pBuffer + bufferOffset + yOffset * c_offsetToMoveToNextRasterLine;

                Color colour = ColourBasedOnFilm(y + yOffset);

                for (int xOffset = 0; xOffset < sprite.WidthInPX; xOffset++)
                {
                    if (sprite.Pixels[xOffset, yOffset] != 0)
                    {
                        Color thisPixel;

                        // there is a film for the extra lives, this "DrawSprite" is used to also draw the extra lives, so we need to ensure the extra lives are drawn in the correct colour.
                        if (yOffset + y > OriginalDataFrom1978.c_greenLineIndicatingFloorPX && x > 16 && x < 136) thisPixel = OriginalDataFrom1978.s_playerColour; else thisPixel = colour;

                        byte* pPixel = yRaster + xOffset * c_bytesPerPixel;

                        *(pPixel /*+ c_offsetBlueChannel*/) = thisPixel.B;  // blue offset is always 0, so we don't need to include it +0.
                        *(pPixel + c_offsetGreenChannel) = thisPixel.G;
                        *(pPixel + c_offsetRedChannel) = thisPixel.R;
                        *(pPixel + c_offsetAlphaChannel) = 255; // thisPixel.A; // alpha is always 255, set by .ClearDisplay()
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draws a sprite on the screen at the specified location.
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public unsafe void DrawShield(Sprite sprite, int x, int y)
    {
        // debugging? Show a box around the sprite (before drawing it)
        if (c_drawBoxesAroundSprites) DrawRectangle(Color.Blue, new Rectangle(x, y, sprite.WidthInPX, sprite.HeightInPX));

        int bufferOffset = c_offsetToMoveToNextRasterLine * y + x * c_bytesPerPixel;

        fixed (byte* pBuffer = BackBuffer)
        {
            for (int yOffset = 0; yOffset < sprite.HeightInPX; yOffset++)
            {
                byte* yRaster = pBuffer + bufferOffset + yOffset * c_offsetToMoveToNextRasterLine;

                Color colour = ColourBasedOnFilm(y + yOffset);

                for (int xOffset = 0; xOffset < sprite.WidthInPX; xOffset++)
                {
                    if (sprite.Pixels[xOffset, yOffset] != 0)
                    {
                        Color thisPixel;

                        // there is a film for the extra lives, this "DrawSprite" is used to also draw the extra lives, so we need to ensure the extra lives are drawn in the correct colour.
                        if (yOffset + y > OriginalDataFrom1978.c_greenLineIndicatingFloorPX && x > 16 && x < 136) thisPixel = OriginalDataFrom1978.s_playerColour; else thisPixel = colour;

                        byte* pPixel = yRaster + xOffset * c_bytesPerPixel;

                        *(pPixel /*+ c_offsetBlueChannel*/) = thisPixel.B;  // blue offset is always 0, so we don't need to include it +0.
                        *(pPixel + c_offsetGreenChannel) = thisPixel.G;
                        *(pPixel + c_offsetRedChannel) = thisPixel.R;

                        *(pPixel + c_offsetAlphaChannel) = 252; // we need to detect
                    }
                }
            }
        }
    }
    /// <summary>
    /// Draws the sprite to our back buffer, whilst checking for collisions before setting the pixel.
    /// i.e. it only checks for collision on the pixels we draw in colour, not the black (transparent) pixels.
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public unsafe bool DrawSpriteWithCollisionDetection(Sprite sprite, int x, int y)
    {
        bool hit = false;

        // debugging? Show a box around the sprite (before drawing it). To avoid breaking the collision detection, we draw the box in blue that it cannot detect.
        if (c_drawBoxesAroundSprites) DrawRectangle(Color.Blue, new Rectangle(x, y, sprite.WidthInPX, sprite.HeightInPX));

        int bufferOffset = c_offsetToMoveToNextRasterLine * y + x * c_bytesPerPixel; // top left corner of the sprite in the back buffer

        fixed (byte* pBuffer = BackBuffer) // fixed to prevent the garbage collector from moving the memory around
        {
            for (int yOffset = 0; yOffset < sprite.HeightInPX; yOffset++) // for each raster line in the sprite
            {
                byte* yRaster = pBuffer + bufferOffset;

                Color colour = ColourBasedOnFilm(y + yOffset);

                for (int xOffset = 0; xOffset < sprite.WidthInPX; xOffset++)
                {
                    if (sprite.Pixels[xOffset, yOffset] != 0) // it's wasteful as pixels need a bit, not byte
                    {
                        byte* pPixel = yRaster + xOffset * c_bytesPerPixel;

                        // hit detection is on the colour pixels, not the transparent pixels.
                        // why "hit ||", because if a pixel already hit, we don't need to check 3 bytes that won't change the outcome (slower)
                        hit |= hit || *(pPixel + c_offsetGreenChannel) != 0 || *(pPixel + c_offsetRedChannel) != 0; // any pixel is non zero. We don't check alpha, as it's always 255. Nothing is blue, except debug, so we don't check blue.

                        *(pPixel /*+ c_offsetBlueChannel*/) = colour.B; // blue offset is always 0, so we don't need to include it +0.
                        *(pPixel + c_offsetGreenChannel) = colour.G;
                        *(pPixel + c_offsetRedChannel) = colour.R;
                        *(pPixel + c_offsetAlphaChannel) = 255; // colour.A; // alpha is always 255, set by .ClearDisplay() - this saves updating 1 byte per pixel (performance)
                    }
                }

                bufferOffset += c_offsetToMoveToNextRasterLine;
            }
        }

        return hit;
    }

    /// <summary>
    /// Erases a sprite on the screen at the specified location.
    /// Important note: we are removing pixels the sprite draws, not the black pixels (transparent) pixels.
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public unsafe void EraseSprite(Sprite sprite, int x, int y)
    {
        int bufferOffset = c_offsetToMoveToNextRasterLine * y + x * c_bytesPerPixel;

        fixed (byte* pBuffer = BackBuffer)
        {
            for (int yOffset = 0; yOffset < sprite.HeightInPX; yOffset++)
            {
                byte* yRaster = pBuffer + bufferOffset;

                for (int xOffset = 0; xOffset < sprite.WidthInPX; xOffset++)
                {
                    if (sprite.Pixels[xOffset, yOffset] != 0)
                    {
                        byte* pPixel = yRaster + xOffset * c_bytesPerPixel;

                        *(pPixel /*+ c_offsetBlueChannel*/) = 0;  // blue offset is always 0, so we don't need to include it +0.
                        *(pPixel + c_offsetGreenChannel) = 0;
                        *(pPixel + c_offsetRedChannel) = 0;
                        *(pPixel + c_offsetAlphaChannel) = 255;
                    }
                }

                bufferOffset += c_offsetToMoveToNextRasterLine;
            }
        }

        // after erasing, if debugging, draw a box around the sprite (after erasing it). To avoid breaking the collision detection, we draw the box in blue that it cannot subsequently detect.
        if (c_drawBoxesAroundSprites) DrawRectangle(Color.Blue, new Rectangle(x, y, sprite.WidthInPX, sprite.HeightInPX));
    }

    /// <summary>
    /// Draws a string to the screen at the specified location. This is not a general purpose string renderer, it is specific to the 
    /// Space Invaders 1978 font, and will only work with the characters for which it provided the font definition for.
    /// If you try to draw a character that is not in the font definition, it *will* throw an exception.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="point"></param>
    public void DrawString(string text, Point point)
    {
        int x = point.X;
        int y = point.Y;

        // for each character in text, look up in SpaceInvaders1978OriginalSprites.Sprites and render
        // using DrawSprite() moving the required width of the character/sprite before placing next character

        foreach (char c in text)
        {
            Sprite s = OriginalSpritesFrom1978.Sprites[c.ToString()];

            DrawSprite(s, x, y);

            x += s.WidthInPX;
        }
    }

    /// <summary>
    /// Draws the green horizontal line at the bottom of the screen, under the player ship.
    /// </summary>
    /// <param name="colour"></param>
    /// <param name="y"></param>
    public unsafe void DrawGreenHorizontalBaseLine(Color colour, int y)
    {
        int bufferOffset = y * c_offsetToMoveToNextRasterLine;

        fixed (byte* pBuffer = BackBuffer)
        {
            byte* pPixel = pBuffer + bufferOffset;

            for (int x = 0; x < c_widthOfBitmapInPX; x++)
            {
                *(pPixel /*+ c_offsetBlueChannel*/) = colour.B;  // blue offset is always 0, so we don't need to include it +0.
                *(pPixel + c_offsetGreenChannel) = colour.G;
                *(pPixel + c_offsetRedChannel) = colour.R;
                *(pPixel + c_offsetAlphaChannel) = 255;
                pPixel += c_bytesPerPixel;
            }
        }
    }

    /// <summary>
    /// Draws a full height vertical line at the specified location, for testing purposes.
    /// Working out our shrunk image is correct is a lot easier with vertical lines...
    /// </summary>
    /// <param name="colour"></param>
    /// <param name="x"></param>
    public unsafe void DrawVerticalLine(Color colour, int x)
    {
        int bufferOffset = x * c_bytesPerPixel;

        fixed (byte* pBuffer = BackBuffer)
        {
            byte* pPixel = pBuffer + bufferOffset;

            for (int y = 0; y < c_heightOfBitmapInPX; y++)
            {
                *(pPixel /*+ c_offsetBlueChannel*/) = colour.B;  // blue offset is always 0, so we don't need to include it +0.
                *(pPixel + c_offsetGreenChannel) = colour.G;
                *(pPixel + c_offsetRedChannel) = colour.R;
                *(pPixel + c_offsetAlphaChannel) = colour.A; // alpha is always 255, set by .ClearDisplay() - this saves updating 1 byte per pixel (performance)

                pPixel += c_offsetToMoveToNextRasterLine;
            }
        }
    }

    /// <summary>
    /// If you pick the AI mode where the source is the video, this method will return a shrunk version of the video.
    /// Shrinking is a necessary evil, reducing the size by 4x means less neurons required and quicker training.
    /// i.e. output is 56x64, not 224x256
    /// </summary>
    /// <returns></returns>
    public unsafe double[] VideoShrunkForAI()
    {
        // quarter the size of the video for the AI
        // 224 x 256 -> 56 x 64
        double[] AIscreen = new double[56 * 64]; // AI needs doubles, not bytes

        // we compute these OUTSIDE the loop , as they're constant
        const int twoPX = c_bytesPerPixel + c_bytesPerPixel;
        const int threePX = twoPX + c_bytesPerPixel;
        const int fourPX = threePX + c_bytesPerPixel;

        const int twoRasterLines = c_offsetToMoveToNextRasterLine + c_offsetToMoveToNextRasterLine;
        const int threeRasterLines = twoRasterLines + c_offsetToMoveToNextRasterLine;

        // we're going to look at 4x4 pixels at a time, and if any of them are illuminated, we'll set the corresponding pixel in the AI screen to 1
        // we only look at the green channel, as it includes white, and green
        // 
        // +-----------------+-----------------+-----------------+-----------------+------
        // | (0,0)      ...  | (4,0)      ...  | (8,0)      ...  | (12,0)     ...  |  ... 56 groups of 4 pixels
        // |  ...      (3,3) |  ...      (7,3) |  ...      (11,3)|   ...     (15,3)|  
        // +-----------------+-----------------+-----------------+-----------------+------
        // | (0,4)      ...  | (4,4)      ...  | (8,4)      ...  | (12,4)     ...  |  ... 56 groups of 4 pixels
        // |  ...      (3,7) |  ...      (7,7) |  ...      (11,7)|  ...      (15,7)|  
        // +-----------------+-----------------+-----------------+-----------------+------
        // | (0,8)      ...  | (4,8)      ...  | (8,8)      ...  | (12,8)     ...  |  ... 56 groups of 4 pixels
        // |  ............................................................................

        //  ... 64 times (for each 4 pixel raster line)

        fixed (byte* pBuffer = BackBuffer)
        {
            byte* pPixel = pBuffer + c_offsetGreenChannel; // we're ONLY looking at green channel (as it includes white, and green)

            for (int aiBufferIndex = 0; aiBufferIndex < AIscreen.Length; aiBufferIndex++)
            {
                // This may seem strange, but there is method in the madness. We don't need to check EVERY pixel if we find an illuminated pixel.
                // By using "||" and short-circuiting, we can avoid checking for any more pixels in the 4x4 block.
                bool hit =
                    // (0,0)-(3,0)
                    (*(pPixel) > 0) ||                    // (0,0)
                    (*(pPixel + c_bytesPerPixel) > 0) ||  // (1,0)
                    (*(pPixel + twoPX) > 0) ||            // (2,0)
                    (*(pPixel + threePX) > 0) ||          // (3,0)

                    // (0,1)-(3,1)
                    (*(pPixel + c_offsetToMoveToNextRasterLine) > 0) ||                     // (0,1)
                    (*(pPixel + c_offsetToMoveToNextRasterLine + c_bytesPerPixel) > 0) ||   // (1,1)
                    (*(pPixel + c_offsetToMoveToNextRasterLine + twoPX) > 0) ||             // (2,1)
                    (*(pPixel + c_offsetToMoveToNextRasterLine + threePX) > 0) ||           // (3,1)

                    // (0,2)-(3,2)
                    (*(pPixel + twoRasterLines) > 0) ||
                    (*(pPixel + twoRasterLines + c_bytesPerPixel) > 0) ||
                    (*(pPixel + twoRasterLines + twoPX) > 0) ||
                    (*(pPixel + twoRasterLines + threePX) > 0) ||

                    // (0,3)-(3,3)
                    (*(pPixel + threeRasterLines) > 0) ||
                    (*(pPixel + threeRasterLines + c_bytesPerPixel) > 0) ||
                    (*(pPixel + threeRasterLines + twoPX) > 0) ||
                    (*(pPixel + threeRasterLines + threePX) > 0);

                AIscreen[aiBufferIndex] = (hit ? 1 : 0);  // 1 if pixel illuminated in 4x4 block, 0 if not

                pPixel += fourPX; // move to the next 4x4 block

                // we've reached the end of the raster line, so move to the next row, which incrementing does.
                // however we're condensing 4 into 1. We would move 4 raster lines, but we've already moved 1, so we only need to move 3 more.
                if ((aiBufferIndex + 1) % 56 == 0 && aiBufferIndex != 0) pPixel += threeRasterLines;
            }
        }

        // scores are draw (see ScoreBoard.cs) @ 24,16 (characters are 8 pixels high). Each shrunk "row" is for 4 x 4px. 6x4=24
        for (int i = 0; i < 56 * 6; i++) AIscreen[i] = 0; // clear the score and high score. The latter mucks up the AI learning. TODO: make it 56x(64-6) i.e. 56x58 - this reduces neuron inputs

        // uncomment the lines below to see the video screen in debug Output as the AI sees it

        // Debug.WriteLine(VideoShrunkForAIOutputAsText(AIscreen));
        // Debugger.Break();

        return AIscreen;
    }

    /// <summary>
    /// It's nice to be able to check the video screen is correct. So this reverses the process of VideoShrunkForAI(),
    /// providing a text-based representation of the video screen.
    /// </summary>
    /// <param name="AIscreen"></param>
    /// <returns></returns>
    public static string VideoShrunkForAIOutputAsText(double[] AIscreen)
    {
        // quarter the size of the video for the AI
        // 224 x 256 -> 56 x 64

        StringBuilder outputAsText = new();

        for (int pixelIndex = 0; pixelIndex < AIscreen.Length; pixelIndex++)
        {
            outputAsText.Append(AIscreen[pixelIndex] == 1 ? "█" : " ");

            // are we at the end of the raster line? If so, add a new line
            if ((pixelIndex + 1) % 56 == 0 && pixelIndex != 0) outputAsText.AppendLine();
        }

        return outputAsText.ToString();
    }

    /// <summary>
    /// This creates an image overlay of screen display size, based on the low-resolution that the AI sees.
    /// It's intentionally semi-transparent, so you can see the 4x4 pixel degradation.
    /// </summary>
    /// <param name="AIscreen"></param>
    /// <returns></returns>
    public static Bitmap VideoShrunkForOverlay(double[] AIscreen)
    {
        // quarter the size of the video for the AI
        // 224 x 256 -> 56 x 64

        Bitmap result = new(224, 256);
        Graphics graphics = Graphics.FromImage(result);
        graphics.Clear(Color.Transparent); // we're plotting the pixels, not the non pixels - so we default to transparent

        //   Image is encoded as one long double[] array, so we need to plot pixels as if [x,y]
        //
        //   source      stored as
        //    ████
        //    █  █  => "█████  █████"
        //    ████

        int x = 0;
        int y = 0;

        // array is 56 x 64, representing the low res screen. Every 56 pixels is a new row.
        for (int i = 0; i < AIscreen.Length; i++)
        {
            if (AIscreen[i] == 1) graphics.FillRectangle(brushForOverlay, x, y, 4, 4); // 1= white, 0 = black

            // wrap to next row. Lesson learnt using modulo, it works best if you remember +1
            // (otherwise it mucks up the first row and everything is then misaligned)
            if ((i + 1) % 56 == 0 && i != 0)
            {
                y += 4; // down to next row of 4 pixels
                x = 0; // start at the left
            }
            else
            {
                x += 4; // move to pixel to the right
            }
        }

        return result;
    }

    /// <summary>
    /// We only need to create this once, so we cache it. It doesn't change based off what the game is doing.
    /// </summary>
    private static Bitmap? overlayOfSemiTransparentOverlayFilm = null;

    /// <summary>
    /// Show the colour of a pixel based on the original 1978 film.
    /// It iterates over 224x256 pixels asking what colour it should be. If it's red or green it paints it that colour but transparent.
    /// This is only useful to prove that we implemented the coloured film effect correctly.
    /// One thing that I think is strange is the film positioning consistency - I am pretty sure it's not aligned perfectly on the display, and that's why Game Over is partially white.
    /// It doesn't strike me as intentional, and had the red film been lower it would all be red.
    /// </summary>
    /// <returns></returns>
    public static Bitmap BitMapTransparentRedGreen()
    {
        // have we got a cached version? If so, return it.
        if (overlayOfSemiTransparentOverlayFilm is not null) return overlayOfSemiTransparentOverlayFilm;

        // it's not cached, so we need to create it.

        // quarter the size of the video for the AI
        // 224 x 256 -> 56 x 64
        overlayOfSemiTransparentOverlayFilm = new(224, 256);

        // test EVERY pixel and plot what colour it will be.
        for (int y = 0; y < 256; y++)
        {
            Color rowColor = ColourBasedOnFilm(y);

            for (int x = 0; x < 224; x++)
            {
                Color c = rowColor; // unless it is on below the floor, then it depends on the X.

                // edge case below the floor.
                if (y > OriginalDataFrom1978.c_greenLineIndicatingFloorPX && x > 16 && x < 136) c = OriginalDataFrom1978.s_playerColour;

                // don't make black pixels grey. Make them transparent.
                if (c == Color.White) c = Color.Transparent; else c = Color.FromArgb(128, c.R, c.G, c.B);

                overlayOfSemiTransparentOverlayFilm.SetPixel(x, y, c);
            }
        }

        // an image 224x256 (the size of the screen) that is a semi-transparent film.
        return overlayOfSemiTransparentOverlayFilm;
    }

    /// <summary>
    /// Draws a rectangle on the back buffer, in the specified colour.
    /// </summary>
    /// <param name="colour"></param>
    /// <param name="rectangle"></param>
    public void SetPixel(Color colour, Point point)
    {
        int offsetInBackBuffer = point.X * c_bytesPerPixel + point.Y * c_offsetToMoveToNextRasterLine;

        BackBuffer[offsetInBackBuffer /*+ c_offsetBlueChannel*/] = colour.B;  // blue offset is always 0, so we don't need to include it +0.
        BackBuffer[offsetInBackBuffer + c_offsetGreenChannel] = colour.G;
        BackBuffer[offsetInBackBuffer + c_offsetRedChannel] = colour.R;
        BackBuffer[offsetInBackBuffer + c_offsetAlphaChannel] = colour.A;
    }

    /// <summary>
    /// Get pixel from the back buffer.
    /// </summary>
    /// <param name="colour"></param>
    /// <param name="rectangle"></param>
    public Color GetPixel(Point point)
    {
        int offsetInBackBuffer = point.X * c_bytesPerPixel + point.Y * c_offsetToMoveToNextRasterLine;

        Color colour = Color.FromArgb(
                        BackBuffer[offsetInBackBuffer + c_offsetAlphaChannel],
                        BackBuffer[offsetInBackBuffer + c_offsetRedChannel],
                        BackBuffer[offsetInBackBuffer + c_offsetGreenChannel],
                        BackBuffer[offsetInBackBuffer /*+ c_offsetBlueChannel*/]);
        return colour;
    }
}