#define FireFromLastInvader
using System.Diagnostics;
using System.Drawing;
using SpaceInvadersCore.Utilities;

namespace SpaceInvadersCore.Game.SpaceInvaders;

/// <summary>
/// This represents a controller for the Space Invaders to shoot.
/// It creates the 5 rows of 11 invaders.
/// It handles the rippled movement, and firing of the invaders.
/// It handles the drawing of the invaders.
/// </summary>
internal class SpaceInvaderController
{

    //   ███    █   █   █   █     █     ████    █████   ████             ███     ███    █   █   █████   ████     ███    █       █       █████   ████
    //    █     █   █   █   █    █ █    █   █   █       █   █           █   █   █   █   █   █     █     █   █   █   █   █       █       █       █   █
    //    █     ██  █   █   █   █   █   █   █   █       █   █           █       █   █   ██  █     █     █   █   █   █   █       █       █       █   █
    //    █     █ █ █   █   █   █   █   █   █   ████    ████            █       █   █   █ █ █     █     ████    █   █   █       █       ████    ████
    //    █     █  ██   █   █   █████   █   █   █       █ █             █       █   █   █  ██     █     █ █     █   █   █       █       █       █ █
    //    █     █   █    █ █    █   █   █   █   █       █  █            █   █   █   █   █   █     █     █  █    █   █   █       █       █       █  █
    //   ███    █   █     █     █   █   ████    █████   █   █            ███     ███    █   █     █     █   █    ███    █████   █████   █████   █   █

    private const int c_verticalSeparationInPixelsOfAlienBulletFromPlayerShip = 40;

    /// <summary>
    /// Left adjustment in pixels because aliens are different sizes
    /// </summary>
    private static readonly int[] xLeftAdjustment = { 0, 0, 1, 1, 2 };

    /// <summary>
    /// Right adjustment in pixels because aliens are different sizes
    /// </summary>
    private static readonly int[] xRightAdjustment = { 13, 13, 13, 13, 11 };

    #region INVADER CONTROL PROPERTIES
    //  ___                 _            ___         _           _   ___                       _   _        
    // |_ _|_ ___ ____ _ __| |___ _ _   / __|___ _ _| |_ _ _ ___| | | _ \_ _ ___ _ __  ___ _ _| |_(_)___ ___
    //  | || ' \ V / _` / _` / -_) '_| | (__/ _ \ ' \  _| '_/ _ \ | |  _/ '_/ _ \ '_ \/ -_) '_|  _| / -_|_-<
    // |___|_||_\_/\__,_\__,_\___|_|    \___\___/_||_\__|_| \___/_| |_| |_| \___/ .__/\___|_|  \__|_\___/__/
    //                                                                          |_|                         

    /// <summary>
    /// When this is set to true the reference alien when processed (#0) will move down causing all the others to move down on their next animation because
    /// they are all drawn relative to the reference alien.
    /// </summary>
    private bool alienStepDown = false;

    /// <summary>
    /// Invaders have 2 two frames. This is used to determine which frame to draw.
    /// </summary>
    private int frame;

    /// <summary>
    /// How many Space Invaders are remaining. Initially = 5 rows of 11.
    /// </summary>
    private int spaceInvadersRemaining = 55;

    /// <summary>
    /// How many bullets the player has avoided.
    /// </summary>
    internal int BulletsAvoided;

    /// <summary>
    /// Each time an alien is dead we add it to this dictionary. The key is the alien index, the value is the number of frames it stays "exploding" for#
    /// </summary>
    private readonly Dictionary<int /*alien-index*/, int /*count*/> deadAliens = new();

    /// <summary>
    /// Returns true if there is at least one alien exploding - used to stop user firing when an alien is exploding.
    /// </summary>
    internal bool AlienExploding
    {
        get
        {
            return deadAliens.Count > 0;
        }
    }

    /// <summary>
    /// This will contain our array of 5 rows of 11 invaders. We number them as follows, in a seemingly 
    /// upside down manner - to be consistent with the original.
    /// [0] is the reference alien. It doesn't matter whether that alien is alive or not, it's the reference 
    /// and drawing is done relative to it. We don't use it to detect edge collisions, unless it is alive.
    /// 
    ///  44 45 46 47 48 49 50 51 52 53 54
    ///  33 34 35 36 37 38 39 40 41 42 43
    ///  22 23 24 25 26 27 28 29 30 31 32
    ///  11 12 13 14 15 16 17 18 19 20 21
    ///   0  1  2  3  4  5  6  7  8  9 10             
    /// </summary>
    internal readonly SpaceInvader.InvaderState[] InvadersAliveState = new SpaceInvader.InvaderState[55];

    /// <summary>
    /// On the original, the aliens are drawn relative to the reference alien. This is the reference alien.
    /// i.e the each frame this reference changes. It's a strange approach, but to be fair making them all 
    /// move in the way they do without has some headaches avoided by doing this.
    /// </summary>
    internal Point referenceAlien;

    /// <summary>
    /// When aliens move, the reference alien is used to determine where the other aliens are drawn. When it comes 
    /// to erasing the aliens, the location they were drawn differs from the reference alien. So we preserve it in this variable.
    /// </summary>
    internal Point lastReferenceAlien;

    /// <summary>
    /// This is the next Space Invader to be rippled (moved).
    /// </summary>
    private int spaceInvaderToRippleNext = -1;

    /// <summary>
    /// Returns invaders remaining, and sets the OneOrMoreSpaceInvaderRemaining flag.
    /// </summary>
    internal int SpaceInvadersRemaining
    {
        get
        {
            return spaceInvadersRemaining;
        }

        set
        {
            if (spaceInvadersRemaining == value) return;

            spaceInvadersRemaining = value;

            // by default there are invaders.
            if (spaceInvadersRemaining == 0)
            {
                OneOrMoreSpaceInvaderRemaining = false;
            }
        }
    }

    /// <summary>
    /// Direction the Space Invaders are travelling (1 = left to right, -1 = right to left). To be precise, this is the
    /// amount added to the reference alien's X coordinate.
    /// </summary>
    private int XDirection;

    /// <summary>
    /// Returns the direction and speed of the invaders. Used by the AI.
    /// </summary>
    public int DirectionAndSpeedOfInvaders
    {
        get { return XDirection; }
    }

    /// <summary>
    /// This indicates whether there are Space Invaders remaining. It saves counting.
    /// </summary>
    internal bool OneOrMoreSpaceInvaderRemaining;

    /// <summary>
    /// This indicates whether the aliens reached the bottom of the screen (game over)
    /// </summary>
    internal bool SpaceInvadersReachedBottom = false;
    #endregion

    #region INVADER FIRING PROPERTIES
    /// <summary>
    /// State of the invader firing.
    /// </summary>
    enum InvaderFiringState { NotFiring, Firing, ReloadPause };

    /// <summary>
    /// When set to false, the firing mechanism is disabled.
    /// </summary>
    internal bool AliensAreAllowedToFire = false;

    ///   ___     _ _ _             ___      _ _     _
    ///  | _ \___| | (_)_ _  __ _  | _ )_  _| | |___| |_
    ///  |   / _ \ | | | ' \/ _` | | _ \ || | | / -_)  _|
    ///  |_|_\___/_|_|_|_||_\__, | |___/\_,_|_|_\___|\__|
    ///                     |___/                        

    /// <summary>
    /// How long it has to pause before firing the rolling bullet.
    /// </summary>
    private int rollingBulletReloadPause;

