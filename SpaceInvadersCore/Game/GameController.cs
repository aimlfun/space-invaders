using SpaceInvadersCore.Game.Player;
using SpaceInvadersCore;
using SpaceInvadersCore.Game.SpaceInvaders;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Globalization.DateTimeFormatting;

namespace SpaceInvadersCore.Game;

/// <summary>
/// Represents a controller for the Space Invaders game.
/// </summary>
public class GameController
{
    //   ████     █     █   █   █████            ███     ███    █   █   █████   ████     ███    █       █       █████   ████
    //  █        █ █    ██ ██   █               █   █   █   █   █   █     █     █   █   █   █   █       █       █       █   █
    //  █       █   █   █ █ █   █               █       █   █   ██  █     █     █   █   █   █   █       █       █       █   █
    //  █       █   █   █ █ █   ████            █       █   █   █ █ █     █     ████    █   █   █       █       ████    ████
    //  █  ██   █████   █   █   █               █       █   █   █  ██     █     █ █     █   █   █       █       █       █ █
    //  █   █   █   █   █   █   █               █   █   █   █   █   █     █     █  █    █   █   █       █       █       █  █
    //   ████   █   █   █   █   █████            ███     ███    █   █     █     █   █    ███    █████   █████   █████   █   █

    #region PRIVATE PROPERTIES
    /// <summary>
    /// The AI is quite good without shields, so we enable it to play with or without shields.
    /// </summary>
    private readonly bool drawShields = true;

    /// <summary>
    /// Represents the play ship with location, and bullet.
    /// </summary>
    private PlayerShip playerShip;

    /// <summary>
    /// Represents a flying saucer, that appears seemingly at random.
    /// </summary>
    private Saucer? saucer;

    /// <summary>
    /// Represents the 5x11 aliens.
    /// </summary>
    private SpaceInvaderController? spaceInvaderController;

    /// <summary>
    /// As the game plays, we track the frame (each move->draw cycle). This is used to determine things like when the saucer appears, which direction etc.
    /// </summary>
    private int currentFrame = 0;

    /// <summary>
    /// Timer for the saucer that is increased. When it reaches a designated amount, the saucer appears.
    /// </summary>
    private int saucerCounterUpToAppearance = 0;

    /// <summary>
    /// Tracks lives, level, etc.
    /// </summary>
    private readonly GameState gameState;

    /// <summary>
    /// This is the video display that is used to render the game.
    /// </summary>
    private readonly VideoDisplay videoScreen;

    /// <summary>
    /// Object that tracks score, and renders it.
    /// </summary>
    private readonly ScoreBoard scoreboard;

    /// <summary>
    /// When set to true, the game will exit when the level is complete rather than continuing to the next level.
    /// </summary>
    private readonly bool exitAtEndOfLevel = false;
    #endregion

    #region INTERNAL PROPERTIES
    /// <summary>
    /// Tracks the number of shots by the player.
    /// </summary>
    private int ShotsMadeByPlayer;

    /// <summary>
    /// Getter that provides a bool indicating whether the saucer is on screen or not.
    /// </summary>
    internal bool SaucerIsOnScreen
    {
        get
        {
            return saucer != null;
        }
    }
    #endregion

    #region PUBLIC PROPERTIES / GETTERS
    /// <summary>
    /// AI needs to know to switch "brain" depending on the level, this is the delegate for it
    /// </summary>
    /// <param name="level"></param>
    public delegate void LevelChangedHandler(int level);

    /// <summary>
    /// This is the even fired when changing level.
    /// </summary>
    public event LevelChangedHandler OnLevelChanged;

    /// <summary>
    /// Returns true, if the game is over.
    /// </summary>
    /// <returns></returns>
    public bool IsGameOver
    {
        get
        {
            return gameState.Lives == 0 || gameState.GameOver || scoreboard.Score > 9999; // artificial cap, in case AI hits the MAX displayable.
        }
    }

