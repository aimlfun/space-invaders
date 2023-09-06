using SpaceInvadersCore.Game.Player;
using SpaceInvadersCore.Game.SpaceInvaders;
using System.Diagnostics;
using System.Drawing;
using SpaceInvadersCore.Game.AISupport;
using SpaceInvadersCore.Utilities;

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
    private readonly bool drawShields;

    /// <summary>
    /// If set, we tag the screen with the frame number at the bottom of the screen.
    /// </summary>
    private readonly bool showingFrameNumberAtBottomOfScreen = false;

    /// <summary>
    /// Represents the play ship with location, and bullet.
    /// </summary>
    internal PlayerShip playerShip;

    /// <summary>
    /// Checks to see if the player is ready to play.
    /// </summary>
    public bool PlayerIsReady
    {
        get { return playerShip.Ready; }
    }

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
    /// This tracks the frame since the start, to output on the bottom of the screen. This means when it loops (past level 10), you can see it isn't the same level.
    /// </summary>
    private int frameSinceStartOfGame = 0;

    /// <summary>
    /// Timer for the saucer that is decrement. When it reaches 0, the saucer appears.
    /// </summary>
    private int saucerCountDownToAppearance = 0;

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

    /// <summary>
    /// Accelerate learning by ending game if a life lost. 
    /// Useful if goal is infinity score.
    /// </summary>
    private readonly bool endGameIfSingleLifeLost = false;
    
    /// <summary>
    /// Stores the radar points plotted, so they can be "unplotted" without sin/cos computation.
    /// </summary>
    private readonly List<Point> radarPoints = new();
    #endregion

    #region INTERNAL PROPERTIES

    /// <summary>
    /// When set to a value other than 0, this decrements with each frame.
    /// </summary>
    internal int GeneralPurposeCountDownTimer = 0;

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
            return gameState.Lives == 0 || gameState.GameOver;
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
            if (spaceInvaderController is null) throw new ApplicationException("controller is null");

            return spaceInvaderController.BulletsAvoided;
        }
    }

    /// <summary>
    /// X position of the player ship.
    /// </summary>
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
    public void AISetScore(int score, bool drawLevelIndicator = true)
    {
        scoreboard.AISetScore(score);
        Reset();

        if (drawLevelIndicator)
        {
            // draw the level where player 2 score would be, otherwise one would never know what level it is currently on.
            string levelLabel = $"LEVEL {gameState.Level}";

            videoScreen.FillRectangle(Color.Black, new Rectangle(216 - 8 - levelLabel.Length * 8, OriginalDataFrom1978.s_scorePlayer2Rectangle.Y, levelLabel.Length * 8, 8));

            // we write these to the screen once for performance reasons. Just to show off, we do so using our video display code not onto a Bitmap.
            videoScreen.DrawString(levelLabel.ToUpper().Replace("/", "-"), new Point(216 - 8 - levelLabel.Length * 8, OriginalDataFrom1978.s_scorePlayer2Rectangle.Y)); // alphanumeric sprites are 8px wide
        }
                
        // Enable it to record level 2, you can add "if (gameState.Level == 2) DebugSettings.c_debugDrawEveryFrameAsAnImage = true;"
    }

    /// <summary>
    /// Resets everything,so we can transition through levels when it's in "AI Play Game" mode.
    /// </summary>
    private void Reset()
    {
        ShotsMadeByPlayer = 0;
        saucer = null;
        saucerCountDownToAppearance = OriginalDataFrom1978.c_saucerFrameFrequency;
        scoreboard.AdditionalLifeDue = false;
        radarPoints.Clear();
        GeneralPurposeCountDownTimer = 0;
    }
    #endregion

    #region PUBLIC METHODS - EXTERNAL INTERFACE
    /// <summary>
    /// Constructor.
    /// </summary>
#pragma warning disable CS8618 // Player is initialised in InitialiseLevel, which is called from the constructor. This is a false positive.
    public GameController(VideoDisplay screen, int highScore, bool playingWithShields = true, bool oneLevelOnly = false, int startLevel = 1, bool endIfSingleLifeLost = false, bool showFrameNumber = false)