    /// <summary>
    /// This is set to stop the invader firing immediately (think reloading pause).
    /// </summary>
    private InvaderFiringState rollingBulletFiringState = InvaderFiringState.NotFiring;

    /// <summary>
    /// Object "2" in the original is the rolling bullet.
    /// </summary>
    private SpaceInvaderRollingBullet? object2RollingSpaceInvaderBullet;

    /// <summary>
    /// When true it skips the rolling bullet.
    /// </summary>
    private bool SkipRollingBullet = false;

    ///
    ///   ___            _           _        ___      _ _     _   
    ///  / __| __ _ _  _(_)__ _ __ _| |_  _  | _ )_  _| | |___| |_ 
    ///  \__ \/ _` | || | / _` / _` | | || | | _ \ || | | / -_)  _|
    ///  |___/\__, |\_,_|_\__, \__, |_|\_, | |___/\_,_|_|_\___|\__|
    ///          |_|      |___/|___/   |__/                        

    /// <summary>
    /// How long it has to pause before firing the squiggly bullet.
    /// </summary>
    private int squigglyBulletReloadPause;

    /// <summary>
    /// When a squiggly is selected, it sequences through SpaceInvaderSpecification.s_squigglyShotColumn to determine which column.
    /// </summary>
    private int squigglyBulletIndex;

    /// <summary>
    /// This is set to stop the invader firing immediately (think reloading pause).
    /// </summary>
    private InvaderFiringState squigglyBulletFiringState = InvaderFiringState.NotFiring;

    /// <summary>
    /// Object "4" in the original is the squiggly bullet. The saucer isn't allowed to appear if a squiggly bullet has been fired because they share the same timer / movement. 
    /// Because of slow CPU speed of the 8080, it would cause a noticeable slow down if both were in play at the same time.
    /// </summary>
    private SpaceInvaderSquigglyBullet? object4SquigglySpaceInvaderBullet;

    /// <summary>
    /// The saucer isn't allowed to appear if a squiggly bullet has been fired.
    /// </summary>
    internal bool SquigglyBulletInUse
    {
        get
        {
            return object4SquigglySpaceInvaderBullet is not null;
        }
    }

    ///   ___ _                          ___      _ _     _
    ///  | _ \ |_  _ _ _  __ _ ___ _ _  | _ )_  _| | |___| |_
    ///  |  _/ | || | ' \/ _` / -_) '_| | _ \ || | | / -_)  _|
    ///  |_| |_|\_,_|_||_\__, \___|_|   |___/\_,_|_|_\___|\__|
    ///                  |___/                                
    /// 

    /// <summary>
    /// When a plunger is selected, it sequences through SpaceInvaderSpecification.s_plungerShotColumn to determine which column.
    /// It is initialised (in the constructor) to the end of the table, so that upon first use it increments to "0" and picks the first column.
    /// Original 
    /// pluShotCFirLSB	Pointer to column-firing table LSB
    //  pluShotCFirMSB Pointer to column-firing table MSB
    /// </summary>
    private int plungerBulletIndex;

    /// <summary>
    /// How long it has to pause before firing the plunger bullet.
    /// </summary>
    private int plungerBulletReloadPause;

    /// <summary>
    /// This is set to stop the invader firing immediately (think reloading pause).
    /// </summary>
    private InvaderFiringState plungerBulletFiringState = InvaderFiringState.NotFiring;

    /// <summary>
    /// Object "3" in the original is the plunger bullet.
    /// </summary>
    private SpaceInvaderPlungerBullet? object3PlungerSpaceInvaderBullet;

    // SHARED LOGIC

    /// <summary>
    /// Returns the bullets that are currently in play, as an IEnumerable.
    /// </summary>
    internal IEnumerable<SpaceInvaderBulletBase> Bullets
    {
        get
        {
            if (object2RollingSpaceInvaderBullet is not null) yield return object2RollingSpaceInvaderBullet;
            if (object3PlungerSpaceInvaderBullet is not null) yield return object3PlungerSpaceInvaderBullet;
            if (object4SquigglySpaceInvaderBullet is not null) yield return object4SquigglySpaceInvaderBullet;
        }
    }

    /// <summary>
    /// Returns the bullets that are currently in play, as an array. Where a bullet is not in play, the array element is set to (0,0).
    /// </summary>
    /// <returns></returns>
    internal Point[] BulletsInAIFormat()
    {
        Point[] result = new Point[3];

        result[0] = object2RollingSpaceInvaderBullet is not null ? object2RollingSpaceInvaderBullet.Position : new Point(0, 0);
        result[1] = object3PlungerSpaceInvaderBullet is not null ? object3PlungerSpaceInvaderBullet.Position : new Point(0, 0);
        result[2] = object4SquigglySpaceInvaderBullet is not null ? object4SquigglySpaceInvaderBullet.Position : new Point(0, 0);

        return result;
    }

    #endregion

    /// <summary>
    /// This is the video display the controller paints then invader sprites and bullets to.
    /// </summary>
    private readonly VideoDisplay videoScreen;

    /// <summary>
    /// Constructor.
    /// We have 5 rows of 11 aliens. When an "edge" alien hits the walls, the whole flock of aliens moves down and reverse direction, one by one.
    /// </summary>
    /// <param name="screen"></param>
    /// <param name="level"></param>
    internal SpaceInvaderController(VideoDisplay screen, int level)
    {
        Debug.Assert(level > 0, "Levels start at 1");

        videoScreen = screen; // store it for drawing

        // true to the original each level starts with aliens at a lower position, until it's scarily near the bottom of the screen. Then it starts again.
        int[] levelOffset = new int[] { 24, 40, 48, 48, 48, 56, 56, 56 };
        //                              #2  #3  #4  #5  #6  #7  #8  #9

        // level 1 is to start the player off easy with aliens very high up, offset 0.
        // Then it gets progressively harder - level 2 is 24px lower, level 3 is 40, level 4 is 48, level 5 is 48, level 6 is 48, level 7 is 56, level 8 is 56, level 9 is 56
        // It starts again with level 10 at lower, level 11 is 40, level 12 is 48, level 13 is 48, level 14 is 48, level 15 is 56, level 16 is 56, level 17 is 56

        // tbh, I was uncomfortable with this. 24 seems a massive leap downwards. However, I've checked 2 videos of the original and it's correct.
        // https://www.youtube.com/watch?v=uGjgxwiemms
        int offsetY = level == 1 ? 0 : levelOffset[(level - 2) % levelOffset.Length];

        /*
          The explanation for the above table is as follows, referring to the original source code.

          From the original EEPROM:
            07EA: 21 78 38        LD      HL,$3878            ; Screen coordinates for lower-left alien
            07ED: 22 FC 21        LD      (p1RefAlienY),HL    ; Initialize reference alien for player 1
            
          This is where it messes with the brain. The video screen on the original is rotated 90 degrees clockwise. So what is X and Y become
          all the more confusing as Y is meant to describe vertical, which in this case is horizontal thanks to rotation.

          Also note in the original "0,0" bottom left is Cartesian based (depending on orientation), where as in a bitmap "0,0" is top left, i.e. inverted vertically.

            $38 = 56 << x
            $78 = 120 << y

          What makes it more confusing is the commentary that the reference alien is bottom left. Is that bottom left with orientation such that
          invaders are at the top, OR in rotated form where bottom left invader would be the bottom right previously?

          Topher's disassembly uses Yr meaning rotated units, and Yn meaning non rotated. 
        
          ;##-AlienStartTable
          ; Starting Y coordinates for aliens at beginning of rounds. The first round is initialized to $78 at 07EA.
          ; After that this table is used for 2nd, 3rd, 4th, 5th, 6th, 7th, 8th, and 9th. The 10th starts over at 1DA3.
            1DA3: 60                                    
            1DA4: 50                                    
            1DA5: 48                                    
            1DA6: 48                                    
            1DA7: 48                                    
            1DA8: 40                                    
            1DA9: 40                                    
            1DAA: 40
         */

        // This suggests at the start, the "y" of the reference alien is 0x78, which is 120 in decimal.
        // The screen is inverted, so actually y = 256 - 120 = 136.

        // If each alien is 8px high, and spaced with +8 in between (16px, as is born out from comparing YouTube real footage to mine), then
        // our bottom position from which we subtract (0,16,32...) is calculated as 16x4+54 = 128 (top of sprite), not 136.
        // But they are 8 tall and drawn inverted. So 136-8 = 128. I believe is correct. We draw downwards, the original draws upwards.

        // x="23" comes from comparing the original video footage to my own and adjusting mine until they overlay precisely. I find this
        // confusing as the original has "56" which is (a) double (b) far to right.
        referenceAlien = new(23, (SpaceInvader.Dimensions.Height + 8) * 4 + offsetY + 64); // = (23, 128+offset)
        lastReferenceAlien = new(referenceAlien.X, referenceAlien.Y);

        // wrap around to item 0 occurs, when we next attempt to find one.
        squigglyBulletIndex = OriginalDataFrom1978.s_squigglyShotColumn.Length - 1;
        plungerBulletIndex = OriginalDataFrom1978.s_plungerShotColumn.Length - 1;

        OneOrMoreSpaceInvaderRemaining = true; // we have 55 alive if we want to be specific

        XDirection = 1; // start moving right

        /*
         *  Invader positions based on spaceInvaderToRippleNext index.
         *  
         *   44 45 46 47 48 49 50 51 52 53 54
         *   33 34 35 36 37 38 39 40 41 42 43
         *   22 23 24 25 26 27 28 29 30 31 32
         *   11 12 13 14 15 16 17 18 19 20 21
         *   [0] 1  2  3  4  5  6  7  8  9 10  __ offset affects [0].
         *    ^reference alien
         */

        for (int i = 0; i < 55; i++)
        {
            InvadersAliveState[i] = SpaceInvader.InvaderState.alive; // we will draw, if alive
        }
    }