    /// <summary>
    /// Aborts this game. Used when a catastrophic mutation results in out of range neuron values, or when it's massively underperforming.
    /// </summary>
    public void AbortGame()
    {
        gameState.Lives = 0;
        gameState.GameOver = true;
    }

    /// <summary>
    /// Getter for the current score.
    /// </summary>
    public int Score
    {
        get { return scoreboard.Score; }
    }

    /// <summary>
    /// Getter for the current level.
    /// </summary>
    public int Level
    {
        get { return gameState.Level; }
    }

    /// <summary>
    /// Getter for the current lives.
    /// </summary>
    public int Lives
    {
        get { return gameState.Lives; }
    }

    /// <summary>
    /// Getter for the kill count, so we can calculate the fitness score (ratio of kills:shots).
    /// </summary>
    public int NumberOfInvadersKilled
    {
        get { return scoreboard.InvaderKills; }
    }

    /// <summary>
    /// Getter for how many frames have been played.
    /// </summary>
    public int FramesPlayed
    {
        get { return gameState.FramesPlayed; }
    }

    /// <summary>
    /// Getter for saucer kill count, so we can calculate the fitness score.
    /// </summary>
    public int NumberOfSaucersKilled
    {
        get { return scoreboard.SaucerKills; }
    }

    /// <summary>
    /// Getter for how many shots were made by the player.
    /// </summary>
    public int Shots
    {
        get { return ShotsMadeByPlayer; }
    }

    /// <summary>
    /// Getter for the amount of bullets avoided, so we can calculate the fitness score (ratio of kills:shots).
    /// </summary>
    public int KillsAvoided
    {
        get
        {
            if (spaceInvaderController is null) throw new Exception("controller is null");

            return spaceInvaderController.BulletsAvoided;
        }
    }

    public int PlayerX
    {
        get { return playerShip.Position.X; }
    }

    /// <summary>
    /// Getter for the number of times shields were hit by the player.
    /// </summary>
    public int NumberOfTimesShieldsWereShotByPlayer
    {
        get { return gameState.ShieldsHit; }
    }

    /// <summary>
    /// Enable points deduction for invaders reaching the bottom.
    /// </summary>
    public bool InvadersReachedBottom
    {
        get
        {
            return gameState.InvadersReachedBottom;
        }
    }

    /// <summary>
    /// Sets the high score.
    /// </summary>
    /// <param name="score"></param>
    public void SetHighScore(int score)
    {
        scoreboard.HighScore = score;
    }

    /// <summary>
    /// Enables the AI to reset the score to 0.
    /// </summary>
    public void ResetScore()
    {
        scoreboard.ResetScore();
    }

    /// <summary>
    /// Sets the score for the AI to enabled independent training.
    /// </summary>
    /// <param name="score"></param>
    public void AISetScore(int score)
    {
        scoreboard.AISetScore(score);
    }
    #endregion

    #region PUBLIC METHODS - EXTERNAL INTERFACE
    /// <summary>
    /// Constructor.
    /// </summary>
#pragma warning disable CS8618 // Player is initialised in InitialiseLevel, which is called from the constructor. This is a false positive.
    public GameController(VideoDisplay screen, int highScore, bool playingWithShields = true, bool oneLevelOnly = false, int startLevel = 1)
#pragma warning restore CS8618 // Player is initialised in InitialiseLevel, which is called from the constructor. This is a false positive
    {
        videoScreen = screen;
        drawShields = playingWithShields;
        exitAtEndOfLevel = oneLevelOnly;

        InitialiseVideoScreen();

        gameState = new(screen, startLevel);

        scoreboard = new(screen)
        {
            HighScore = highScore
        };

        InitialiseLevel();

        scoreboard.Draw();
        scoreboard.DrawHighScore();

        videoScreen.DrawString("CREDIT 00", new Point(136, OriginalDataFrom1978.c_greenLineIndicatingFloorPX + 2));
    }

