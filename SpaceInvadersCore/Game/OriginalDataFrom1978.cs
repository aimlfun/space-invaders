using System.Drawing;

namespace SpaceInvadersCore.Game;

/// <summary>
/// General things that enable us to draw it consistent with the original.
/// </summary>
internal static class OriginalDataFrom1978
{
    //    █      ███    █████    ███             ███     ███    █   █   █████    ███     ████   █   █   ████      █     █████    ███     ███    █   █
    //   ██     █   █       █   █   █           █   █   █   █   █   █   █         █     █       █   █   █   █    █ █      █       █     █   █   █   █
    //    █     █   █      █    █   █           █       █   █   ██  █   █         █     █       █   █   █   █   █   █     █       █     █   █   ██  █
    //    █      ████     █      ███            █       █   █   █ █ █   ████      █     █       █   █   ████    █   █     █       █     █   █   █ █ █
    //    █         █    █      █   █           █       █   █   █  ██   █         █     █  ██   █   █   █ █     █████     █       █     █   █   █  ██
    //    █        █     █      █   █           █   █   █   █   █   █   █         █     █   █   █   █   █  █    █   █     █       █     █   █   █   █
    //   ███    ███      █       ███             ███     ███    █   █   █        ███     ████    ███    █   █   █   █     █      ███     ███    █   █

    #region BRUSHES AND PENS
    /// <summary>
    /// This is the colour of the player space ship.
    /// </summary>
    internal static readonly Color s_playerColour = Color.FromArgb(31, 255, 31);

    /// <summary>
    /// This brush is used to draw the player.
    /// </summary>
    internal static readonly SolidBrush s_playerColourBrush = new(s_playerColour);

    /// <summary>
    /// This is a pen of the colour the player is draw in, and used for the horizontal line.
    /// </summary>
    internal static readonly Pen s_playerColourPen = new(s_playerColour);

    /// <summary>
    /// The is the colour the space invaders are drawn in.
    /// </summary>
    internal static readonly SolidBrush s_spaceInvaderBrush = new(Color.FromArgb(240, 240, 255));
    #endregion

    #region POSITIONS AND SIZES
    /// <summary>
    /// This is the width of the Space Invader screen.
    /// </summary>
    internal const int c_screenWidthPX = 224;

    /// <summary>
    /// This is the height of the Space Invader screen.
    /// </summary>
    internal const int c_screenHeightPX = 256;

    /// <summary>
    /// This determines the speed bullets move at.
    /// </summary>
    internal const int c_playerBulletSpeedPX = 4;

    //  +----------------------------------------------------------+
    //  |                                                          |
    //  | SCORE<1>               HIGH-SCORE               SCORE<2> |
    //  |                                                          |
    //  |   0010                    1000                           |
    //  |                                                          | ___  c_topOfSaucerLinePX
    //  | <ooo>                                                    | ___  c_verticalPointWherePlayerBulletsStopPX
    //  |                                                          | ___  c_invaderStartRowOffsetPX
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
    //  |                                                          |
    //  |                                                          |
    //  |                                                          |
    //  |                                                          |
    //  |  <#|#>                                                   | ___  c_yOfBaseLineAboveWhichThePlayerShipIsDrawnPX
    //  |                                                          |
    //  |----------------------------------------------------------| <--  c_greenLineIndicatingFloorPX
    //  |  3  <#|#> <#|#>                                          | 
    //  +----------------------------------------------------------+
    //                         c_screenWidthPX

    /// <summary>
    /// This is where player bullets stop.
    /// </summary>
    internal const int c_verticalPointWherePlayerBulletsStopPX = 35;

    /// <summary>
    /// This is the vertical top where the saucer starts.
    /// </summary>
    internal const int c_topOfSaucerLinePX = 40;

    /// <summary>
    /// This is where the player is positioned vertically.
    /// </summary>
    internal const int c_yOfBaseLineAboveWhichThePlayerShipIsDrawnPX = 228; //px

    /// <summary>
    /// This is where row 1 of invaders starts.
    /// </summary>
    internal const int c_invaderStartRowOffsetPX = 56; //px

    /// <summary>
    /// This is used for the green line at the bottom.
    /// </summary>
    internal const int c_greenLineIndicatingFloorPX = 242;
    #endregion