    /// <summary>
    /// The alien "fire rate" is based on the number of steps the other two shots on the screen
    /// have made.The smallest number-of-steps is compared to the reload-rate. If it is too
    /// soon then no shot is made.The reload-rate is based on the player's score. The MSB
    /// is looked up in a table to get the reload-rate. The smaller the rate the faster the
    /// aliens fire. Setting rate this way keeps shots from walking on each other.
    /// </summary>
    /// <param name="spaceInvaderBullet"></param>
    /// <param name="reloadPauseInterval"></param>
    /// <returns></returns>
    private bool ReloadAchieved(SpaceInvaderBulletBase? spaceInvaderBullet, int reloadPauseInterval)
    {
        int min = int.MaxValue;

        // check squiggly to find the minimum number of steps
        if (object4SquigglySpaceInvaderBullet is not null &&
            object4SquigglySpaceInvaderBullet != spaceInvaderBullet &&
            object4SquigglySpaceInvaderBullet.Steps != -1 &&
            object4SquigglySpaceInvaderBullet.Steps < min)
        {
            min = object4SquigglySpaceInvaderBullet.Steps;
        }

        // check plunger to find the minimum number of steps
        if (object3PlungerSpaceInvaderBullet is not null &&
            object3PlungerSpaceInvaderBullet != spaceInvaderBullet &&
            object3PlungerSpaceInvaderBullet.Steps != -1 &&
            object3PlungerSpaceInvaderBullet.Steps < min)
        {
            min = object3PlungerSpaceInvaderBullet.Steps;
        }

        // check rolling to find the minimum number of steps
        if (object2RollingSpaceInvaderBullet is not null &&
            object2RollingSpaceInvaderBullet != spaceInvaderBullet &&
            object2RollingSpaceInvaderBullet.Steps != -1 &&
            object2RollingSpaceInvaderBullet.Steps < min)
        {
            min = object2RollingSpaceInvaderBullet.Steps;
        }

        // does min exceed the reload rate? (or are no bullets on screen?)
        return min > reloadPauseInterval || min == int.MaxValue;
    }

    /// <summary>
    /// Increments each time a reload is complete, and depending on which one it is, a different bullet type is fired.
    /// </summary>
    private int bulletTypeSelectionTimer = -1;

    /// <summary>
    /// Fires a bullet, if one is not already in motion.
    /// </summary>
    /// <param name="score"></param>
    /// <param name="playerCentreX"></param>
    /// <param name="saucer"></param>
    internal void FireIfBulletNotInMotion(int score, int playerCentreX, bool saucer)
    {
        // while assembling the aliens, they are not allowed to fire
        if (!AliensAreAllowedToFire) return;

        /*
         * ; The task-timer at 2032 is copied to 2080 in the game loop. The flag is used as a
         * ; synchronization flag to keep all the shots processed on separate interrupt ticks. This
         * ; has the main effect of slowing the shots down.
         *  
         * ; When the timer is 2 the squiggly-shot/saucer (object 4) runs.
         * ; When the timer is 1 the plunger-shot (object 3) runs.
         * ; When the timer is 0 this object, the rolling-shot, runs.
         * 
         */
        // pick them in order
        bulletTypeSelectionTimer = (bulletTypeSelectionTimer + 1) % 3;

        switch (bulletTypeSelectionTimer)
        {
            case 2:
                FireSquigglyBulletIfNotInMotion(score, saucer);
                break;

            case 1:
                FirePlungerBulletIfNotInMotion(score);
                break;

            case 0:
                FireRollingBulletIfNotInMotion(score, playerCentreX);
                break;
        }
    }