    /// <summary>
    /// Moves the space invaders, until they reach the bottom.
    /// </summary>
    public void Play()
    {
        bool gameOver = gameState.GameOver;

        ++currentFrame;
        ++gameState.FramesPlayed; // keep track, we can teach it to complete levels in less frames

        if (!gameOver)
        {
            MoveFlyingSaucerIfOnScreenOrCreateIfTime();
            saucer?.DrawSprite();

            MoveSpaceInvadersHorizontallyAndDownIfTheyHitAnEdge();
            spaceInvaderController?.DrawInvaders();

            MovePlayerIfRequested();

            // The alien explosion is actually the player bullet based on watching YouTube videos.
            // So what I can glean is that you cannot fire whilst the explosion is happening.
            if (!spaceInvaderController.AlienExploding && playerShip.FireBulletIfRequested()) ++ShotsMadeByPlayer;// increment the # shots, so AI can rank the more accurate players higher

            UpdateBullets();
        }

        // draw the bullet even on game over, so the player can see where it hit
        if (currentFrame % 3 == 0) spaceInvaderController?.DrawInvaderBullets();

        if (gameState.GameOver) return; // ensure game cannot resume if game over

        playerShip.MoveBullet();

        HandleCollisions();
    }

    /// <summary>
    /// User is holding left arrow button.
    /// </summary>
    public void SetPlayerMoveDirectionToLeft()
    {
        playerShip.XDirection = -1;
    }

    /// <summary>
    /// User is holding right arrow button.
    /// </summary>
    public void SetPlayerMoveDirectionToRight()
    {
        playerShip.XDirection = 1;
    }

    /// <summary>
    /// User pressed "space" to fire bullets.
    /// </summary>
    public void RequestPlayerShipFiresBullet()
    {
        playerShip.FireBulletRequested = true;
    }

    /// <summary>
    /// Cancels moving - when user releases the arrow keys.
    /// </summary>
    public void CancelMove()
    {
        if (playerShip is null)
            return;
        
        playerShip.XDirection = 0;
    }

    // the methods below exist for the AI

    /// <summary>
    /// Used by the AI to move the player to a specific X coordinate.
    /// There are 2 modes for the AI, one which uses the above left/right. But the better approach asks the AI where it wants to position the ship (x coordinate).
    /// This attempts to move the ship to that position.
    /// </summary>
    /// <param name="desiredXPosition"></param>
    public void MovePlayerTo(int desiredXPosition)
    {
        if (playerShip.Position.X == desiredXPosition)
        {
            CancelMove();
            return;
        }

        if (playerShip.Position.X < desiredXPosition)
        {
            SetPlayerMoveDirectionToRight();
            return;
        }

        if (playerShip.Position.X > desiredXPosition)
        {
            SetPlayerMoveDirectionToLeft();
        }
    }

