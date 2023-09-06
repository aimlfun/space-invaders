using SpaceInvadersCore.Game.SpaceInvaders;
using System.Drawing;
using Windows.ApplicationModel.Background;
using static System.Formats.Asn1.AsnWriter;

namespace SpaceInvadersCore.Game;

/// <summary>
/// General things that enable us to draw it consistent with the original.
/// </summary>
public static class OriginalDataFrom1978
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
    /// [$04] @202C	shotDeltaX	Shot's delta X (rotated screen, it is delta Y)
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
    //  |  <#|#>                                                   | ___  OriginalDataFrom1978.c_PlayerShipStartLocation.Y
    //  |                                                          |
    //  |----------------------------------------------------------| <--  c_greenLineIndicatingFloorPX
    //  |  3   <#|#> <#|#>                                         | 
    //  +----------------------------------------------------------+
    //     ^s_livesNumberLocation
    //         ^ s_additionalLifeLocation
    //  |<----------------- c_screenWidthPX ---------------------->|

    /// <summary>
    /// This the top of the screen where player bullets stop.
    /// </summary>
    internal const int c_verticalPointWherePlayerBulletsStopPX = 40;

    /// <summary>
    /// This is the vertical top where the saucer starts. 
    /// (9,47) is the top left of the saucer.
    /// </summary>
    internal const int c_topOfSaucerLinePX = 47;

    /// <summary>
    /// This is where row 1 of invaders starts.
    /// </summary>
    internal const int c_invaderStartRowOffsetPX = 56; //px

    /// <summary>
    /// This is where the player is positioned.
    /// 60 1c 20 30 10
    /// $1c60 = sprite image
    /// $3020 = pixel position; it seems a little odd but the problem is the anti-clockwise rotation means we add $20 to a screen address per X.
    /// int address = VideoDisplay.ConvToScr(0x30, 0x20);
    /// Point p = VideoDisplay.AddressToXY(address); // {X = 16 Y = 223}
    /// 
    /// We draw downwards, not upwards, so we need to subtract the height of the player.
    /// 223 is the BOTTOM of the player ship
    /// -8
    /// => 215 is the TOP of the player ship
    /// </summary>
    internal readonly static Point s_PlayerShipStartLocation = new(16, 215);

    /// <summary>
    /// This is the top left of the lives number.
    /// 1A8B: 21 01 25        LD      HL,$2501            ; Screen coordinates
    /// </summary>
    internal readonly static Point s_livesNumberLocation = new(8, 240);

    /// <summary>
    /// This is the top left of the addition life, where we draw the player ship.
    /// Each next one is 16 pixels to the right.
    /// </summary>
    internal readonly static Point s_additionalLifeLocation = new(24, 240);
    
    /// <summary>
    /// Location of score header " SCORE<1> HI-SCORE SCORE<2> ".
    /// 191A: 0E 1C           LD      C,$1C               ; 28 bytes in message
    /// 191C: 21 1E 24        LD      HL,$241E            ; Screen coordinates
    /// 191F: 11 E4 1A        LD      DE,$1AE4            ; Score header message
    /// 1922: C3 F3 08        JP      PrintMessage; Print score header
    /// 
    /// $241e is at (0,8)
    /// </summary>
    internal static readonly Point s_score1HighScoreAndScore2Position = new(0, 8);

    /// <summary>
    /// Position of "CREDIT 00" text.
    /// ; Print message "CREDIT "
    /// 193C: 0E 07           LD      C,$07               ; 7 bytes in message
    /// 193E: 21 01 35        LD      HL,$3501            ; Screen coordinates
    /// 1941: 11 A9 1F        LD      DE,$1FA9            ; Message = "CREDIT "
    /// 1944: C3 F3 08        JP      PrintMessage        ; Print message
    /// 
    /// $3501, is at 136, 240 (rotated).
    /// </summary>
    public static readonly Point s_credit_00_position = new(136, 240);

    /// <summary>
    /// This is used for the green line at the bottom.
    /// 01CF: 3E 01           LD      A,$01               ; Bit 1 set ... going to draw a 1-pixel stripe down left side
    /// 01D1: 06 E0           LD      B,$E0               ; All the way down the screen
    /// 01D3: 21 02 24        LD      HL,$2402            ; Screen coordinates (3rd byte from upper left)
    /// 01D6: C3 CC 14        JP      $14CC               ; Draw line down left side
    /// 
    /// top left = $2400.
    /// line starts at $2402, which is the 3rd byte from the top left (non rotated). = 2*8 = 16px, the $01 = |00000001|
    /// In rotated mode, it is row 23 (2 bytes = 16px, the 3rd byte bit 0 is makes another 8) = 16+8 = 24px, except we start at pixel 0, so 23.
    /// 256px height(0..255) means 255-23 = 232.
    /// </summary>
    internal const int c_greenLineIndicatingFloorPX = 232;

    /// <summary>
    /// 0221: 01 02 16        LD      BC,$1602            ; 22 rows, 2 bytes/row (for 1 shield pattern)
    /// 0224: 21 06 28        LD      HL,$2806            ; Screen coordinates
    /// ...
    /// 023A: 11 E0 02        LD      DE,$02E0            ; Add 2E0 (23 rows) to get to ...
    /// 
    /// (shields are 22 wide wide, 16 tall in rotated mode, the way you view it)
    /// 
    /// $2806 = (32,207) this is FIRST pixel of the bottom part of the shields. We need to move up 16px to get to the top of the shields.
    /// 
    /// |                                                          |  ___ 191
    /// |     ##             ##              ##             ##     | 
    /// |    ####           ####            ####           ####    | 
    /// |    #  #           #  #            #  #           #  #    |  ___ 207 
    /// |                                                          |        
    /// |                                                          |        
    /// +----------------------------------------------------------+  --- 255  (it goes 0...255 = 256px)
    /// |<-->|        
    ///   32px                                                    
    /// 
    /// </summary>
    internal const int c_topOfShieldsPX = 191; // px 

    /// <summary>
    /// Some task are time critical, so we need to know how frequent our frames are occurring.
    /// The screen refresh rate on the original is 60Hz. So each interrupt is executed 60 times a seconds = 16.67 ms.
    /// Although RST 1 ($0008) and RST 2 ($0010) are used, it doesn't mean that the game runs twice the speed.
    /// isr (interrupt service routine) 1 fires at line 96, and 2 fires at 224 (bottom, v-blank). The game treats 96 as "mid screen" and draws things 
    /// other than the player in the half of the screen that isn't being updated to avoid flickering. We don't have that problem, so no need to split logic
    /// across 2 routines.
    /// </summary>
    public const float c_timerFrequency = 16.6667f/2f; // ms - 60hz = 16.67ms, but that seems too slow for my liking
    #endregion

    #region ANIMATIONS
    /// <summary>
    /// Each Space Invader image (2) frames, indexed by their row (some appear on 2 rows).
    /// </summary>
    internal readonly static Dictionary<int /*row*/, string[] /*Space Invader frames*/> s_spaceInvaderImageFramesIndexedByRow = new();

    /// <summary>
    /// Image frames for the rolling bullet.
    /// </summary>
    internal readonly static string[] s_rollingBulletIndexedByFrame;

    /// <summary>
    /// Image frames for the plunger bullet.
    /// </summary>
    internal readonly static string[] s_plungerBulletIndexedByFrame;

    /// <summary>
    /// Image frames for the squiggly bullet.
    /// </summary>
    internal readonly static string[] s_squigglyBulletIndexedByFrame;

    /// <summary>
    /// Saucer appear every 30 seconds.
    /// [$00] @$2091	tillSaucerLSB
    /// [$06] @$2092    tillSaucerMSB Count down every game loop. 
    /// Disassembly says "When it reaches 0 saucer is triggered. Reset to 600."
    /// 0921: 21 00 06        LD      HL,$0600             ; Reset timer to 600 game loops <<<--- 1536 game loops ($600)
    /// 0924: 3E 01           LD      A,$01                ; Flag a...
    /// 0926: 32 83 20        LD      (saucerStart), A     ; ... saucer sequence
    /// 0929: 2B              DEC     HL                   ; Decrement the...
    /// 092A: 22 91 20        LD      (tillSaucerLSB), HL  ; ... time-to-saucer <<<--- 1536 game loops ($600)
    /// </summary>
    internal const int c_saucerFrameFrequency = 6 * 256;
    #endregion

    /// <summary>
    /// Scores depend on shots fired. This table is used to look up which to assign.
    /// SaucerScrTab:
    /// ; 208D points here to the score given when the saucer is shot.It advances
    /// ; every time the player-shot is removed. The code wraps after 15, but there
    /// ; are 16 values in this table. This is a bug in the code at 044E (thanks to
    /// ; Colin Dooley for finding this).
    /// ;
    /// ; Thus the one and only 300 comes up every 15 shots (after an initial 8).
    /// 1D54: 10 05 05 10 15 10 10 05 30 10 10 10 05 15 10 05   
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
    /// Scoring points for an invader.
    /// 
    /// AlienScores:
    /// ; Score table for hitting alien type
    /// 1DA0: 10 ; Bottom 2 rows
    /// 1DA1: 20 ; Middle row
    /// 1DA2: 30 ; Highest row
    /// </summary>
    const int c_invaderPointsRow0 = 30;
    const int c_invaderPointsRow1and2 = 20;
    const int c_invaderPointsRow3and4 = 10;

    /// <summary>
    /// The points awarded for killing an invader indexed by the row of the invader.
    /// </summary>
    internal static readonly int[] s_invaderPoints = new[] { c_invaderPointsRow3and4, c_invaderPointsRow3and4, c_invaderPointsRow1and2, c_invaderPointsRow1and2, c_invaderPointsRow0 };

    /// <summary>
    /// This is the rectangle that will be used to erase the score for player 1.
    /// Defaulted in the static constructor (depends on other static variables).
    /// </summary>
    internal static readonly Rectangle s_scorePlayer1Rectangle;

    /// <summary>
    /// This is the rectangle that will be used to erase the score for player 2.
    /// Defaulted in the static constructor (depends on other static variables).
    /// </summary>
    public static readonly Rectangle s_scorePlayer2Rectangle;

    /// <summary>
    /// This is the rectangle that will be used to erase the high score.
    /// Defaulted in the static constructor (depends on other static variables).
    /// </summary>
    internal static Rectangle s_highScoreRectangle;

    /// <summary>
    /// This is the location where the player 1 score will be drawn.
    /// Comes from $20F8.
    /// 00 00 1c 27
    /// Position: $271c => (24,24)
    /// </summary>
    internal readonly static Point s_scorePlayer1Location = new(24, 24);

    /// <summary>
    /// This is the location where the high score will be drawn.
    /// Comes from $20F4 descriptor. Score hex (2 bytes, 99 99) and then the location.
    /// 00 00 1c 2f
    /// Position: $2f1c => (88,24)
    /// </summary>
    internal readonly static Point s_highScoreLocation = new(88, 24);

    /// <summary>
    /// This is the location where the player 2 score will be drawn.
    /// Comes from $20FC.
    /// 00 00 1c 39
    /// Position: $391c => (168,24)
    /// </summary>
    internal readonly static Point s_scorePlayer2Location = new(168, 24);

    /// <summary>
    /// Static Constructor.
    /// </summary>
    static OriginalDataFrom1978()
    {
        // scores are 2 bytes, each of 2 hex digits, i.e. 4 digits, hence 9999 max score.
        s_scorePlayer1Rectangle = new(s_scorePlayer1Location.X, s_scorePlayer1Location.Y, 4 * 8, 8);
        s_scorePlayer2Rectangle = new(s_scorePlayer2Location.X, s_scorePlayer2Location.Y, 4 * 8, 8);
        s_highScoreRectangle = new(s_highScoreLocation.X, s_highScoreLocation.Y, 4 * 8, 8);

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