    /// <summary>
    /// If a bullet exists, we cannot fire another.
    /// If a bullet doesn't exist, pick a random invader and fire from the bottom invader in that column (where they are stacked).
    /// Consistent with the 1978 original, we have a reload pause that varies with score.
    /// </summary>
    /// <param name="score"></param>
    /// <param name="playerCentreX"></param>
    internal void FireRollingBulletIfNotInMotion(int score, int playerCentreX)
    {
        // bullet is in motion, we cannot fire
        switch (rollingBulletFiringState)
        {
            case InvaderFiringState.Firing:
                return; // nothing to do.

            case InvaderFiringState.NotFiring:
                ApplyAReloadPauseBetweenFiring(score, ref rollingBulletReloadPause);

                rollingBulletFiringState = InvaderFiringState.ReloadPause;

                // now it will pause, before firing
                return;

            case InvaderFiringState.ReloadPause:
                if (!ReloadAchieved(object2RollingSpaceInvaderBullet, rollingBulletReloadPause)) return;
                break;
        }

        // The first shot from rolling is blocked.
        SkipRollingBullet = !SkipRollingBullet;

        // ..  a flag to have the shot skip its first
        // attempt at firing every time it is reinitialized (when it blows up).
        if (SkipRollingBullet)
        {
            object2RollingSpaceInvaderBullet = null;
            rollingBulletFiringState = InvaderFiringState.NotFiring;
            return;
        }

        // ; Start a shot right over the player
        // 061B: 3A 1B 20        LD      A,(playerXr)        ; Player's X coordinate
        // 061E: C6 08           ADD     A,$08               ; Center of player
        // 0620: 67              LD      H,A                 ; To H for routine
        // 0621: CD 6F 15        CALL    FindColumn          ; Find the column
        // 0624: 79              LD      A,C                 ; Get the column right over player
        // 0625: FE 0C           CP      $0C                 ; Is it a valid column?
        // 0627: DA A5 05        JP      C,$05A5             ; Yes ... use what we found
        // 062A: 0E 0B           LD      C,$0B               ; Else use ...
        // 062C: C3 A5 05        JP      $05A5               ; ... as far over as we can

        Dictionary<int, int> spaceInvaderIndexedByColumn = GetBottomInvaderForEachLiveColumn();

        // this happens briefly when all are killed.
        if (spaceInvaderIndexedByColumn.Count == 0)
        {
            return; // no alien to shoot.
        }

        int bestDistance = int.MaxValue; // we want the closest alien to the player
        Point invaderToFire = new ();

        // check each invader in a column
        foreach (int col in spaceInvaderIndexedByColumn.Keys)
        {
            SpaceInvader.GetPosition(referenceAlien, spaceInvaderIndexedByColumn[col], out int _, out int _, out int X, out int Y);
            X += SpaceInvader.Dimensions.Width / 2; // middle of the invader

            int distance = Math.Abs(playerCentreX - X);

            if (distance < bestDistance)
            {
                // we fire from one at the bottom of the stack, so invaders don't shoot thru one that is lower.
                // we are close to the player, so we fire at the player.
                bestDistance = distance;
                invaderToFire = new Point(X, Y);
            }
        }

        // we will always pick one, because (at least) one is alive.

#if FireFromLastInvader
        // personally, I _really_ don't like this. I would delete this block and have it fire from the closest invader, in the hope the player will
        // be cornered or inadvertently get hit by it. But this is meant to simulate the original game, so I will leave it as is.
        if (bestDistance > 8)
        {
            // 062A: 0E 0B           LD      C,$0B               ; Else use ...
            // 062C: C3 A5 05        JP      $05A5               ; ... as far over as we can

            // we traverse left to right along the bottom most row, going upwards. If we destroy one from the bottom, then that
            // column will be included later. We want the LAST column in order.
            List<int> columns = new(spaceInvaderIndexedByColumn.Keys);
            columns.Sort(); // ascending

            // right most column.
            SpaceInvader.GetPosition(referenceAlien, spaceInvaderIndexedByColumn[columns[^1]], out int _, out int _, out int X, out int Y);
            X += SpaceInvader.Dimensions.Width / 2; // middle of the invader
            invaderToFire = new Point(X, Y);
        }
#endif

        // now we have a bullet. Starts horizontal middle of space invader, and below it.
        object2RollingSpaceInvaderBullet = new SpaceInvaderRollingBullet(videoScreen, invaderToFire.X, invaderToFire.Y + SpaceInvader.Dimensions.Height);

        rollingBulletFiringState = InvaderFiringState.Firing;
    }

    /// <summary>
    /// If a bullet exists, we cannot fire another.
    /// If a bullet doesn't exist, pick a random invader and fire from the bottom invader in that column (where they are stacked).
    /// Squiggly bullets are not fired if the saucer is on screen
    /// </summary>
    /// <param name="score"></param>
    /// <param name="saucerIsOnscreen"></param>
    private void FireSquigglyBulletIfNotInMotion(int score, bool saucerIsOnscreen)
    {
        if (saucerIsOnscreen) return; // sauce and bullet share the same object on the original, and cannot be onscreen at the same time.

        // bullet is in motion, we cannot fire
        switch (squigglyBulletFiringState)
        {
            case InvaderFiringState.Firing:
                return;

            case InvaderFiringState.NotFiring:
                ApplyAReloadPauseBetweenFiring(score, ref squigglyBulletReloadPause);

                squigglyBulletFiringState = InvaderFiringState.ReloadPause;

                // now it will pause, before firing
                return;

            case InvaderFiringState.ReloadPause:
                if (!ReloadAchieved(object4SquigglySpaceInvaderBullet, squigglyBulletReloadPause)) return;
                break;
        }

        Dictionary<int, int> spaceInvaderIndexedByColumn = GetBottomInvaderForEachLiveColumn();

        // this happens briefly when all are killed.
        if (spaceInvaderIndexedByColumn.Count == 0)
        {
            return; // no alien to shoot
        }

        // cycle thru squiggly table
        squigglyBulletIndex = (squigglyBulletIndex + 1) % OriginalDataFrom1978.s_squigglyShotColumn.Length;

        int squigglyColumn = OriginalDataFrom1978.s_squigglyShotColumn[squigglyBulletIndex];

        if (spaceInvaderIndexedByColumn.TryGetValue(squigglyColumn, out int invaderCol))
        {
            // we fire from one at the bottom of the stack, so invaders don't shoot thru one that is lower.
            SpaceInvader.GetPosition(referenceAlien, invaderCol, out int row, out _, out int X, out int Y);

            X += SpaceInvader.Dimensions.Width / 2;
            X += ((row != 0) ? 1 : 0) + ((row == 4) ? 1 : 0); // adjust because SI's vary in width

            // Debug.Assert(X >= 0 && X < 224, "X is out of range");

            // now we have a bullet. Starts horizontal middle of space invader, and below it.
            object4SquigglySpaceInvaderBullet = new SpaceInvaderSquigglyBullet(videoScreen, X, Y + SpaceInvader.Dimensions.Height);
        }

        if (object4SquigglySpaceInvaderBullet is null)
        {
            squigglyBulletFiringState = InvaderFiringState.NotFiring;
            return; // skip this go, no alien is near enough to the player
        }

        squigglyBulletFiringState = InvaderFiringState.Firing;
    }

    /// <summary>
    /// The invaders are stacked in columns, so we can find the bottom one in each column.
    /// Bullets are fired from the bottom invader in each column.
    /// </summary>
    /// <returns></returns>
    private Dictionary<int, int> GetBottomInvaderForEachLiveColumn()
    {
        // get firing positions

        //    col   col     col
        //     0      1      2
        //   /<O>\  /<O>\  /<O>\
        //   /<O>\    ¦    /<O>\
        //   /<O>\           ¦
        //     ¦
        //
        // "¦" indicates the bullet will be fired from the bottom invader in that column.

        Dictionary<int, int> spaceInvaderIndexedByColumn = new();

        HashSet<int> columnsWithInvaders = new();

        // read all the invaders and stack into columns, top down.

        for (int i = 0; i < 55; i++)
        {
            if (InvadersAliveState[i] == SpaceInvader.InvaderState.alive)
            {
                SpaceInvader.GetPosition(referenceAlien, i, out _, out int col, out int _, out int Y);

                if (columnsWithInvaders.Contains(col + 1)) continue; // if we already found an invader in this column, skip this one

                columnsWithInvaders.Add(col + 1); // record we found an invader in this column

                // the row before ground is a no fire row.
                if (Y > OriginalDataFrom1978.s_PlayerShipStartLocation.Y - 8 /*<- top of player */ - SpaceInvader.Dimensions.Height)
                {
                    continue;
                }

                spaceInvaderIndexedByColumn.Add(col + 1, i);

                // there are ONLY 11 columns of invaders, so we can stop when we have them all.
                if (spaceInvaderIndexedByColumn.Count == 11) break;
            }
        }

        return spaceInvaderIndexedByColumn;
    }