    /// <summary>
    /// Returns an array for the neural network, containing the game object state.
    /// </summary>
    /// <returns></returns>
    public List<double> AIGetObjectArray()
    {
        if (spaceInvaderController is null) throw new Exception("spaceInvaderController is null - something has broken in the initialisation");

        // where is the player located? Returned so the AI knows where it is.
        List<double> data = new()
        {
            // it needs to know WHERE it is
            playerShip.Position.X / (double)OriginalDataFrom1978.c_screenWidthPX
        };

        // if it is firing, AI doesn't need to know where. It either hits or doesn't
        if (playerShip.BulletIsInMotion)
        {
            data.Add(playerShip.BulletLocation.Y / OriginalDataFrom1978.c_screenHeightPX);
        }
        else
        {
            data.Add(0); // not firing
        }

        // tell the AI where the invader bullets are (if in motion). There are a max of 3 at any given time.
        foreach (Point p in spaceInvaderController.BulletsInAIFormat())
        {
            if (p.Y == 0)
            {
                // no bullet.
                data.Add(0);
                data.Add(0);
            }
            else
            {
                data.Add((double)p.X / OriginalDataFrom1978.c_screenWidthPX);
                data.Add((double)p.Y / OriginalDataFrom1978.c_screenHeightPX);
            }
        }

        // Provide the AI with a 1/0 indicating alive/dead for each alien.
        // We don't provide the locations of each alien, because they are all relative to the reference alien.
        // We do however give it the location of the reference alien, and the direction/speed aliens are moving.
        // Remember, this isn't like my Missile Command AI / Hittiles, where we have a heat sensor on the ship
        // and steer missiles. This is very much getting it to come up with logic / formula to move/fire based 
        // on the inputs. It doesn't even care to know it is playing Space Invaders.

        data.AddRange(spaceInvaderController.GetAIDataForAliens());

        // location and speed of the saucer, if it exists
        if (saucer is null)
        {
            data.Add(0); // direction -> not moving
            data.Add(0); // location -> not on screen
        }
        else
        {
            data.Add(saucer.XDirection / (double)OriginalDataFrom1978.c_screenWidthPX);
            data.Add(saucer.X / (double)OriginalDataFrom1978.c_screenWidthPX);
        }

        // give the player and indicator of whether there is a barrier in front of them.
        // this is misleading, because there could be a hole in the barrier
        if ((playerShip.Position.X >= 32 && playerShip.Position.X <= 32 + 21) ||
            (playerShip.Position.X >= 77 && playerShip.Position.X <= 77 + 21) ||
            (playerShip.Position.X >= 122 && playerShip.Position.X <= 122 + 21) ||
            (playerShip.Position.X >= 167 && playerShip.Position.X <= 167 + 21))
        {
            data.Add(-1); // there is a barrier in front of the player
        }
        else
        {
            data.Add(1); // there is no barrier in front of the player
        }

        return data;
    }
    #endregion

    #region PRIVATE METHODS
    /// <summary>
    /// Draw static things.
    /// </summary>
    private void InitialiseVideoScreen()
    {
        // the screen is black, with an alpha 255
        videoScreen.ClearDisplay(Color.Black);

        // the green line at the bottom of the screen is drawn once, and damaged as the aliens fire bullet
        videoScreen.DrawGreenHorizontalBaseLine(OriginalDataFrom1978.s_playerColour, OriginalDataFrom1978.c_greenLineIndicatingFloorPX);

        // whilst the score changes throughout the game, the labels for them do not.
        videoScreen.DrawString("SCORE<1> HI-SCORE SCORE<2>", new Point(8, 8));
    }

    /// <summary>
    /// Initialises a level (could be level 1, could be level 30).
    /// Resets things, creates a new PlayerShip (and bullets) and new SpaceInvader controller (and bullets).
    /// Because it's a new PlayerShip, the p
    /// New shields are created.
    /// Event handler informs the PlayController (or any subscriber) that we've changed level.
    /// </summary>
    private void InitialiseLevel()
    {
        currentFrame = 0;
        saucerCounterUpToAppearance = 0;
        
        CancelMove();

        // when we start a new level, a player will already be on screen, so we need to erase it.
        if (Level != 1) playerShip?.EraseSprite();

        playerShip = new(videoScreen);
        
        // no saucer at start.
        saucer = null; // saucer appearance is seemingly going to appear random, but not yet.

        // we need a fresh set of Space Invaders.
        spaceInvaderController = new(videoScreen, gameState.Level);

        if (drawShields) AddShields(); // the shields provide somewhere to hide, but the AI doesn't know of them or care about their existence.

        OnLevelChanged?.Invoke(Level); // event to tell the world we are starting a new level.
    }