    #region ANIMATIONS
    /// <summary>
    /// Each Space Invader image (2) frames, indexed by their row (some appear on 2 rows).
    /// </summary>
    internal static Dictionary<int /*row*/, string[] /*Space Invader frames*/> s_spaceInvaderImageFramesIndexedByRow = new();

    /// <summary>
    /// Image frames for the rolling bullet.
    /// </summary>
    internal static string[] s_rollingBulletIndexedByFrame;

    /// <summary>
    /// Image frames for the plunger bullet.
    /// </summary>
    internal static string[] s_plungerBulletIndexedByFrame;

    /// <summary>
    /// Image frames for the squiggly bullet.
    /// </summary>
    internal static string[] s_squigglyBulletIndexedByFrame;

    /// <summary>
    /// Some task are time critical, so we need to know how frequent our frames are occurring.
    /// The screen refresh rate on the original is 60Hz. So each interrupt is executed 60 times a seconds = 16.67 ms
    /// Except it seemed too slow, so I made it 2 times faster. There are 3 interrupts on the SI game, it's possibly
    /// related to that (top, middle, bottom).
    /// </summary>
    private const float s_timerFrequency = 16.6667f / 2f; // ms

    /// <summary>
    /// Saucer appear every 30 seconds.
    /// </summary>
    internal const int c_saucerFrameFrequency = (int)(15f * (1000f / s_timerFrequency));
    #endregion

    /// <summary>
    /// Scores depend on shots fired. This table is used to look up which to assign.
    /// </summary>
    internal static int[] s_saucerScores = new int[] { 100, 50, 50, 100, 150, 100, 50, 300, 100, 100, 100, 50, 150, 10, 50 };

    /*
     * ColFireTable:
        ; This table decides which column a shot will fall from. The column number is read from the
        ; table (1-11) and the pointer increases for the shot type. For instance, the "squiggly" shot
        ; will fall from columns in this order: 0B, 01, 06, 03. If you play the game you'll see that
        ; order.
        ;
        ; The "plunger" shot uses index 00-0F (inclusive)
        ; The "squiggly" shot uses index 06-14 (inclusive)
        ; The "rolling" shot targets the player
    */

    /// <summary>
    /// Used to determine the column that the "plunger" shot will fall from. Each shot uses the next column in the table.
    /// </summary>
    internal static int[] s_plungerShotColumn = new int[] { 1, 7, 1, 1, 1, 4, 11, 1, 6, 3, 1, 1, 11, 9, 2, 8 };

    /// <summary>
    /// Used to determine the column that the "squiggly" shot will fall from. Each shot uses the next column in the table.
    /// </summary>
    internal static int[] s_squigglyShotColumn = new int[] { 11, 1, 6, 3, 1, 1, 11, 9, 2, 8, 2, 11, 4, 7, 10 };

    /// <summary>
    /// A life is awarded at 1500 points. The expectation is that the players aren't meant to get them every 1500, given
    /// the score is limited at 9999 before wrapping to 0.
    /// </summary>
    internal const int c_scoreAtWhichExtraLifeIsAwarded = 1500; // default is 1500, but can be 1000 depending on DIP switch settings.

    /// <summary>
    /// Static Constructor.
    /// </summary>
    static OriginalDataFrom1978()
    {
        // load the frames for the Space Invaders
        s_spaceInvaderImageFramesIndexedByRow.Add(4, new string[] { "AlienSprC0", "AlienSprC1" });
        s_spaceInvaderImageFramesIndexedByRow.Add(3, new string[] { "AlienSprB0", "AlienSprB1" });
        s_spaceInvaderImageFramesIndexedByRow.Add(2, new string[] { "AlienSprB0", "AlienSprB1" });
        s_spaceInvaderImageFramesIndexedByRow.Add(1, new string[] { "AlienSprA0", "AlienSprA1" });
        s_spaceInvaderImageFramesIndexedByRow.Add(0, new string[] { "AlienSprA0", "AlienSprA1" });

        // load the bullet frames.
        s_rollingBulletIndexedByFrame = new string[] { "RollShot-1", "RollShot-2", "RollShot-3", "RollShot-4" };
        s_squigglyBulletIndexedByFrame = new string[] { "SquigglyShot-1", "SquigglyShot-2", "SquigglyShot-3", "SquigglyShot-4" };
        s_plungerBulletIndexedByFrame = new string[] { "PlungerShot-1", "PlungerShot-2", "PlungerShot-3", "PlungerShot-4" };
    }
}