    /// <summary>
    /// For an element of skill, TAITO varied the fire-rate as the score increases.
    /// </summary>
    /// <param name="score"></param>
    /// <param name="reloadPause"></param>
    private static void ApplyAReloadPauseBetweenFiring(int score, ref int reloadPause)
    {
        // I messed this up first go. The assembler uses binary coded decimal to store the score...

        // ReloadCounter starts when the shot is dropped and counts up with each step as it falls.
        // The game keeps a constant reload rate that determines how fast the aliens can fire. The
        // game takes the smallest count of the other two shots and compares it
        // to the reload rate. If it is too soon since the last shot then no shot is fired.

        // The reload rate gets faster as the game progresses. The code uses the upper two digits of the player's score to set the reload rate.
        // < 200        => 48 (0x30)
        //  200 to 1000 => 16 (0x10)
        // 1000 to 2000 => 11 (0x0B)
        // 2000 to 3000 => 8
        // 3000 => 7. With a little flying-saucer-luck you will reach 3000 in 2 or 3 racks.

        // Assembly code for this is is at $170E
        /*
            AShotReloadRate:
            ; Use the player's MSB to determine how fast the aliens reload their
            ; shots for another fire.
            170E: CD CA 09        CALL    $09CA               ; Get score descriptor for active player
            1711: 23              INC     HL                  ; MSB value
            1712: 7E              LD      A,(HL)              ; Get the MSB value
            1713: 11 B8 1C        LD      DE,$1CB8            ; Score MSB table
            1716: 21 A1 1A        LD      HL,$1AA1            ; Corresponding fire reload rate table
            1719: 0E 04           LD      C,$04               ; Only 4 entries (a 5th value of 7 is used after that)
            171B: 47              LD      B,A                 ; Hold the score value
            171C: 1A              LD      A,(DE)              ; Get lookup from table
            171D: B8              CP      B                   ; Compare them
            171E: D2 27 17        JP      NC,$1727            ; Equal or below ... use this table entry
            1721: 23              INC     HL                  ; Next ...
            1722: 13              INC     DE                  ; ... entry in table
            1723: 0D              DEC     C                   ; Do all ...
            1724: C2 1C 17        JP      NZ,$171C            ; ... 4 entries in the tables
            1727: 7E              LD      A,(HL)              ; Load the shot reload value
            1728: 32 CF 20        LD      (aShotReloadRate),A ; Save the value for use in shot routine
            172B: C9              RET                         ; Done
         */

        // AReloadScoreTab:
        // ; The tables at 1CB8 and 1AA1 control how fast shots are created.The speed is based
        // ; on the upper byte of the player's score. For a score of less than or equal 0200 then
        // ; the fire speed is 30. For a score less than or equal 1000 the shot speed is 10. Less
        // ; than or equal 2000 the speed is 0B. Less than or equal 3000 is 08. And anything
        // ; above 3000 is 07.
        // 
        // 1AA1: 30 10 0B 08
        // 1AA5: 07; Fastest shot firing speed
        // 1CB8: 02 10 20 30  

        int scoreMSB = score / 100; // not 256, this is BCD (binary coded decimal)

        if (scoreMSB < 2)  // < 200
        {
            reloadPause = 48; //0x30
        }
        else
        if (scoreMSB < 10) // < 1000
        {
            reloadPause = 16; //0x10
        }
        else
        if (scoreMSB < 20)  // < 2000
        {
            reloadPause = 11; //0x0B
        }
        else
        if (scoreMSB < 30) // < 3000
        {
            reloadPause = 8;
        }
        else
        {
            reloadPause = 7;
        }
    }

    /// <summary>
    /// If a bullet exists, we cannot fire another.
    /// If a bullet doesn't exist, pick a random invader and fire from the bottom invader in that column (where they are stacked).
    /// </summary>
    /// <param name="score"></param>
    private void FirePlungerBulletIfNotInMotion(int score)
    {
        //  One alien left? Skip plunger shot?
        // 00 206E	skipPlunger	When there is only one alien left this goes to 1 to disable the plunger-shot when it ends
        if (spaceInvadersRemaining == 1) return;

        // bullet is in motion, we cannot fire
        switch (plungerBulletFiringState)
        {
            case InvaderFiringState.Firing:
                return;

            case InvaderFiringState.NotFiring:
                ApplyAReloadPauseBetweenFiring(score, ref plungerBulletReloadPause);

                plungerBulletFiringState = InvaderFiringState.ReloadPause;

                // now it will pause, before firing
                return;

            case InvaderFiringState.ReloadPause:
                if (!ReloadAchieved(object3PlungerSpaceInvaderBullet, plungerBulletReloadPause)) return;
                break;
        }

        Dictionary<int, int> spaceInvaderIndexedByColumn = GetBottomInvaderForEachLiveColumn();

        // this happens briefly when all are killed.
        if (spaceInvaderIndexedByColumn.Count == 0)
        {
            return; // no alien to shoot
        }

        // cycle thru plunger table
        plungerBulletIndex = (plungerBulletIndex + 1) % OriginalDataFrom1978.s_plungerShotColumn.Length;

        int plungerColumn = OriginalDataFrom1978.s_plungerShotColumn[plungerBulletIndex];

        if (spaceInvaderIndexedByColumn.TryGetValue(plungerColumn, out int invaderCol))
        {
            SpaceInvader.GetPosition(referenceAlien, invaderCol, out int row, out _, out int X, out int Y);
            X += SpaceInvader.Dimensions.Width / 2;
            X += ((row != 0) ? 1 : 0) + ((row == 4) ? 1 : 0); // adjust because SI's vary in width

            Debug.Assert(X >= 0 && X < 224, "X is out of range");

            // now we have a bullet. Starts horizontal middle of space invader, and below it.
            object3PlungerSpaceInvaderBullet = new SpaceInvaderPlungerBullet(videoScreen, X, Y + SpaceInvader.Dimensions.Height);
        }

        if (object3PlungerSpaceInvaderBullet is null)
        {
            plungerBulletFiringState = InvaderFiringState.NotFiring;
            return; // skip this go, no alien is near enough to the player
        }

        plungerBulletFiringState = InvaderFiringState.Firing;
    }