    /// <summary>
    /// Draws the 4 hefty shields protecting the player.
    /// </summary>
    private void AddShields()
    {
        /*
                ██████████████                          
               ████████████████                         
              ██████████████████                        
             ████████████████████                       
            ██████████████████████                      
            ██████████████████████                      
            ██████████████████████                      
            ██████████████████████                      
            ██████████████████████                      
            ██████████████████████                      
            ██████████████████████                      
            ██████████████████████                      
            ███████       ████████                      
            ██████         ███████                      
            █████           ██████                      
            █████           ██████          
        */

        int topYofShields = OriginalDataFrom1978.c_greenLineIndicatingFloorPX - 48;

        // They are positioned as follows:
        //  |                                                          |____ <-- 56px above the green line.
        //  |     ##             ##              ##             ##     | 
        //  |    ####           ####            ####           ####    | 
        //  |    #  #           #  #            #  #           #  #    | 
        //
        //       |              |               |              |
        //       32             77             122            167    
        videoScreen.DrawSprite(OriginalSpritesFrom1978.Sprites["Shield"], 32, topYofShields);
        videoScreen.DrawSprite(OriginalSpritesFrom1978.Sprites["Shield"], 77, topYofShields);
        videoScreen.DrawSprite(OriginalSpritesFrom1978.Sprites["Shield"], 122, topYofShields);
        videoScreen.DrawSprite(OriginalSpritesFrom1978.Sprites["Shield"], 167, topYofShields);
    }

    /// <summary>
    /// This happens upon losing a life. Player is put at the starting position.
    /// The timer is reset, so it doesn't appear for a couple of seconds.
    /// </summary>
    private void ResetPlayerPosition()
    {
        if (spaceInvaderController is null) throw new Exception("controller is null");

        // put the player back at the left
        playerShip = new(videoScreen);
        spaceInvaderController.CancelAllSpaceInvaderBullets();

        // saucer can continue.

        // aliens continue, just player resets
        spaceInvaderController.AliensAreAllowedToFire = false;
    }

    /// <summary>
    /// Detect collisions and apply an action.
    /// End of game IF
    /// * aliens reached bottom
    /// * player runs out of lives
    /// 
    /// alien bullet hit player -> decrement lives -> if lives = 0 -> end of game
    /// player bullet hit saucer -> award points -> destroy saucer
    /// player bullet hits alien -> award points -> destroy alien -> if no aliens left -> next level
    /// </summary>
    private void HandleCollisions()
    {
        // aliens reached the bottom is a game-over state. This is not true to the original, but shortens the 
        // length of the game the AI plays.
        if (spaceInvaderController.SpaceInvadersReachedBottom)
        {
            gameState.InvadersReachedBottom = true; // makes it game over
            return;
        }

        // if alien bullet hits player, it's bad news...
        foreach (SpaceInvaderBulletBase bullet in spaceInvaderController.Bullets)
        {
            if (bullet.IsDead) continue;

            // bullet reach bottom base line, destroy it
            if (bullet.Position.Y > OriginalDataFrom1978.c_greenLineIndicatingFloorPX - 7)
            {
                BulletReachedBottomGreenLineDestroyTheBullet(bullet);
                continue;
            }

            // bullet hit player or shield
            if (bullet.BulletHitSomething)
            {
                if (playerShip.BulletHitPlayer(bullet.Position))
                {
                    HandleWhenPlayerIsHitByAlienBullet(bullet);

                    return; // we return, because the player lost a life and we need to reset the player.
                }
                else // bullet hit shield
                {
                    bullet.EraseSprite();

                    // destroy an area of the shield
                    videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["AShotExplo"], bullet.Position.X - 3, bullet.Position.Y - 3);

                    bullet.IsDead = true;
                }
            }
        }

