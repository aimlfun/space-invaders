using SpaceInvadersCore.Game;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace SpaceInvadersCore.Tests
{
    /// <summary>
    /// Test the video display object.
    /// 
    /// One of the issues I had was with the reduction in size 56x64, where pixels seemed misaligned. 
    /// Broken because index % 56, is not the correct code. (index+1)%56 is what I should have used.
    /// A rookie mistake, maybe. This test helped me find it.
    /// 
    /// This is not a full test harness. There a million tests I could write. My code is generally reliable, most
    /// defects removed during the thousands of times I run the app and refine.
    /// </summary>
    public static class TestHarnessForVideo
    {
        //  █   █    ███    ████    █████    ███            █████   █████    ███    █████    ███    █   █    ████
        //  █   █     █     █   █   █       █   █             █     █       █   █     █       █     █   █   █
        //  █   █     █     █   █   █       █   █             █     █       █         █       █     ██  █   █
        //  █   █     █     █   █   ████    █   █             █     ████     ███      █       █     █ █ █   █
        //  █   █     █     █   █   █       █   █             █     █           █     █       █     █  ██   █  ██
        //   █ █      █     █   █   █       █   █             █     █       █   █     █       █     █   █   █   █
        //    █      ███    ████    █████    ███              █     █████    ███      █      ███    █   █    ████

        /// <summary>
        /// Test(s) as necessary.
        /// </summary>
        public static void PerformTest()
        {
            // make a video display object
            VideoDisplay video = new();

            // provide a distinct background colour
            video.ClearDisplay(Color.Blue);
            Bitmap b = DrawTestPattern(video);

            // shrink it.
            double[] a = video.VideoShrunkForAI();

            // output as text, which gives us a visual representation of the data independent of image rendering.
            File.WriteAllText(@"c:\temp\rectangleAsText.txt", VideoDisplay.VideoShrunkForAIOutputAsText(a));

            DrawOverlayOntoTestPattern(b, a);
        }

        /// <summary>
        /// Turns overlay into a bitmap and draws transparently  onto the original image.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private static void DrawOverlayOntoTestPattern(Bitmap b, double[] a)
        {
            Bitmap overlay;
            Graphics graphics;
            
            // now output it as a bitmap, and overlay it on the original image.
            overlay = VideoDisplay.VideoShrunkForOverlay(a);
            graphics = Graphics.FromImage(b);
            graphics.DrawImageUnscaled(overlay, 0, 0); // draw the transparent black, semi transparent red overlay on the original image

            b.Save(@"c:\temp\rectangle-overlay.png", ImageFormat.Png);
        }

        /// <summary>
        /// Outputs vertical stripe, base line and 4 shields to "rectangle.png".
        /// Creates overlay containing shrunk image for "rectangle.png" and outputs to "rectangle-overlay.png".
        /// Outputs shrunk image to "rectangleAsText.txt".
        /// </summary>
        /// <param name="video"></param>
        /// <returns></returns>
        private static Bitmap DrawTestPattern(VideoDisplay video)
        {
            PlotPointsToAidWithMappingDisassemblyScreenAddressToXY(video);

            int topYofShields = OriginalDataFrom1978.c_greenLineIndicatingFloorPX - 50;

            Sprite shield = OriginalSpritesFrom1978.Get("Shield");

            // add some shields, big and useful for boundary checks
            video.DrawSprite(shield, 32, topYofShields);
            video.DrawSprite(shield, 77, topYofShields);
            video.DrawSprite(shield, 122, topYofShields);
            video.DrawSprite(shield, 167, topYofShields);

            // add some vertical lines. They make it more obvious if our shrunk image is misaligned.
            for (int x = 0; x < 224; x += 8)
            {
                video.DrawVerticalLine(Color.Cyan, x);
            }

            CheckSetGetPixel(video, Color.Red);
            CheckSetGetPixel(video, Color.Blue);
            CheckSetGetPixel(video, Color.Green);


            for (int y = 0; y < 224; y++)
            {
                video.SetPixel(Color.Red, new Point(y, y));
                video.SetPixel(Color.Green, new Point(224 - y, y));
                video.SetPixel(Color.Blue, new Point(y, 200));
            }

            // this line wrapped like this.
            //  █████████████...
            // █

            video.DrawGreenHorizontalBaseLine(Color.DeepPink, 242);

            Bitmap b = video.GetVideoDisplayContent();

            // image as rendered
            b.Save(@"c:\temp\rectangle.png", ImageFormat.Png);
            return b;
        }

        /// <summary>
        /// The disassembly sometimes provides X, Y coordinates, sometimes a screen address.
        /// This helps map between the two.
        /// </summary>
        /// <param name="video"></param>
        private static void PlotPointsToAidWithMappingDisassemblyScreenAddressToXY(VideoDisplay video)
        {
            video.DrawByte(0x2400 + 32 * 0, 1);
            video.DrawByte(0x2400 + 32 * 1, 2);
            video.DrawByte(0x2400 + 32 * 2, 4);
            video.DrawByte(0x2400 + 32 * 3, 8);
            video.DrawByte(0x2400 + 32 * 4, 16);
            video.DrawByte(0x2400 + 32 * 5, 32);
            video.DrawByte(0x2400 + 32 * 6, 64);
            video.DrawByte(0x2400 + 32 * 7, 128);

            // test the base line drawing by the original, using our address-byte drawer.
            int screenAddress = 0x2402;

            for (int i = 0; i < 0xe0; i++)
            {
                video.DrawByte(screenAddress, 1);
                screenAddress += 32;
            }

            // this is the position of CREDIT 00, top,left.
            video.DrawByte(0x3501, 1);

            /*
               Print score header " SCORE<1> HI-SCORE SCORE<2> "
                191A: 0E 1C           LD      C,$1C               ; 28 bytes in message
                191C: 21 1E 24        LD      HL,$241E            ; Screen coordinates
                191F: 11 E4 1A        LD      DE,$1AE4            ; Score header message
                1922: C3 F3 08        JP      PrintMessage        ; Print score header
            */

            video.DrawByte(0x241E, 1);


            // Location of "0000" score for player 1
            video.DrawByte(0x271c, 1);

            // Location of "0000" score for high score
            video.DrawByte(0x2f1c, 1);

            // Location of "0000" score for player 2
            video.DrawByte(0x391c, 1);

            // player position
            video.DrawByte(0x2604, 1);

            // lives indicator (8,240)
            video.DrawByte(0x2501, 1);

            // location additional lives starts
            video.DrawByte(0x2701, 1);

            // shield bottom.
            video.DrawByte(0x2806, 128);
            
            // game over label
            Point pGameOver = VideoDisplay.AddressToXY(0x2803); // {X=32,Y=231}
            Debug.WriteLine($"game over={pGameOver}");

            // saucer
            int addressSaucer = VideoDisplay.ConvToScr(0x29, 0xd0);
            Point pSaucer = VideoDisplay.AddressToXY(addressSaucer); // {X = 9 Y = 47}
            Debug.WriteLine($"saucer={pSaucer}");

            // right edge detection: x=213, y=223
            video.DrawByte(0x3ea4, 128);

            // left edge detection:  x=9,   y=223
            video.DrawByte(0x2524, 128); 

            int addressLeft2 = VideoDisplay.ConvToScr(0x30, 0x20); // 9732 => 2604 
            int addressRight2 = VideoDisplay.ConvToScr(0xd9, 0x20); // 15140 => 3b24

            Point pLeft = VideoDisplay.AddressToXY(addressLeft2); // {X = 16 Y = 223}
            Point pRight = VideoDisplay.AddressToXY(addressRight2); // {X = 185 Y = 223}
            
            Debug.WriteLine($"left={pLeft} right={pRight}");
        }

        /// <summary>
        /// Used to ensure SetPixel and GetPixel work as expected.
        /// </summary>
        /// <param name="video"></param>
        /// <param name="colour"></param>
        /// <exception cref="Exception"></exception>
        private static void CheckSetGetPixel(VideoDisplay video, Color colour)
        {
            // set a pixel
            video.SetPixel(colour, new Point(0, 0));

            // now get it back, and it should match what we set.
            Color pixel = video.GetPixel(new Point(0, 0));
            
            // if it doesn't match set/get are not working properly. (e.g. RBGA storage/retrieval misaligned)
            if (pixel.R != colour.R && pixel.G != colour.G && pixel.B != colour.B)
            {
                throw new ApplicationException("GetPixel failed - RGBA don't match, which they should after setting");
            }
        }
    }

}