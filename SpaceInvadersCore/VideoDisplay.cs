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


    // The Space Invader video screen is naturally, 256 x 224 pixels.

    // 2400                     241f
    // +------------------------+
    // |                        |
    // |                        |
    // |------------------------224   -- ScanLine96
    // |                        |
    // |                        |
    // +----------256-----------+     -- ScanLine224 VBLANK
    // 3FE0

    // BUT... Taito rotated it 90 degrees counter clockwise, so it is 224 x 256 pixels.

    // 241F
    // +--------------------+
    // |                    |        
    // |                    |
    // |                    |
    // |                   256
    // |                    |
    // |                    |
    // |                    |
    // +---------224--------+      ^ bits are plotted UPWARDS y,x (not x,y). That's why the sprites are rotated 90 clockwise degrees.
    // 2400                 3FE0

    // e.g the bits 10000011
    //
    // +---
    // |
    // |
    // |
    // ..
    // |1 }
    // |0 }
    // |0 }
    // |0 }
    // |0 } 1 byte ^
    // |0 }        |
    // |1 }        |
    // +1 }


    #region CONSTANTS - DO NOT CHANGE THESE VALUES!
    /// <summary>
    /// Pixels are 32bit ARGB, stored BGRA in terms of offset.
    /// This is the offset for BLUE.
    /// SonarQube: "Remove this unused 'c_offsetBlueChannel' private field.". I don't want to. It's not referenced, but to add clarity to the code, I'm leaving it in.
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
    private const int c_bytesPerPixel = 4; // is the equivalent of PixelFormat.Format32bppArgb. 32 bits per pixel, 4 bytes per pixel (A,R,G,B).

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
        if (DebugSettings.c_debugDrawEveryFrameAsAnImage)
        {
            bitmapBeingModifiedDirectly.Save(DebugSettings.GetFrameFilename(), ImageFormat.Png);
        }

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

        // useful if you need to know that the step down occurs at the correct place on the screen.
        if (DebugSettings.c_debugDrawVerticalLinesIndicatingStepDown)
        {
            DrawVerticalLine(Color.Blue, 9);
            DrawVerticalLine(Color.Blue, 213);
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
        //  +----------------------------------------------------------+ _ 8 s_score1HighScoreAndScore2Position
        //  | SCORE<1>               HIGH-SCORE               SCORE<2> |  
        //  |                                                         .| _ 24
        //  |   0010                    1000                           | s_scorePlayer1Location, s_highScoreLocation, s_scorePlayer2Location
        //  |..........................................................| } y=47
        //  |.<ooo>....................................................| } red transparent film 
        //  |..........................................................| } y=58
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
        //  |.....##.............##..............##.............##.....| } y = OriginalDataFrom1978.c_topOfShieldsPX-8
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

        if (y > 33 && y < 58) return Color.FromArgb(255, 255, 25, 0); // magenta film.
        if (y > OriginalDataFrom1978.c_topOfShieldsPX - 8 && y < OriginalDataFrom1978.c_greenLineIndicatingFloorPX) return OriginalDataFrom1978.s_playerColour; // green film.

        return Color.White; // non coloured
    }

    /// <summary>
    /// Draws a sprite on the screen at the specified location.
    /// </summary>
    /// <param name="sprite"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public unsafe void DrawSprite(Sprite sprite, int x, int y, byte alpha = 255)
    {
        // debugging? Show a box around the sprite (before drawing it)
        if (DebugSettings.c_debugDrawBoxesAroundSprites) DrawRectangle(Color.Blue, new Rectangle(x, y, sprite.WidthInPX, sprite.HeightInPX));

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
                        *(pPixel + c_offsetAlphaChannel) = alpha; // thisPixel.A; // alpha is always 255, set by .ClearDisplay()
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
        if (DebugSettings.c_debugDrawBoxesAroundSprites) DrawRectangle(Color.Blue, new Rectangle(x, y, sprite.WidthInPX, sprite.HeightInPX));

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
    public unsafe bool DrawSpriteWithCollisionDetection(Sprite sprite, int x, int y, byte alpha = 255)
    {
        bool hit = false;

        // debugging? Show a box around the sprite (before drawing it). To avoid breaking the collision detection, we draw the box in blue that it cannot detect.
        if (DebugSettings.c_debugDrawBoxesAroundSprites) DrawRectangle(Color.Blue, new Rectangle(x, y, sprite.WidthInPX, sprite.HeightInPX));

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
                        *(pPixel + c_offsetAlphaChannel) = alpha; // colour.A; // alpha is always 255, set by .ClearDisplay() - this saves updating 1 byte per pixel (performance)
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
        if (DebugSettings.c_debugDrawBoxesAroundSprites) DrawRectangle(Color.Blue, new Rectangle(x, y, sprite.WidthInPX, sprite.HeightInPX));
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
            Sprite s = OriginalSpritesFrom1978.Get(c.ToString());

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
        // 32 pixels at top with score, and 32 pixels at bottom with lives, so we can exclude those
        double[] AIScreen = new double[56 * (64 - 16)]; // AI needs doubles, not bytes

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
            // 56x8x4 = top score to skip [224 pixels per row, 8 rows x 4 row chunks x 4 bytes per pixel]
            byte* pPixel = pBuffer + c_offsetGreenChannel + 224 * 8 * 4 * 4; // we're ONLY looking at green channel (as it includes white, and green)

            for (int aiBufferIndex = 0; aiBufferIndex < AIScreen.Length; aiBufferIndex++)
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

                AIScreen[aiBufferIndex] = (hit ? 1 : 0);  // 1 if pixel illuminated in 4x4 block, 0 if not

                pPixel += fourPX; // move to the next 4x4 block

                // we've reached the end of the raster line, so move to the next row, which incrementing does.
                // however we're condensing 4 into 1. We would move 4 raster lines, but we've already moved 1, so we only need to move 3 more.
                if ((aiBufferIndex + 1) % 56 == 0 && aiBufferIndex != 0) pPixel += threeRasterLines;
            }
        }

        // SonarQube doesn't like this. However, I don't want to add some "if()" mapped to a const. It makes more sense to comment it
        // out, and leave it as a reminder of what the code is doing.
        // uncomment the lines below to see the video screen in debug Output as the AI sees it

        // Debug.WriteLine(VideoShrunkForAIOutputAsText(AIScreen));
        // Debugger.Break();

        return AIScreen;
    }

    /// <summary>
    /// It's nice to be able to check the video screen is correct. So this reverses the process of VideoShrunkForAI(),
    /// providing a text-based representation of the video screen.
    /// </summary>
    /// <param name="AIScreen"></param>
    /// <returns></returns>
    public static string VideoShrunkForAIOutputAsText(double[] AIScreen)
    {
        // quarter the size of the video for the AI
        // 224 x 256 -> 56 x 64

        StringBuilder outputAsText = new();

        for (int pixelIndex = 0; pixelIndex < AIScreen.Length; pixelIndex++)
        {
            outputAsText.Append(AIScreen[pixelIndex] == 1 ? "█" : " ");

            // are we at the end of the raster line? If so, add a new line
            if ((pixelIndex + 1) % 56 == 0 && pixelIndex != 0) outputAsText.AppendLine();
        }

        return outputAsText.ToString();
    }

    /// <summary>
    /// This creates an image overlay of screen display size, based on the low-resolution that the AI sees.
    /// It's intentionally semi-transparent, so you can see the 4x4 pixel degradation.
    /// </summary>
    /// <param name="AIScreen"></param>
    /// <returns></returns>
    public static Bitmap VideoShrunkForOverlay(double[] AIScreen)
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
        int y = 8*4; // top 32 pixel rows are score, nothing to overlay. .Length will ensure bottom 32 pixel rows are not plotted

        // array is 56 x 64, representing the low res screen. Every 56 pixels is a new row.
        for (int i = 0; i < AIScreen.Length; i++)
        {
            if (AIScreen[i] == 1)
            {
                graphics.FillRectangle(DebugSettings.s_brushForOverlay, x, y, 4, 4); // 1= white, 0 = black
            }

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
    /// <param name="point"></param>
    public void SetPixel(Color colour, Point point)
    {
        int offsetInBackBuffer = point.X * c_bytesPerPixel + point.Y * c_offsetToMoveToNextRasterLine;

        BackBuffer[offsetInBackBuffer /*+ c_offsetBlueChannel*/] = colour.B;  // blue offset is always 0, so we don't need to include it +0.
        BackBuffer[offsetInBackBuffer + c_offsetGreenChannel] = colour.G;
        BackBuffer[offsetInBackBuffer + c_offsetRedChannel] = colour.R;
        BackBuffer[offsetInBackBuffer + c_offsetAlphaChannel] = colour.A;
    }

    /// <summary>
    /// Get colour of the pixel at the specified point from the back buffer.
    /// </summary>
    /// <param name="point"></param>
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

    /// <summary>
    /// This translates the 1978 Space Invader screen address and byte into a pixel on the screen.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="byteOfData"></param>
    public void DrawByte(int address, int byteOfData)
    {
        Point addressAsPoint = AddressToXY(address);

        // for each bit in the byte, draw a pixel.
        for (int i = 0; i < 8; i++)
        {
            int bit = (byteOfData >> i) & 1;

            // if the bit is set, draw a pixel.
            if (bit == 1)
            {
                SetPixel(Color.White, new Point(addressAsPoint.X, addressAsPoint.Y - (7 - i))); // 0 (lsb) is the right most pixel, on the original screen (non rotated)
            }
        }
    }

    /// <summary>
    /// Maps an address to an X and Y coordinate (screen is 2400-3fff in Space Invaders).
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Point AddressToXY(int address)
    {
        if (address < 0x2400 || address > 0x3FFF) throw new ArgumentOutOfRangeException(nameof(address), "Invalid address - it is not within the screen");

        // in the original 2400...3fe0 step 32 is the raster lines (y). But we're drawing it rotated 90 degrees.
        int x = (address - 0x2400) / 32;

        // 8 pixels per byte, address $2400 is bottom left, $241f is top left, bytes are draw upwards, so we need to work out the X and Y
        int y = 255 - (((address - 0x2400) - x * 32) * 8);

        return new Point(x, y);
    }

    /// <summary>
    /// Implementation of the original Space Invaders screen drawing routine ConvToScr @ 1A47.
    /// I really don't understand WHY the original code was implemented this way. Storing a real x,y makes more sense.
    /// Please let me know if you know why it was done this way.
    /// </summary>
    /// <param name="H"></param>
    /// <param name="L"></param>
    /// <returns>HL</returns>
    public static int ConvToScr(int H, int L)
    {
        /*
        ConvToScr:
        ; The screen is organized as one - bit - per - pixel.
        ; In: HL contains pixel number(bbbbbbbbbbbbbppp)
        ; Convert from pixel number to screen coordinates(without shift)
        ; Shift HL right 3 bits(clearing the top 2 bits)
        ; and set the third bit from the left.

            1A48: 06 03           LD      B,$03 ; 3 shifts(divide by 8)
            1A4A: 7C              LD      A,H   ; H to A
            1A4B: 1F              RRA           ; Shift right(into carry, from doesn't matter)
            1A4C: 67              LD      H, A  ; Back to H
            1A4D: 7D              LD      A, L  ; L to A
            1A4E: 1F              RRA           ; Shift right(from/ to carry)
            1A4F: 6F              LD      L, A  ; Back to L
            1A50: 05              DEC     B     ; Do all ...
            1A51: C2 4A 1A        JP      NZ,$1A4A ; ... 3 shifts
            1A54: 7C              LD      A,H   ; H to A
            1A55: E6 3F           AND     $3F   ; Mask off all but screen(less than or equal 3F)
            1A57: F6 20           OR      $20   ; Offset into RAM
            1A59: 67              LD      H, A  ; Back to H
        */
        
        int HL = (H << 8) | L;  // HL = H * 256 + L (lsb, msb)
        int b = 3;

        // Do a /2, 3 times
        for(int i = 0; i < b; i++)
        {
            HL >>= 1; // 2
        }
        
        H = HL / 256;
        L = HL % 256;
        
        H &= 0x3F;
        H |= 0x20;
        
        // return HL reconstructed
        return (H << 8) | L;
    }
}