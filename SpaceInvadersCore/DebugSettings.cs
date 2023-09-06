using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersCore
{
    /// <summary>
    /// Debug settings for the troubleshooting the game play/rendering.
    /// </summary>
    public static class DebugSettings
    {
        // W A R N I N G :  DO NOT LEAVE THIS SET TO TRUE, IT WILL SLOW THE GAME DOWN HUGELY, AND FILL YOUR DISK WITH FRAME BY FRAME IMAGES!

        /// <summary>
        /// When true, it will save the bitmap to disk. It's a const but is reset if the debugger is not attached
        /// </summary>
        public static bool c_debugDrawEveryFrameAsAnImage = false;

        /// <summary>
        /// If debugging is on it, it will output every frame sequentially to this file.
        /// Note the path below is for my machine, you will need to change it. c:\temp\frames\ 
        /// </summary>
        public const string c_debugFileName = @"c:\temp\frames\SpaceInvader-video-debug-frame-{{debuggingFrameNumberForFileName}}.png";

        /// <summary>
        /// Used to number the debugging frames images sequentially.
        /// </summary>
        public static int s_debugFrameNumberForFileName = 1;

        /// <summary>
        /// If you set this to a number, it will stop the game at that frame number. It's useful if you see something happen and need to drill down.
        /// into what is happening.
        /// </summary>
        public const int c_debugStopAtFrameNumber = -1; // -1 means don't stop

        /// <summary>
        /// If the level is this, it will collect debugging data. It's useful if you see something happen and need to drill down.
        /// </summary>
        public const int c_debugDataCollectionAtLevel = -1; // -1 means don't collect data

        /// <summary>
        /// When true, it will draw a box around each sprite. It's a const but is reset if the debugger is not attached
        /// </summary>
        public static bool c_debugDrawBoxesAroundSprites = false;

        /// <summary>
        /// This is the brush used to draw a pixel overlay on top of the video display. It indicates what chunky pixels the AI sees.
        /// </summary>
        public static SolidBrush s_brushForOverlay = new(Color.FromArgb(220, 255, 100, 100));

        /// <summary>
        /// When the invaders get to x=9 or x=213 the invaders will step down. This is a visual indicator of that (2 blue vertical lines).
        /// It's a const but is reset if the debugger is not attached
        /// </summary>
        public static bool c_debugDrawVerticalLinesIndicatingStepDown = false;

        /// <summary>
        /// Paints the non shield radar (what it sees). Only applies if the input is RADAR.
        /// </summary>
        public static bool c_debugDrawMainRadar = false;

        /// <summary>
        /// Paints the shield radar (what it pings). Only applies if the input is RADAR.
        /// </summary>
        public static bool c_debugDrawShieldRadar = false;

        /// <summary>
        /// Debugging. Shows the hit box for the player.
        /// </summary>
        public static bool c_debugPlayerShipDrawHitBox = false;

        /// <summary>
        /// Debugging. Shows the hit box for the saucer.
        /// </summary>
        public static bool c_debugSaucerDrawHitBox = false;

        /// <summary>
        /// Debugging. Shows the hit box for the invaders.
        /// </summary>
        public static bool c_debugSpaceInvaderDrawHitBox = false;

        /// <summary>
        /// Set this to aid debugging bullet hitboxes.
        /// </summary>
        public static bool c_debugSpaceInvaderBulletDrawHitBox = false;

        /// <summary>
        /// Set by AI RADAR to indicate shields must be drawn in alpha 252. Not strictly a debug setting, but it's here for now.
        /// </summary>
        public static bool s_DrawShieldsIn252 = false;  // DO NOT MODIFY THIS, THE AI CODE DOES.

        /// <summary>
        /// Attempt to warn developers that they have debugging turned on, and turn it off if not being debugged.
        /// </summary>
        static DebugSettings()
        {
            // is the debug on?
            if (!c_debugDrawEveryFrameAsAnImage) return;

            if (!Debugger.IsAttached) // no debugger, we need to turn it off for performance, and to avoid filling the disk.
            {
                c_debugDrawEveryFrameAsAnImage = false;
                c_debugDrawBoxesAroundSprites = false;
                c_debugDrawVerticalLinesIndicatingStepDown = false;
                c_debugDrawMainRadar = false;
                c_debugDrawShieldRadar = false;
                c_debugPlayerShipDrawHitBox = false;
                c_debugSaucerDrawHitBox = false;
                c_debugSpaceInvaderDrawHitBox = false;
                c_debugSpaceInvaderBulletDrawHitBox = false;
            }
            else
            {
                Debug.WriteLine("WARNING: DEBUG IS TURNED ON. THIS WILL WRITE FRAME BY FRAME TO DISK, AND FILL IT.");
                Debug.WriteLine($"Debug frame-by-frame will be written to \"{c_debugFileName}\".");
                Debugger.Break(); // last chance saloon.
            }
        }

        /// <summary>
        /// Returns a filename to save images of the video screen, numbered sequentially.
        /// </summary>
        /// <returns></returns>
        internal static string GetFrameFilename()
        {
            return c_debugFileName.Replace("{{debuggingFrameNumberForFileName}}", (s_debugFrameNumberForFileName++).ToString().PadLeft(10, '0'));
        }
    }
}