    /// <summary>
    /// Moves the Space Invaders..
    /// The more aliens there are on the screen the longer it takes to get back around to moving the reference alien. 
    /// At the start of the round there are 55 alien - almost 1 second to move the entire rack. 
    /// At the end of the round there is only one alien left - it moves 2 pixels left or 3 pixels right about 60 times a second.
    /// </summary>
    internal void Move()
    {
        // when an alien dies, we pause briefly. Initially I had one variable to track. But it is possible to kill multiple before the timer has elapse.
        // so we keep them in an array, removing them when their timer has elapsed.
        if (deadAliens.Count > 0)
        {
            RemoveDeadAliensIfTimeIsUp();

            // if there are still exploding aliens, we don't move the aliens
            if (deadAliens.Count > 0) return;
        }

        // work out which alien we need to move in this frame.
        DetermineNextInvaderToRippleTo();

        // remove the alien before moving
        SpaceInvader.EraseSprite(videoScreen, spaceInvaderToRippleNext, InvadersAliveState[spaceInvaderToRippleNext], lastReferenceAlien, frame);

        // find out where the alien is now. Its position is relative to the reference alien, and the reference alien will have moved, so we need to move this alien.
        SpaceInvader.GetPosition(referenceAlien, spaceInvaderToRippleNext, out int row, out _, out int X, out int Y);

        // detect aliens reached the bottom.
        if (!SpaceInvadersReachedBottom && Y > OriginalDataFrom1978.s_PlayerShipStartLocation.Y - SpaceInvader.Dimensions.Height)
        {
            SpaceInvadersReachedBottom = true; // game over...
        }

        // Check if any aliens have reached the edge of the screen.
        // If an invader hits the edges, we need to go down, and reverse.
        // MoveReferenceAlienHorizontally()
        // * (spaceInvadersRemaining == 1) => 3 pixels right, 2 pixels left
        // * (spaceInvadersRemaining != 1) => 2 pixels right, 2 pixels left

        /*
             row 0/1 invaders
                    |      ████      
                    |   ██████████   
                    |  ████████████  
                    |  ███  ██  ███  
                    |  ████████████  
                    |     ██  ██     
                    |    ██ ██ ██    
                    |  ██        ██  
                     0123456789012345
                       ^          ^   
                       +2         +13
                    
             row 2/3 invaders
                    |     █     █    
                    |   █  █   █  █  
                    |   █ ███████ █  
                    |   ███ ███ ███  
                    |   ███████████  
                    |    █████████   
                    |     █     █    
                    |    █       █   
                     0123456789012345
                        ^         ^  
                        +3        +13 

             row 4 invader       
                    |       ██       
                    |      ████      
                    |     ██████     
                    |    ██ ██ ██    
                    |    ████████    
                    |      █  █      
                    |     █ ██ █     
                    |    █ █  █ █    
                     0123456789012345
                         ^      ^
                         +4     +11

                 */

        
        int xLeft = X + xLeftAdjustment[row] - XDirection;
        int xRight = X + xRightAdjustment[row] + XDirection;

        // RackBump: @$1597, it uses pixels, I compute using offsets. row=0, is the bottom row.
        // If moving right, and the rightmost invader is at the right edge of the screen [did we detect a pixel in X = 213, from Y = 39 to 223?]
        // If moving left, and the leftmost invader is at the left edge of the screen [did we detect a pixel in X = 9, from Y = 39 to 223?]
        if ((xRight > 213 && XDirection > 0) || (xLeft < 9 && XDirection < 0))
        {
            // this indicates that when we reach the reference alien [0], that it needs to start the ripple of aliens 1-54 to move down. 
            // refAlienDYr = rackDownDelta (-8), or +8, as our non Cartesian bitmaps work.
            alienStepDown = true;
        }
    }

    /// <summary>
    /// Alien explosion stays visible for a few frames after they are killed. This method removes them from the screen when their time is up.
    /// </summary>
    private void RemoveDeadAliensIfTimeIsUp()
    {
        // we have one or more exploding aliens.
        List<int> listOfExplosionsToRemove = new();
        foreach (int deadAlien in deadAliens.Keys)
        {
            // decrement counter, when it reaches zero, we remove it from the screen and make it "dead".
            if (--deadAliens[deadAlien] == 0)
            {
                listOfExplosionsToRemove.Add(deadAlien);

                SpaceInvader.EraseSprite(videoScreen, deadAlien, InvadersAliveState[deadAlien], deadAlien > spaceInvaderToRippleNext ? lastReferenceAlien : referenceAlien, deadAlien > spaceInvaderToRippleNext ? frame : 1 - frame);
                InvadersAliveState[deadAlien] = SpaceInvader.InvaderState.dead;
            }
        }

        // we cannot remove from the dictionary while we are iterating over it, so we keep a list of the ones to remove, and then remove them.
        foreach (int deadAlien in listOfExplosionsToRemove)
        {
            deadAliens.Remove(deadAlien);
        }
    }

    /// <summary>
    /// This iterates through the aliens, and determines which alien is next to be moved. We move one alien per frame and
    /// this gives it the ripple effect. 
    /// True to the original we use the reference alien for drawing all the aliens in the correct position.
    /// </summary>
    private void DetermineNextInvaderToRippleTo()
    {
        /*
         *  Invader positions based on spaceInvaderToRippleNext index.
         *  
         *   44 45 46 47 48 49 50 51 52 53 54
         *   33 34 35 36 37 38 39 40 41 42 43
         *   22 23 24 25 26 27 28 29 30 31 32
         *   11 12 13 14 15 16 17 18 19 20 21
         *  [0] 1  2  3  4  5  6  7  8  9 10
         *   ^ reference alien ALWAYS.
         */
        bool moved = false;

        do
        {
            ++spaceInvaderToRippleNext;

            if (spaceInvaderToRippleNext > 54) // aliens are 0..54, this indicates we have moved all aliens and are at "0" the reference alien.
            {
                // store the reference alien, so we can erase it next frame.
                lastReferenceAlien = new(referenceAlien.X, referenceAlien.Y);

                // reset the index to the reference alien, element [0].
                spaceInvaderToRippleNext = 0;

                // The game does a nasty trick to the timing when there is one alien left. 
                // Instead of moving 2 pixels both directions the alien moves 2 pixels at a time to the left but 3 pixels at a time to the right.
                // The last little alien is faster going right than it is going left.
                // This changes the timing up just enough to make it difficult to lead the advancing alien with a shot.

                MoveReferenceAlienHorizontally();

                // toggle the alien frame for the animation
                frame = 1 - frame;

                if (alienStepDown)
                {
                    alienStepDown = false;

                    // Move aliens down one level and change direction 
                    XDirection = -XDirection; // direction is same for all aliens

                    // Why 2?
                    MoveReferenceAlienHorizontally(); // this undoes the collision
                    MoveReferenceAlienHorizontally(); // this makes it move one step in the new direction.

                    // i.e.
                    //   |  /o\  current pos             
                    //   |<-- moves 1 step          XDirection = -1: MoveReferenceAlienHorizontally()
                    //   |/o\   alien reaches side  Edge
                    //   | /o\  alien moves right   XDirection = +1: MoveReferenceAlienHorizontally(), back to where we started
                    //   |   /o\  alien moves right XDirection = +1: MoveReferenceAlienHorizontally(), one step right
                    //   |   ...  alien moves down  

                    // The thing to remember is that we are moving the reference alien, not the alien that just hit the edge.
                    // That distinction is important. The reference alien may already be dead. It doesn't change the fact, it
                    // is still the reference that is changed and all aliens drawn relative.
                    //
                    // tbh, this concept from the original took me a while to grasp why. I first assumed when aliens are shot
                    // it has to recompute bottom left. But that isn't the case. In fact it was the AI input failing, out of
                    // bounds -1/+1 that reinforced the implication - it can be offscreen to the left, particularly if the 
                    // remaining alien is the top right one. When the top right reaches the left, the [0] reference alien will 
                    // be minus the width of one row of aliens horizontally, and as that top right alien reaches the bottom, the
                    // reference alien will be well beneath the player!

                    MoveReferenceAlienDownwards();
                }
            }

            // we could maintained a list of alive invaders, but this is simpler and safer (possibly quicker).
            // Instead of .RemoverAt() we just skip dead aliens.
            if (InvadersAliveState[spaceInvaderToRippleNext] != SpaceInvader.InvaderState.dead)
            {

                moved = true; // we found an alive invader to move.
            }
        } while (!moved);
    }

    /// <summary>
    /// Aliens drop down 8 pixels upon one hitting the edge.
    /// </summary>
    private void MoveReferenceAlienDownwards()
    {
        int amountToShuffleDown = SpaceInvader.Dimensions.Height; // When rack bumps the edge of the screen then the direction flips and the rack drops 8 pixels.

        referenceAlien.Y += amountToShuffleDown;
    }