        // if player fired bullet, check for collisions.
        if (playerShip.BulletIsInMotion)
        {
            DetectAndHandleCollisionOfBullet();
        }
    }

    /// <summary>
    /// Player was hit by a bullet fired by an alien, we decrement lives. If no lives left, game over.
    /// </summary>
    /// <param name="bullet"></param>
    private void HandleWhenPlayerIsHitByAlienBullet(SpaceInvaderBulletBase bullet)
    {
        // unlike the real world, when the player dies, so does their bullet
        CancelPlayerBullet();

        // we're going to place the player at the start, so we need to remove them.
        playerShip.EraseSprite();

        // the bullet that hit the player is destroyed, and removed from the screen
        bullet.EraseSprite();
        bullet.IsDead = true;

        // being shot is bad news, we reduce the lives. To be fair, being shot by a honking great alien weapon is likely to blow you up.
        DecrementLives();

        // player was hit, decrement lives                
        if (IsGameOver)
        {
            gameState.GameOver = true; // no more lives -> player dies, game over
        }
        else
        {
            ResetPlayerPosition(); // player lost a life, continue playing
        }
    }

    /// <summary>
    /// The Space Invader bullet reached the bottom of the screen, destroy it.
    /// The challenge is that it should remove 3 pixels from the green line (alternate pixels).
    /// </summary>
    /// <param name="bullet"></param>
    private void BulletReachedBottomGreenLineDestroyTheBullet(SpaceInvaderBulletBase bullet)
    {
        // remove the bullet
        bullet.EraseSprite();

        // destroy a couple of pixels on the green horizontal line
        bullet.Position.Y = OriginalDataFrom1978.c_greenLineIndicatingFloorPX - 4; // stop it destroying below the green line

        //   █   
        // █   █ 
        //   ██ █
        //  ████ 
        // █ ███ 
        //  █████
        // █ ███ 
        //  █ █ █    <- removes this pattern from the green line

        videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["AShotExplo"], (bullet.Position.X / 2) * 2 - 3, bullet.Position.Y - 3);

        // bullet hit the bottom, it is dead
        bullet.IsDead = true;
    }

    /// <summary>
    /// Player lost a life, decrement lives.
    /// </summary>
    private void DecrementLives()
    {
        --gameState.Lives;

        // the game state controller will display lives remaining
    }

    /// <summary>
    /// If saucer is hit, award points, and remove it.
    /// If player bullet hits Space Invader, increase score, remove Space Invader.
    /// </summary>
    private void DetectAndHandleCollisionOfBullet()
    {
        if (playerShip.BulletHitSomething)
        {
            // if player bullet hits saucer -> increase score, remove saucer
            if (saucer is not null && saucer.BulletHit(playerShip.BulletLocation))
            {
                // player shot the saucer
                HandleSaucerHit();
                return;
            }

            // if player bullet hits alien -> increase score, remove  
            if (spaceInvaderController.BulletHitAlien(playerShip.BulletLocation, out int row, out int indexOfAlienHit))
            {
                HandleSpaceInvaderHit(row, indexOfAlienHit);
                return; // < just in case additional "hit" detection is placed below, we exit
            }
            else
            {
                bool playerShotBullet = false;

                // check to see if player shot a bullet fired by an alien
                foreach (SpaceInvaderBulletBase bullet in spaceInvaderController.Bullets)
                {
                    if (bullet.BulletHit(new Point(playerShip.BulletLocation.X, playerShip.BulletLocation.Y)))
                    {
                        bullet.EraseSprite();
                        bullet.IsDead = true;
                        playerShotBullet = true;
                        break; // can't hit multiple bullets
                    }
                }

                // player shot shield :(
                if (!playerShotBullet)
                {
                    ++gameState.ShieldsHit;
                    //spaceInvaderController.DebugOutputAlienPositions(player.BulletLocation);

                    CancelPlayerBullet();
                    return;
                }

                // player shot something.
                videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["ShotExploding"], playerShip.BulletLocation.X - 2, playerShip.BulletLocation.Y + 3);
                CancelPlayerBullet();
            }
        }
    }

    /// <summary>
    /// A Space Invader was shot by player, award points, and if game-over (all shot), increment level.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="indexOfAlienHit"></param>
    private void HandleSpaceInvaderHit(int row, int indexOfAlienHit)
    {
        scoreboard.InvaderHit(row); // points for destroying a Space Invader
        scoreboard.Draw();

        CancelPlayerBullet();

        spaceInvaderController.KillAt(indexOfAlienHit /*player.BulletLocation*/);

        // no aliens? we start the next level.
        if (!spaceInvaderController.OneOrMoreSpaceInvaderRemaining)
        {
            spaceInvaderController.CancelAllSpaceInvaderBullets();

            // AI can be trained on a single level, or multiple levels.
            if (exitAtEndOfLevel)
            {
                gameState.GameOver = true;
                return;
            }

            ++gameState.Level;
            InitialiseLevel();
        }

        AwardExtraShipIfDue();
    }

    /// <summary>
    /// The flying saucer was shot, award points. 
    /// </summary>
    private void HandleSaucerHit()
    {
        scoreboard.SaucerHit(ShotsMadeByPlayer);
        scoreboard.Draw();
        ResetSaucer();

        CancelPlayerBullet();

        AwardExtraShipIfDue();
    }

    /// <summary>
    /// When user reaches the threshold they get an extra life. I believe this happens ONCE only, unless they 
    /// exceed 10,000 points and the score is reset to "0" and so is triggered again.
    /// </summary>
    private void AwardExtraShipIfDue()
    {
        if (!scoreboard.AdditionalLifeDue) return;

        ++gameState.Lives;
        scoreboard.AdditionalLifeDue = false;
    }

    /// <summary>
    /// Cancels the player bullet, and removes it from the screen. This happens when the bullet hits something (invader, invader bullet, saucer), or when it reaches the top of the screen.
    /// </summary>
    private void CancelPlayerBullet()
    {
        if (!playerShip.BulletIsInMotion) return;

        // removes the bullet from the screen
        videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["PlayerShotSpr"], playerShip.BulletLocation.X, playerShip.BulletLocation.Y - 2);

        // we explode the bullet, and wipe out a larger area than the bullet itself.
        videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["ShotExploding"], playerShip.BulletLocation.X - 4, playerShip.BulletLocation.Y - 2);

        playerShip.CancelBullet(); // we're done with this bullet.
        playerShip.BulletHitSomething = false;
    }

    /// <summary>
    /// If the flying saucer is on screen we move it. 
    /// The appearance is every 30 seconds, except if there are less than 6 aliens on screen, or if the squiggly bullet is in use.
    /// </summary>
    private void MoveFlyingSaucerIfOnScreenOrCreateIfTime()
    {
        ++saucerCounterUpToAppearance;

        // saucer doesn't fly if squiggly bullet is in use.
        if (spaceInvaderController.SquigglyBulletInUse) return;

        // If there are 8 or more aliens on the screen then a saucer begins its journey across the screen.
        if (saucer is null && spaceInvaderController.SpaceInvadersRemaining >= 8 && saucerCounterUpToAppearance > OriginalDataFrom1978.c_saucerFrameFrequency)
        {
            saucer = new(videoScreen, ShotsMadeByPlayer);
        }

        if (currentFrame % 3 == 0) saucer?.Move();

        // remove the saucer when it goes off screen
        if (saucer is not null && saucer.IsOffScreen)
        {
            ResetSaucer();
        }
    }

    /// <summary>
    /// Resets the saucer, which involves erasing it before destruction and setting up the counter.
    /// </summary>
    private void ResetSaucer()
    {
        saucer?.EraseSprite();

        saucer = null; // stops it moving the offscreen/dead saucer

        // reset the counter so we don't get a saucer appearing immediately after the last one has been destroyed.
        saucerCounterUpToAppearance = 0;
    }

    /// <summary>
    /// I think if one were fighting aliens, one would be firing missiles not bullets. I don't imagine a 50cal is really going to
    /// cut it.
    /// So here we ensure a seemingly random Space Invader is always firing, and player has a chance to.
    /// </summary>
    private void UpdateBullets()
    {
        spaceInvaderController.FireIfBulletNotInMotion(scoreboard.Score, playerShip.Position.X, saucer is not null);

        // A shot makes a step every 3 frames. (4*60/3 = 80 pixels per second).
        if (currentFrame % 3 == 0) spaceInvaderController.MoveBullet(playerShip.Position);
    }

    /// <summary>
    /// Aliens move in a shuffling way.
    /// </summary>
    private void MoveSpaceInvadersHorizontallyAndDownIfTheyHitAnEdge()
    {
        if (playerShip.Ready) spaceInvaderController.AliensAreAllowedToFire = true;
        spaceInvaderController?.Move();
    }

    /// <summary>
    /// Moves the player left or right.
    /// </summary>
    private void MovePlayerIfRequested()
    {
        playerShip.EraseSprite();
        playerShip.Move();
        playerShip.DrawSprite();
    }

    /// <summary>
    /// Returns the video display condensed in size to reduce the neurons required to process it.
    /// </summary>
    /// <returns></returns>
    public double[] AIGetShrunkScreen()
    {
        return videoScreen.VideoShrunkForAI();
    }

    /// <summary>
    /// A radar array is a 1D array of 50 values, each value is the distance to the nearest object in that direction.
    /// </summary>
    /// <returns></returns>
    public double[] AIGetRadarArray()
    {
        int samplePoints = 50;

        double[] LIDAROutput = new double[samplePoints];

        float LIDARAngleToCheckInDegrees = -60; 

        float LIDARVisionAngleInDegrees = 2*(-LIDARAngleToCheckInDegrees)/samplePoints;

        int searchDistanceInPixels = 200;

        for (int LIDARAngleIndex = 0; LIDARAngleIndex < samplePoints; LIDARAngleIndex++)
        {
            //     -45  0  45
            //  -90 _ \ | / _ 90   <-- relative to direction of player. 0 = right, 90 = up, so we adjust for
            double LIDARAngleToCheckInRadians = DegreesInRadians(90 + LIDARAngleToCheckInDegrees);

            // calculate ONCE per angle, not per radius.
            double cos = Math.Cos(LIDARAngleToCheckInRadians);
            double sin = Math.Sin(LIDARAngleToCheckInRadians);

            float distanceToAlien = 0;

            for (int currentLIDARScanningDistanceRadius = 5;
                     currentLIDARScanningDistanceRadius < searchDistanceInPixels;
                     currentLIDARScanningDistanceRadius += 4) // no need to check at 1 pixel resolution
            {
                double positionBeingScannedX = Math.Round(cos * currentLIDARScanningDistanceRadius);
                double positionBeingScannedY = Math.Round(sin * currentLIDARScanningDistanceRadius);

                // y has to be negated because the screen is upside down. Cartesian (0,) is bottom left, our back-buffer is Bitmap aligned (0,0) is top left.
                Point p = new(playerShip.Position.X - (int)positionBeingScannedX, playerShip.Position.Y - (int)positionBeingScannedY);

                if (p.X < 0 || p.X > 224 || p.Y < 32) break; // off screen

                // do we see invader / saucer on that pixel?
                if (videoScreen.GetPixel(p).G == 255) // true of invader (white) or saucer (magenta) shields (green).
                {
                    distanceToAlien = currentLIDARScanningDistanceRadius;
                    break; // we've found the closest pixel in this direction
                }

                // DEBUG: enable this to see the radar rays
                // videoScreen.SetPixel(Color.Blue, p);
            }

            if (distanceToAlien > 0)
            {
                distanceToAlien  /= searchDistanceInPixels;
                
                LIDAROutput[LIDARAngleIndex] = 1 - distanceToAlien;
            }
            else
            {
                LIDAROutput[LIDARAngleIndex] = 0;
            }

            LIDARAngleToCheckInDegrees += LIDARVisionAngleInDegrees;
        }

        // an array of float values 0..1 indicating "1" something is really close in that direction to "0" nothing.
        return LIDAROutput;
    }
   
    /// <summary>
    /// Logic requires radians but we track angles in degrees, this converts.
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static double DegreesInRadians(double angle)
    {
        // if (angle < 0 || angle > 360) Debugger.Break();

        return (double)Math.PI * angle / 180;
    }

    #endregion
}