#pragma warning restore CS8618 // Player is initialised in InitialiseLevel, which is called from the constructor. This is a false positive
    {
        videoScreen = screen;
        drawShields = playingWithShields;
        exitAtEndOfLevel = oneLevelOnly;
        showingFrameNumberAtBottomOfScreen = showFrameNumber;
        endGameIfSingleLifeLost = endIfSingleLifeLost;

        InitialiseVideoScreen();

        gameState = new(screen, startLevel);

        scoreboard = new(screen)
        {
            HighScore = highScore
        };

        InitialiseLevel();

        scoreboard.Draw();
        scoreboard.DrawHighScore();

        videoScreen.DrawString("CREDIT 00", OriginalDataFrom1978.s_credit_00_position);
    }

    /// <summary>
    /// Moves the space invaders, until they reach the bottom.
    /// </summary>
    public void Play()
    {
        if (gameState.Level == DebugSettings.c_debugDataCollectionAtLevel) Logger.Log($"data-collection-{gameState.Level}", string.Join(",", GetGameData()));

        bool gameOver = gameState.GameOver;

        ++currentFrame; // this is 0 at start of level, and is used to ensure saucer moves every 3 etc.

        // we track frames, and if reaches the frame to debug, it enters the debugger.
        if (++frameSinceStartOfGame == DebugSettings.c_debugStopAtFrameNumber) Debugger.Break();

        if (showingFrameNumberAtBottomOfScreen) ShowFrameNumber();

        ++gameState.FramesPlayed; // keep track, we can teach it to complete levels in less frames

        // every frame the count down timer is decremented, so we can use it for timing events.
        if (GeneralPurposeCountDownTimer > 0) --GeneralPurposeCountDownTimer;

        if (!gameOver)
        {
            MoveFlyingSaucerIfOnScreenOrCreateIfTime();
            saucer?.DrawSprite();

            MoveSpaceInvadersHorizontallyAndDownIfTheyHitAnEdge();
            spaceInvaderController?.DrawInvaders();

            MovePlayerIfRequested();

            Debug.Assert(spaceInvaderController is not null);

            // The alien explosion is actually the player bullet based on watching YouTube videos.
            // So what I can glean is that you cannot fire whilst the explosion is happening.
            if (!spaceInvaderController.AlienExploding && playerShip.FireBulletIfRequested()) ++ShotsMadeByPlayer;// increment the # shots, so AI can rank the more accurate players higher

            UpdateBullets();
        }

        // draw the bullet even on game over, so the player can see where it hit
        spaceInvaderController?.DrawInvaderBullets();

        if (gameState.GameOver) return; // ensure game cannot resume if game over

        playerShip.MoveBullet();

        HandleCollisions();
    }

    /// <summary>
    /// Sometimes it's useful to collate all the data points frame by frame and write them to a log file.
    /// </summary>
    /// <returns></returns>
    private double[] GetGameData()
    {
        Debug.Assert(spaceInvaderController is not null);

        List<double> data = new(AIGetObjectArray())
        {
            PlayerX,
            Score,
            currentFrame,
            gameState.ShieldsHit
        };

        data.AddRange(spaceInvaderController.GetGameDataForDebugging());

        return data.ToArray();
    }

    /// <summary>
    /// Draws a frame number to the screen, so it is clear when recording a video which frame is being displayed.
    /// </summary>
    private void ShowFrameNumber()
    {
        string frameLabel = $"{frameSinceStartOfGame}";

        int width = frameLabel.Length * 8; // alphanumeric sprites are 8px wide

        // clear the space we're drawing, the write the size
        videoScreen.FillRectangle(Color.Black, new Rectangle(120 - width / 2, 240, width, 8));
        videoScreen.DrawString($"{frameLabel}", new Point(120 - width / 2, 240));
    }

    /// <summary>
    /// Enables the AIPlayController to write the game over message to the screen.
    /// </summary>
    public void WriteGameOverToVideoScreen()
    {
        videoScreen.DrawString("GAME OVER", new Point(76, 40));
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
    public double[] AIGetObjectArray()
    {
        if (spaceInvaderController is null) throw new ApplicationException("spaceInvaderController is null - something has broken in the initialisation");

        // where is the player located? Returned so the AI knows where it is.
        List<double> data = new()
        {
            // it needs to know WHERE it is
            playerShip.Position.X / (double)OriginalDataFrom1978.c_screenWidthPX
        };

        // if it is firing, AI doesn't need to know where. It either hits or doesn't
        if (playerShip.BulletIsInMotion)
        {
            // SonarQube pointed out the lack of casting one or both to (double). Embarassingly it is *correct*.
            // That means this is likely to always be "0". I would fix it, but if I do the trained AI will be invalid, it won't appreciate
            // the fact it learnt with 0 and now it has strange values.
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

        return data.ToArray();
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

        
        // whilst the score changes throughout the game, the labels for them do not.
        videoScreen.DrawString(" SCORE<1> HI-SCORE SCORE<2>", OriginalDataFrom1978.s_score1HighScoreAndScore2Position);
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
        // the green line at the bottom of the screen is drawn once per level, and damaged as the aliens fire bullet
        videoScreen.DrawGreenHorizontalBaseLine(OriginalDataFrom1978.s_playerColour, OriginalDataFrom1978.c_greenLineIndicatingFloorPX);

        currentFrame = 0;
        saucerCountDownToAppearance = OriginalDataFrom1978.c_saucerFrameFrequency;

        CancelMove();

        // when we start a new level, a player will already be on screen, so we need to erase it.
        if (Level != 1) playerShip?.EraseSprite();

        playerShip = new(videoScreen);

        // no saucer at start.
        saucer = null; // saucer appearance is seemingly going to appear random, but not yet.

        // we need a fresh set of Space Invaders.
        spaceInvaderController = new(videoScreen, gameState.Level);

        gameState.ResetShieldCount();

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

        int topYofShields = OriginalDataFrom1978.c_topOfShieldsPX;

        // They are positioned as follows:
        //  |                                                          |
        //  |     ##             ##              ##             ##     | 
        //  |    ####           ####            ####           ####    | 
        //  |    #  #           #  #            #  #           #  #    | 
        //
        //       |              |               |              |
        //       32             77             122            167    

        Sprite shield = OriginalSpritesFrom1978.Get("Shield");

        if (DebugSettings.s_DrawShieldsIn252)
        {
            videoScreen.DrawShield(shield, 32, topYofShields);
            videoScreen.DrawShield(shield, 77, topYofShields);
            videoScreen.DrawShield(shield, 122, topYofShields);
            videoScreen.DrawShield(shield, 167, topYofShields);
        }
        else
        {
            videoScreen.DrawSprite(shield, 32, topYofShields);
            videoScreen.DrawSprite(shield, 77, topYofShields);
            videoScreen.DrawSprite(shield, 122, topYofShields);
            videoScreen.DrawSprite(shield, 167, topYofShields);
        }
    }

    /// <summary>
    /// This happens upon losing a life. Player is put at the starting position.
    /// The timer is reset, so it doesn't appear for a couple of seconds.
    /// </summary>
    private void ResetPlayerPosition()
    {
        if (playerShip.BulletIsInMotion) playerShip.CancelBullet();

        if (spaceInvaderController is null) throw new ApplicationException("controller is null");

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
        Debug.Assert(spaceInvaderController is not null);

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
            if (bullet.State == SpaceInvaderBulletBase.BulletState.dead) continue;

            // bullet reach bottom base line, destroy it
            if (bullet.Position.Y > OriginalDataFrom1978.c_greenLineIndicatingFloorPX - 7 && bullet.State == SpaceInvaderBulletBase.BulletState.normalMovement)
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
                    videoScreen.EraseSprite(OriginalSpritesFrom1978.Get("AShotExplo"), bullet.Position.X - 3, bullet.Position.Y - 3);

                    bullet.State = SpaceInvaderBulletBase.BulletState.dead;
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
        bullet.State = SpaceInvaderBulletBase.BulletState.dead;

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
    private static void BulletReachedBottomGreenLineDestroyTheBullet(SpaceInvaderBulletBase bullet)
    {
        bullet.EraseSprite();

        bullet.State = SpaceInvaderBulletBase.BulletState.explosionInProgress;
        bullet.Position.Y = OriginalDataFrom1978.c_greenLineIndicatingFloorPX - 4; // stop it destroying below the green line
        bullet.LastPosition = bullet.Position;

        // explosion

        //   █   
        // █   █ 
        //   ██ █
        //  ████ 
        // █ ███ 
        //  █████
        // █ ███ 
        //  █ █ █    <- removes this pattern from the green line
    }

    /// <summary>
    /// Player lost a life, decrement lives.
    /// </summary>
    private void DecrementLives()
    {
        --gameState.Lives;

        // the game state controller will display lives remaining
        if (endGameIfSingleLifeLost)
        {
            //Debug.WriteLine(" exiting game, because player lost a life.");
            gameState.GameOver = true;
        }
    }

    /// <summary>
    /// If saucer is hit, award points, and remove it.
    /// If player bullet hits Space Invader, increase score, remove Space Invader.
    /// </summary>
    private void DetectAndHandleCollisionOfBullet()
    {
        Debug.Assert(spaceInvaderController is not null);

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
                bool playerShotBullet = DidPlayerShootBullet();

                // player shot shield :(
                if (!playerShotBullet)
                {
                    ++gameState.ShieldsHit;
                    //for debugging, you can add spaceInvaderController.DebugOutputAlienPositions(player.BulletLocation);

                    CancelPlayerBullet();
                    return;
                }

                // player shot something.
                videoScreen.EraseSprite(OriginalSpritesFrom1978.Get("ShotExploding"), playerShip.BulletLocation.X - 2, playerShip.BulletLocation.Y + 3);
                CancelPlayerBullet();
            }
        }
    }

    /// <summary>
    /// Detect if player shot a bullet fired by an alien.
    /// </summary>
    /// <returns></returns>
    private bool DidPlayerShootBullet()
    {
        // check to see if player shot a bullet fired by an alien
        foreach (SpaceInvaderBulletBase bullet in spaceInvaderController.Bullets)
        {
            if (bullet.BulletHit(new Point(playerShip.BulletLocation.X, playerShip.BulletLocation.Y)))
            {
                bullet.EraseSprite();
                bullet.State = SpaceInvaderBulletBase.BulletState.dead;
                return true; // can't hit multiple bullets
            }
        }

        return false;
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

        CancelPlayerBulletWithoutTimer(); // the alien exploding draws a "splat", and holds up the firing for a moment

        spaceInvaderController?.KillAt(indexOfAlienHit /*player.BulletLocation*/);

        Debug.Assert(spaceInvaderController is not null);

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

        playerShip.ExplosionTime();
        
        playerShip.BulletHitSomething = false;
    }
    
    /// <summary>
    /// Cancels the player bullet, and removes it from the screen but without the explosion. It's used for when the player hits an invader.
    /// </summary>
    private void CancelPlayerBulletWithoutTimer()
    {
        if (!playerShip.BulletIsInMotion) return;
        
        // removes the bullet from the screen
       
        playerShip.CancelBullet(); // we're done with this bullet.
        playerShip.BulletHitSomething = false;
    }

    /// <summary>
    /// If the flying saucer is on screen we move it. 
    /// The appearance is every 30 seconds, except if there are less than 6 aliens on screen, or if the squiggly bullet is in use.
    /// </summary>
    private void MoveFlyingSaucerIfOnScreenOrCreateIfTime()
    {
        Debug.Assert(spaceInvaderController is not null);

        --saucerCountDownToAppearance;

        // saucer doesn't fly if squiggly bullet is in use.
        if (spaceInvaderController.SquigglyBulletInUse) return;

        // If there are 8 or more aliens on the screen then a saucer begins its journey across the screen.
        if (saucer is null && spaceInvaderController.SpaceInvadersRemaining >= 8 && saucerCountDownToAppearance < 1)
        {
            saucer = new(videoScreen, ShotsMadeByPlayer);
        }

        if (currentFrame % 3 == 0) saucer?.Move(); // saucer moves every 3 frames, because "GameObject3" would be updated every 3 frames.

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
        saucerCountDownToAppearance = OriginalDataFrom1978.c_saucerFrameFrequency;
    }

    /// <summary>
    /// I think if one were fighting aliens, one would be firing missiles not bullets. I don't imagine a 50-cal is really going to
    /// cut it.
    /// So here we ensure a seemingly random Space Invader is always firing, and player has a chance to.
    /// </summary>
    private void UpdateBullets()
    {
        spaceInvaderController?.FireIfBulletNotInMotion(scoreboard.Score, playerShip.Position.X, saucer is not null);

        // an active shot makes a step every 3 frames. (4*60/3 = 80 pixels per second).
        spaceInvaderController?.MoveBullet(playerShip.Position);
    }

    /// <summary>
    /// Aliens move in a shuffling way.
    /// </summary>
    private void MoveSpaceInvadersHorizontallyAndDownIfTheyHitAnEdge()
    {
        Debug.Assert(spaceInvaderController is not null);

        if (!spaceInvaderController.AliensAreAllowedToFire && playerShip.Ready)
        {
            spaceInvaderController.AliensAreAllowedToFire = true;
        }
     
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
    /// Returns the "radar", which enables the AI to see the Space Invaders and shields
    /// </summary>
    /// <returns></returns>
    public double[] AIGetRadarArray()
    {
        Debug.Assert(spaceInvaderController is not null);

        return Radar.Output(playerShip, spaceInvaderController.DirectionAndSpeedOfInvaders, radarPoints, currentFrame, videoScreen);
    }

    #endregion
}