    /// <summary>
    /// Aliens typically move 2 pixels left or right, but when there is only one alien left it moves 2 pixels left, but 3 pixels right.
    /// This is a sneaky twist in the game design to make it harder to hit the last alien.
    /// </summary>
    private void MoveReferenceAlienHorizontally()
    {
        if (spaceInvadersRemaining == 1)
        {
            referenceAlien.X += (XDirection == 1 ? 3 : 2) * XDirection;
        }
        else
        {
            referenceAlien.X += 2 * XDirection;
        }
    }

    /// <summary>
    /// Draw the aliens.
    /// </summary>
    internal void DrawInvaders()
    {
        if (deadAliens.Count == 0 && InvadersAliveState[spaceInvaderToRippleNext] == SpaceInvader.InvaderState.alive) SpaceInvader.DrawSprite(videoScreen, spaceInvaderToRippleNext, InvadersAliveState[spaceInvaderToRippleNext], referenceAlien, frame);
    }

    /// <summary>
    /// Draw the bullets fired by the aliens.
    /// </summary>
    internal void DrawInvaderBullets()
    {
        switch (bulletTypeSelectionTimer)
        {
            case 0:
                object2RollingSpaceInvaderBullet?.DrawSprite();
                break;

            case 1:
                object3PlungerSpaceInvaderBullet?.DrawSprite();
                break;

            case 2:
                object4SquigglySpaceInvaderBullet?.DrawSprite();
                break;
        }
    }

    /// <summary>
    /// Move the bullets (fired by the alien) downwards.
    /// </summary>
    /// <param name="playerPosition"></param>
    internal void MoveBullet(Point playerPosition)
    {
        // in the original, there are 3 objects used to represent the bullets.
        // object 3 is shared with the saucer.

        // true to that, they are named accordingly

        switch (bulletTypeSelectionTimer)
        {
            case 0:
                MoveRollingBulletIfActive(playerPosition);
                break;

            case 1:
                MovePlungerBulletIfActive(playerPosition);
                break;

            case 2:
                MoveSquigglyBulletIfActive(playerPosition);
                break;
        }
    }

    /// <summary>
    /// Moves the squiggly bullet if active.
    /// </summary>
    /// <param name="playerPosition"></param>
    private void MoveSquigglyBulletIfActive(Point playerPosition)
    {
        if (object4SquigglySpaceInvaderBullet is null) return;

        // Move bullet object4SquigglySpaceInvaderBullet at the desired speed.
        object4SquigglySpaceInvaderBullet.Move(SpaceInvadersRemaining);

        // AI is "scored" on how many bullets it avoids. If a bullet is within 7 pixels of the player when close to player, it is deemed avoided. We want to encourage
        // the AI to avoid getting hit.
        if (object4SquigglySpaceInvaderBullet.Position.Y >= playerPosition.Y - c_verticalSeparationInPixelsOfAlienBulletFromPlayerShip &&
            object4SquigglySpaceInvaderBullet.Position.Y + 6 < playerPosition.Y && Math.Abs(playerPosition.X - object4SquigglySpaceInvaderBullet.Position.X) <= 7)
        {
            object4SquigglySpaceInvaderBullet.BulletAvoided = true;
        }

        // gone below the user? remove bullet.
        if (object4SquigglySpaceInvaderBullet.State == SpaceInvaderBulletBase.BulletState.dead)
        {
            // if bullet was going to hit the player, and the player has moved, then the bullet is deemed to have been avoided.
            if (object4SquigglySpaceInvaderBullet.BulletAvoided) ++BulletsAvoided;

            object4SquigglySpaceInvaderBullet = null; // no more bullet
            squigglyBulletFiringState = InvaderFiringState.NotFiring;
        }
    }

    /// <summary>
    /// Moves the plunger bullet, if active.
    /// </summary>
    /// <param name="playerPosition"></param>
    private void MovePlungerBulletIfActive(Point playerPosition)
    {
        if (object3PlungerSpaceInvaderBullet is null) return;

        // Move bullet downwards at the desired speed.
        object3PlungerSpaceInvaderBullet.Move(SpaceInvadersRemaining);

        // AI is "scored" on how many bullets it avoids. If a bullet is within 7 pixels of the player when close to player, it is deemed avoided. We want to encourage
        // the AI to avoid getting hit.
        if (object3PlungerSpaceInvaderBullet.Position.Y >= playerPosition.Y - c_verticalSeparationInPixelsOfAlienBulletFromPlayerShip &&
            object3PlungerSpaceInvaderBullet.Position.Y + 6 < playerPosition.Y && Math.Abs(playerPosition.X - object3PlungerSpaceInvaderBullet.Position.X) <= 7)
        {
            object3PlungerSpaceInvaderBullet.BulletAvoided = true;
        }

        // gone below the user? remove bullet.
        if (object3PlungerSpaceInvaderBullet.State == SpaceInvaderBulletBase.BulletState.dead)
        {
            // if bullet was going to hit the player, and the player has moved, then the bullet is deemed to have been avoided.
            if (object3PlungerSpaceInvaderBullet.BulletAvoided) ++BulletsAvoided;

            object3PlungerSpaceInvaderBullet = null; // no more bullet
            plungerBulletFiringState = InvaderFiringState.NotFiring;
        }
    }

    /// <summary>
    /// Moves the squiggly bullet if active.
    /// </summary>
    /// <param name="playerPosition"></param>
    private void MoveRollingBulletIfActive(Point playerPosition)
    {
        if (object2RollingSpaceInvaderBullet is null) return;

        // Move bullet downwards at the desired speed.
        object2RollingSpaceInvaderBullet.Move(SpaceInvadersRemaining);

        // AI is "scored" on how many bullets it avoids. If a bullet is within 7 pixels of the player when close to player, it is deemed avoided. We want to encourage
        // the AI to avoid getting hit.
        if (object2RollingSpaceInvaderBullet.Position.Y >= playerPosition.Y - c_verticalSeparationInPixelsOfAlienBulletFromPlayerShip &&
            object2RollingSpaceInvaderBullet.Position.Y + 6 < playerPosition.Y && Math.Abs(playerPosition.X - object2RollingSpaceInvaderBullet.Position.X) <= 7)
        {
            object2RollingSpaceInvaderBullet.BulletAvoided = true;
        }

        // gone below the user? remove bullet.
        if (object2RollingSpaceInvaderBullet.State == SpaceInvaderBulletBase.BulletState.dead)
        {
            // if bullet was going to hit the player, and the player has moved, then the bullet is deemed to have been avoided.
            if (object2RollingSpaceInvaderBullet.BulletAvoided) ++BulletsAvoided;

            object2RollingSpaceInvaderBullet = null;
            rollingBulletFiringState = InvaderFiringState.NotFiring;
        }
    }

    /// <summary>
    /// For this we have check whether the bullet is within the bounds of each invader.
    /// An optimisation would be to check the bounds of the invader row, and if the bullet is outside that, then we can skip the row.
    /// </summary>
    /// <param name="bulletLocation"></param>
    /// <param name="invaderRow"></param>
    /// <param name="indexOfAlienHit"></param>
    /// <returns></returns>
    internal bool BulletHitAlien(Point bulletLocation, out int invaderRow, out int indexOfAlienHit)
    {
        for (int i = 0; i < 55; i++)
        {
            if (InvadersAliveState[i] == SpaceInvader.InvaderState.alive && SpaceInvader.BulletHit(bulletLocation, i, i > spaceInvaderToRippleNext ? lastReferenceAlien : referenceAlien))
            {
                SpaceInvader.GetPosition(referenceAlien, i, out int row, out _, out _, out _);
                invaderRow = row;
                indexOfAlienHit = i;
                return true;
            }
        }

        indexOfAlienHit = -1;
        invaderRow = -1;
        return false;
    }

