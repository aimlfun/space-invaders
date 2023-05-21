using SpaceInvadersCore.Game;
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
            int topYofShields = OriginalDataFrom1978.c_greenLineIndicatingFloorPX - 50;

            // add some shields, big and useful for boundary checks
            video.DrawSprite(OriginalSpritesFrom1978.Sprites["Shield"], 32, topYofShields);
            video.DrawSprite(OriginalSpritesFrom1978.Sprites["Shield"], 77, topYofShields);
            video.DrawSprite(OriginalSpritesFrom1978.Sprites["Shield"], 122, topYofShields);
            video.DrawSprite(OriginalSpritesFrom1978.Sprites["Shield"], 167, topYofShields);

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

        private static void CheckSetGetPixel(VideoDisplay video, Color colour)
        {
            video.SetPixel(colour, new Point(0, 0));
            Color pixel = video.GetPixel(new Point(0, 0));
            if (pixel.R != colour.R && pixel.G != colour.G && pixel.B != colour.B)
            {
                throw new Exception("GetPixel failed");
            }
        }
    }

}