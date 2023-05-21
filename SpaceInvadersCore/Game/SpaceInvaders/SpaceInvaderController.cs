using SpaceInvadersCore.Game.SpaceInvaders;
using SpaceInvadersCore;
using SpaceInvadersCore.Game;
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
    private int frame = 0;

    /// <summary>
    /// How many Space Invaders are remaining. Initially = 5 rows of 11.
    /// </summary>
    private int spaceInvadersRemaining = 55;

    /// <summary>
    /// How many bullets the player has avoided.
    /// </summary>
    internal int BulletsAvoided = 0;

    /// <summary>
    /// Each time an alien is dead we add it to this dictionary. The key is the alien index, the value is the number of frames it stays "exploding" for#
    /// </summary>
    private readonly Dictionary<int /*alien-index*/,int /*count*/> deadAliens = new();

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
    internal Point referenceAlien = new(23, (SpaceInvader.Dimensions.Height + 8) * 4 +4 );

    /// <summary>
    /// When aliens move, the reference alien is used to determine where the other aliens are drawn. When it comes 
    /// to erasing the aliens, the location they were drawn differs from the reference alien. So we preserve it in this variable.
    /// </summary>
    internal Point lastReferenceAlien = new(23, (SpaceInvader.Dimensions.Height + 8) * 4 + 4);

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
    private int XDirection = 1;

    public int DirectionAndSpeedOfInvaders
    {
        get { return XDirection; }
    }

    /// <summary>
    /// This indicates whether there are Space Invaders remaining. It saves counting.
    /// </summary>
    internal bool OneOrMoreSpaceInvaderRemaining = true;

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
    private int rollingBulletReloadPause = 0;

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
    private int squigglyBulletReloadPause = 0;

    /// <summary>
    /// When a squiggly is selected, it sequences through SpaceInvaderSpecification.s_squigglyShotColumn to determine which column.
    /// </summary>
    private int squigglyBulletIndex = 0;

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
    /// </summary>
    private int plungerBulletIndex = 0;

    /// <summary>
    /// How long it has to pause before firing the plunger bullet.
    /// </summary>
    private int plungerBulletReloadPause = 0;

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

            $38 = 56
            $78 = 120

          What makes it more confusing is the commentary that the reference alien is bottom left. Is that bottom left with orientation such that
          invaders are at the top, OR in rotated form where bottom left invader would be the bottom right previously?
        
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
        // But they are 8 tall and drawn inverted. So 136-8 = 128. I believe is correct.
        // "23" comes from comparing the original video footage to my own and adjusting mine until they overlay precisely. I find this
        // confusing as the original has "56" which is (a) double (b) far to right.
        referenceAlien = new(23, (SpaceInvader.Dimensions.Height + 8) * 4 + offsetY + 64);
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
        if (object4SquigglySpaceInvaderBullet != spaceInvaderBullet && object4SquigglySpaceInvaderBullet is not null)
        {
            if (object4SquigglySpaceInvaderBullet.Steps != -1 && object4SquigglySpaceInvaderBullet.Steps < min)
            {
                min = object4SquigglySpaceInvaderBullet.Steps;
            }
        }

        // check plunger to find the minimum number of steps
        if (object3PlungerSpaceInvaderBullet != spaceInvaderBullet && object3PlungerSpaceInvaderBullet is not null)
        {
            if (object3PlungerSpaceInvaderBullet.Steps != -1 && object3PlungerSpaceInvaderBullet.Steps < min)
            {
                min = object3PlungerSpaceInvaderBullet.Steps;
            }
        }

        // check rolling to find the minimum number of steps
        if (object2RollingSpaceInvaderBullet != spaceInvaderBullet && object2RollingSpaceInvaderBullet is not null)
        {
            if (object2RollingSpaceInvaderBullet.Steps != -1 && object2RollingSpaceInvaderBullet.Steps < min)
            {
                min = object2RollingSpaceInvaderBullet.Steps;
            }
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
    private void FireRollingBulletIfNotInMotion(int score, int playerCentreX)
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
            rollingBulletFiringState = InvaderFiringState.NotFiring;
            return;
        }

        Dictionary<int, int> spaceInvaderIndexedByColumn = GetBottomInvaderForEachLiveColumn();

        // this happens briefly when all are killed.
        if (spaceInvaderIndexedByColumn.Count == 0)
        {
            return; // no alien to shoot
        }

        int bestDistance = int.MaxValue; // we want the closest alien to the player
        int bestCol = -1; // the column of the closest alien, -1 = no alien found

        int X = 0, Y = 0;

        // check each invader in a column
        foreach (int col in spaceInvaderIndexedByColumn.Keys)
        {
            SpaceInvader.GetPosition(referenceAlien, spaceInvaderIndexedByColumn[col], out int row, out int colInv, out X, out Y);
            X += SpaceInvader.Dimensions.Width / 2; // middle of the invader

            int distance = Math.Abs(playerCentreX - X);

            if (distance < bestDistance)
            {
                // we fire from one at the bottom of the stack, so invaders don't shoot thru one that is lower.
                // we are close to the player, so we fire at the player.
                bestDistance = distance;
                bestCol = col;
            }
        }

        // no column to fire from
        if (bestCol == -1)
        {
            rollingBulletFiringState = InvaderFiringState.NotFiring;
            return; // skip this go, no alien is near enough to the player
        }

        // now we have a bullet. Starts horizontal middle of space invader, and below it.
        object2RollingSpaceInvaderBullet = new SpaceInvaderRollingBullet(videoScreen, X, Y + SpaceInvader.Dimensions.Height);

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
                if (Y > OriginalDataFrom1978.c_yOfBaseLineAboveWhichThePlayerShipIsDrawnPX - 16 /*<- top of player */ - SpaceInvader.Dimensions.Height )
                {
                    continue;
                }

                spaceInvaderIndexedByColumn.Add(col + 1, i);
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
        // < 200 => 48 (0x30)
        // 200 to 1000 => 16 (0x10)
        // 1000 to 2000 => 11 (0x0B)
        // 2000 to 3000 => 8
        // 3000 => 7. With a little flying-saucer-luck you will reach 3000 in 2 or 3 racks.
        // Assembly code for this is is at $170E

        int scoreMSB = score / 100; // not 256

        if (scoreMSB < 2)  // < 200
            reloadPause = 48; //0x30
        else
        if (scoreMSB < 10) // < 1000
            reloadPause = 16; //0x10
        else
        if (scoreMSB < 20)  // < 2000
            reloadPause = 11; //0x0B
        else
        if (scoreMSB < 30) // < 3000
            reloadPause = 8;
        else
            reloadPause = 7;
    }

    /// <summary>
    /// If a bullet exists, we cannot fire another.
    /// If a bullet doesn't exist, pick a random invader and fire from the bottom invader in that column (where they are stacked).
    /// </summary>
    /// <param name="score"></param>
    private void FirePlungerBulletIfNotInMotion(int score)
    {
        //  One alien left? Skip plunger shot?
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

            // Debug.Assert(X >= 0 && X < 224, "X is out of range");

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

            // if there are still exploding aliens, we don't move the aliens
            if (deadAliens.Count > 0) return;
        }

        // work out which alien we need to move in this frame.
        DetermineNextInvaderToRippleTo();

        // remove the alien before moving
        SpaceInvader.EraseSprite(videoScreen, spaceInvaderToRippleNext, InvadersAliveState[spaceInvaderToRippleNext], lastReferenceAlien, frame);

        // find out where the alien is now. Its position is relative to the reference alien, and the reference alien will have moved, so we need to move this alien.
        SpaceInvader.GetPosition(referenceAlien, spaceInvaderToRippleNext, out _, out _, out int X, out int Y);

        // detect aliens reached the bottom.
        if (!SpaceInvadersReachedBottom && Y > OriginalDataFrom1978.c_yOfBaseLineAboveWhichThePlayerShipIsDrawnPX - SpaceInvader.Dimensions.Height - 8)
        {
            SpaceInvadersReachedBottom = true; // game over.
        }

        // Check if any aliens have reached the edge of the screen.
        // If an invader hits the edges, we need to go down, and reverse.
        // MoveReferenceAlienHorizontally()
        // * (spaceInvadersRemaining == 1) => 3 pixels right, 2 pixels left
        // * (spaceInvadersRemaining != 1) => 2 pixels right, 2 pixels left
        if ((X > 206 && XDirection > 0) || (X < 2 && XDirection < 0))
        {
            // this indicates that when we reach the reference alien [0], that it starts the ripple of aliens 1-54 to move down.
            alienStepDown = true;
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
        object2RollingSpaceInvaderBullet?.DrawSprite();
        object3PlungerSpaceInvaderBullet?.DrawSprite();
        object4SquigglySpaceInvaderBullet?.DrawSprite();
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

        if (object2RollingSpaceInvaderBullet is not null)
        {
            // Move bullet downwards at the desired speed.
            object2RollingSpaceInvaderBullet.Move(SpaceInvadersRemaining);

            // AI is "scored" on how many bullets it avoids. If a bullet is within 7 pixels of the player when close to player, it is deemed avoided. We want to encourage
            // the AI to avoid getting hit.
            if (object2RollingSpaceInvaderBullet.Position.Y >= playerPosition.Y-30 && Math.Abs(playerPosition.X - object2RollingSpaceInvaderBullet.Position.X) <= 7)
            {
                object2RollingSpaceInvaderBullet.BulletAvoided = true;
            }

            // gone below the user? remove bullet.
            if (object2RollingSpaceInvaderBullet.Position.Y >= OriginalDataFrom1978.c_greenLineIndicatingFloorPX || object2RollingSpaceInvaderBullet.IsDead)
            {
                // if bullet was going to hit the player, and the player has moved, then the bullet is deemed to have been avoided.
                if (object2RollingSpaceInvaderBullet.BulletAvoided) ++BulletsAvoided;
                
                object2RollingSpaceInvaderBullet = null;
                rollingBulletFiringState = InvaderFiringState.NotFiring;
            }
        }

        if (object3PlungerSpaceInvaderBullet is not null)
        {
            // Move bullet downwards at the desired speed.
            object3PlungerSpaceInvaderBullet.Move(SpaceInvadersRemaining);

            // AI is "scored" on how many bullets it avoids. If a bullet is within 7 pixels of the player when close to player, it is deemed avoided. We want to encourage
            // the AI to avoid getting hit.
            if (object3PlungerSpaceInvaderBullet.Position.Y >= playerPosition.Y - 30 && Math.Abs(playerPosition.X - object3PlungerSpaceInvaderBullet.Position.X) <= 7)
            {
                object3PlungerSpaceInvaderBullet.BulletAvoided = true;
            }

            // gone below the user? remove bullet.
            if (object3PlungerSpaceInvaderBullet.Position.Y >= OriginalDataFrom1978.c_greenLineIndicatingFloorPX || object3PlungerSpaceInvaderBullet.IsDead)
            {
                // if bullet was going to hit the player, and the player has moved, then the bullet is deemed to have been avoided.
                if (object3PlungerSpaceInvaderBullet.BulletAvoided) ++BulletsAvoided;
                
                object3PlungerSpaceInvaderBullet = null; // no more bullet
                plungerBulletFiringState = InvaderFiringState.NotFiring;
            }
        }

        if (object4SquigglySpaceInvaderBullet is not null)
        {
            // Move bullet object4SquigglySpaceInvaderBullet at the desired speed.
            object4SquigglySpaceInvaderBullet.Move(SpaceInvadersRemaining);

            // AI is "scored" on how many bullets it avoids. If a bullet is within 7 pixels of the player when close to player, it is deemed avoided. We want to encourage
            // the AI to avoid getting hit.
            if (object4SquigglySpaceInvaderBullet.Position.Y >= playerPosition.Y - 30 && Math.Abs(playerPosition.X - object4SquigglySpaceInvaderBullet.Position.X) <= 7)
            {
                object4SquigglySpaceInvaderBullet.BulletAvoided = true;
            }

            // gone below the user? remove bullet.
            if (object4SquigglySpaceInvaderBullet.Position.Y >= OriginalDataFrom1978.c_greenLineIndicatingFloorPX || object4SquigglySpaceInvaderBullet.IsDead)
            {
                // if bullet was going to hit the player, and the player has moved, then the bullet is deemed to have been avoided.
                if (object4SquigglySpaceInvaderBullet.BulletAvoided) ++BulletsAvoided;

                object4SquigglySpaceInvaderBullet = null; // no more bullet
                squigglyBulletFiringState = InvaderFiringState.NotFiring;
            }
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
    /// <exception cref="NotImplementedException"></exception>
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
                int xAdjustment = ((row != 0) ? 1 : 0) + ((row == 4) ? 1 : 0);
                int widthAdjustment = ((row != 0) ? -1 : 0) + ((row == 4) ? -3 : 0);

                // invader on top row is 8x8, next 2 rows  is 11x8 ..next 2 rows 12x8, so we need to adjust the hitbox
                Rectangle rectangleOfHitBox = new(x + 2 + xAdjustment - 1, y,  SpaceInvader.Dimensions.Width - 4 + widthAdjustment + 1, SpaceInvader.Dimensions.Height);

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

        return data;
    }
}