    /// <summary>
    /// Outputs the alien positions to the debug window.
    /// </summary>
    /// <param name="bulletLocation"></param>
    internal void DebugOutputAlienPositions(Point bulletLocation)
    {
        // output bulletLocation to Debug
        Logger.Log("SpaceInvaderController.txt", $"bulletLocation: {bulletLocation}");

        for (int i = 0; i < 55; i++)
        {
            if (InvadersAliveState[i] == SpaceInvader.InvaderState.alive)
            {
                SpaceInvader.GetPosition(i <= spaceInvaderToRippleNext ? lastReferenceAlien : referenceAlien, i, out int row, out int col, out int x, out int y);

                int xAdjustment = (row != 0) ? 1 : (row == 4) ? 1 : 0;
                int widthAdjustment = (row != 0) ? -1 : (row == 4) ? -3 : 0;

                // invader on top row is 8x8, next 2 rows  is 11x8 ..next 2 rows 12x8, so we need to adjust the hitbox
                Rectangle rectangleOfHitBox = new(x + 2 + xAdjustment - 1, y, SpaceInvader.Dimensions.Width - 4 + widthAdjustment + 1, SpaceInvader.Dimensions.Height);

                Logger.Log("SpaceInvaderController.txt", $"{i}. row={row} col={col} x={x} y={y} hitbox = ({rectangleOfHitBox.Left},{rectangleOfHitBox.Top})-({rectangleOfHitBox.Right},{rectangleOfHitBox.Bottom})");
            }
        }
    }

    /// <summary>
    /// Removes an alien at the specified location (if present).
    /// If outside the bounds, it does nothing.
    /// </summary>
    /// <param name="indexOfAlienHit"></param>
    internal void KillAt(int indexOfAlienHit)
    {
        if (deadAliens.ContainsKey(indexOfAlienHit)) return; // just in case!

        // only one alien can be hit at a time, so we can exit.
        --SpaceInvadersRemaining;

        // remove the alien
        SpaceInvader.EraseSprite(videoScreen, indexOfAlienHit, InvadersAliveState[indexOfAlienHit], indexOfAlienHit > spaceInvaderToRippleNext ? lastReferenceAlien : referenceAlien, indexOfAlienHit > spaceInvaderToRippleNext ? frame : 1 - frame);

        // redraw it exploding
        InvadersAliveState[indexOfAlienHit] = SpaceInvader.InvaderState.exploding;

        // no need to draw it if it is the last one, as we're going to start a new screen anyway.
        if (SpaceInvadersRemaining > 0)
        {
            SpaceInvader.DrawSprite(videoScreen, indexOfAlienHit, InvadersAliveState[indexOfAlienHit], indexOfAlienHit > spaceInvaderToRippleNext ? lastReferenceAlien : referenceAlien, 0);

            /*
             * 152A: 3E 10           LD      A,$10               ; Initiate alien-explosion
             * 152C: 32 03 20        LD      (expAlienTimer),A   ; ... timer to 16 
             */
            deadAliens.Add(indexOfAlienHit, 16);
        }
    }

    /// <summary>
    /// Stops all Space Invader bullets that are on-screen. This typically is what happens on start of new level.
    /// </summary>
    internal void CancelAllSpaceInvaderBullets()
    {
        // remove all bullet sprites off screen
        object2RollingSpaceInvaderBullet?.EraseSprite();
        object3PlungerSpaceInvaderBullet?.EraseSprite();
        object4SquigglySpaceInvaderBullet?.EraseSprite();

        // reset the firing state
        squigglyBulletFiringState = InvaderFiringState.NotFiring;
        plungerBulletFiringState = InvaderFiringState.NotFiring;
        rollingBulletFiringState = InvaderFiringState.NotFiring;

        // remove all bullet objects, so they can be re-created.
        object2RollingSpaceInvaderBullet = null;
        object3PlungerSpaceInvaderBullet = null;
        object4SquigglySpaceInvaderBullet = null;
    }

    // the methods below are used by the AI to get the data it needs to make a decision.

    /// <summary>
    /// Returns the data that the AI needs to make a decision.
    /// </summary>
    /// <returns></returns>
    internal List<double> GetAIDataForAliens()
    {
        List<double> data = new();

        // this provides 1/0 for each alien, indicating whether it is alive or not.
        for (int i = 0; i < 55; i++)
        {
            data.Add(InvadersAliveState[i] == SpaceInvader.InvaderState.alive ? 1 : 0);
        }

        // This provides the position of the reference alien, all the alive aliens are relative to this.
        // Why divide by 2x, because the reference alien could be offscreen. Example, alien 55 (top right) is the only one
        // alive, as it moves left, when it reaches the left, the reference alien is 10x16 pixels left of it (offscreen).
        // When it moves to the bottom, the reference alien is below the screen. (this burnt me for a while).

        data.Add((double)referenceAlien.X / (2 * OriginalDataFrom1978.c_screenWidthPX));
        data.Add((double)referenceAlien.Y / (2 * OriginalDataFrom1978.c_screenHeightPX));

        // provide alien direction
        data.Add(XDirection / 3); // the last alien moves left 2px, and right 3px. So we divide by 3 to get a value between -1 and 1.
        // per SonarQube, the above should be  data.Add((double) XDirection / 3); adding the "(double)". It is indeed correct
        // *however* the AI trained without that fix, and managed perfectly well without the correct casting. If you fix it, the 
        // training for "internalData" will fail on level 2.

        return data;
    }

    /// <summary>
    /// Return game data for debugging.
    /// </summary>
    /// <returns></returns>
    internal List<double> GetGameDataForDebugging()
    {
        List<double> gameData = new()
        {
            bulletTypeSelectionTimer,
            plungerBulletIndex,
            squigglyBulletIndex
        };

        gameData.AddRange(GetBulletDataForDebugging(object2RollingSpaceInvaderBullet));
        gameData.AddRange(GetBulletDataForDebugging(object3PlungerSpaceInvaderBullet));
        gameData.AddRange(GetBulletDataForDebugging(object4SquigglySpaceInvaderBullet));

        return gameData;
    }

    /// <summary>
    /// Returns the bullet data for debugging.
    /// </summary>
    /// <param name="bullet"></param>
    /// <returns></returns>
    private List<double> GetBulletDataForDebugging(SpaceInvaderBulletBase? bullet)
    {
        List<double> gameData = new();

        if (bullet is not null)
        {
            gameData.Add((double)bullet.State);
            gameData.Add(bullet.BulletHitSomething ? 1 : 0);
            gameData.Add(bullet.BulletExplodeTimer);
            gameData.Add(plungerBulletReloadPause);
            gameData.Add(squigglyBulletReloadPause);
            gameData.Add(rollingBulletReloadPause);
        }
        else
        {
            gameData.Add(-1);
            gameData.Add(-1);
            gameData.Add(-1);
            gameData.Add(-1);
            gameData.Add(-1);
            gameData.Add(-1);
        }

        return gameData;
    }
}