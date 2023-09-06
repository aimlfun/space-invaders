using System.Diagnostics;
using System.Drawing;

namespace SpaceInvadersCore.Game.Player;

/// <summary>
/// Represents a player ship.
/// </summary>
internal class PlayerShip
{
    //  ████    █         █     █   █   █████   ████             ███    █   █    ███    ████
    //  █   █   █        █ █    █   █   █       █   █           █   █   █   █     █     █   █
    //  █   █   █       █   █    █ █    █       █   █           █       █   █     █     █   █
    //  ████    █       █   █     █     ████    ████             ███    █████     █     ████
    //  █       █       █████     █     █       █ █                 █   █   █     █     █
    //  █       █       █   █     █     █       █  █            █   █   █   █     █     █
    //  █       █████   █   █     █     █████   █   █            ███    █   █    ███    █


    //       █       
    //      ███      
    //      ███      
    //  ███████████  
    // █████████████ 
    // █████████████    <<<--- this
    // █████████████ 
    // █████████████  

    /// <summary>
    /// Dimensions of the player sprite.
    /// </summary>
    internal readonly static Size Dimensions = new()
    {
        Width = 16,  // [$10] @ $201C	plyrSprSiz	Player sprite descriptor ... size of sprite
        Height = 8
    };

    
    /// <summary>
    /// Until this reaches zero, the player does not appear.
    /// </summary>
    internal byte PlayerWaitTimer = 128; // should be "128", from disassembly: @2011 obj0TimerLSB	Wait 128 interrupts (about 2 secs) before player task starts

    /// <summary>
    /// Returns true if the player is ready to move (and visible).
    /// </summary>
    internal bool Ready
    {
        get
        {
            return PlayerWaitTimer == 0;
        }
    }

    /// <summary>
    /// This is the position of the player.
    /// </summary>
    internal Point Position;

    /// <summary>
    /// This is set when the player bullet hits something
    /// </summary>
    internal bool BulletHitSomething = false;

    /// <summary>
    /// This determines the direction the player moves (left or right).
    /// </summary>
    internal int XDirection = 0;

    /// <summary>
    /// To fire a bullet, the caller sets this to true.
    /// Next time the player moves, a bullet is fired.
    /// </summary>
    internal bool FireBulletRequested;

    /// <summary>
    /// If the player has fired a bullet, this is non null and contains the bullet.
    /// </summary>
    private PlayerBullet? playerBullet;

    /// <summary>
    /// This is our video display object that draws the sprites
    /// </summary>
    internal VideoDisplay videoScreen;

    /// <summary>
    /// If a player bullet is in flight, this returns the location of the bullet.
    /// </summary>
    internal Point BulletLocation
    {
        get
        {
            if(playerBullet == null) throw new ApplicationException("No bullet in flight.");

            return playerBullet.Position;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="screen"></param>
    internal PlayerShip(VideoDisplay screen)
    {
        // why plus 8? The values in "OriginalDataFrom1978" are the left edge of the sprite.
        Position.X = OriginalDataFrom1978.s_PlayerShipStartLocation.X + 8; // player starts on the left of screen... 
        Position.Y = OriginalDataFrom1978.s_PlayerShipStartLocation.Y; // player position vertically is fixed
        
        videoScreen = screen; // save reference to video display object
    }

    /// <summary>
    /// Moves the player left or right.
    /// </summary>
    internal void Move()
    {
        if (PlayerWaitTimer > 0)
        {
            --PlayerWaitTimer;
            return;
        }

        int widthDiv2 = Dimensions.Width / 2;

        // Original X is the left of the sprite, ours is the centre.
        // we thus need to subtract half the width of the sprite to get the left edge.
        int xAsPerOriginalIncludingAmountToMove = Position.X - widthDiv2 + XDirection; // - 8 

        // stop player moving off screen

        // 
        // 034A: 3A 1B 20        LD      A,(playerXr)        ; Current player coordinates
        // 034D: 47              LD      B, A                ; Hold it

        // ; Handle player moving right
        // 0381: 78              LD      A,B                 ; Player coordinate
        // 0382: FE D9           CP      $D9                 ; At right edge?
        // 0384: CA 6F 03        JP      Z,$036F             ; Yes ... ignore this
        // 0387: 3C              INC     A                   ; Bump X coordinate
        // 0388: 32 1B 20        LD      (playerXr),A        ; New X coordinate
        // 038B: C3 6F 03        JP      $036F               ; Draw player and out

        // I am curious about the above..
        // The ship is 16 pixels wide, so 223 - 16 = 207.
        // So if the left is capped at 16, then the right should be capped at 207. The asymmetry is odd. Could I have made a mistake?
        // I would use: 223 - Dimensions.Width - 3;
        int maxRight = 191; // the computed value is 185, but 191 seems to be what the game uses.

        // ; Handle player moving left
        // 038E: 78              LD      A,B                 ; Player coordinate
        // 038F: FE 30           CP      $30                 ; At left edge
        // 0391: CA 6F 03        JP      Z,$036F             ; Yes ... ignore this
        // 0394: 3D              DEC     A                   ; Bump X coordinate
        // 0395: 32 1B 20        LD      (playerXr),A        ; New X coordinate
        // 0398: C3 6F 03        JP      $036F               ; Draw player and out

        const int minLeft = 16; // I would use 3
 
        if (xAsPerOriginalIncludingAmountToMove < minLeft ||
            xAsPerOriginalIncludingAmountToMove > maxRight) return;

        Position.X += XDirection; 
    }

    /// <summary>
    /// Returns true if the players bullet is in motion.
    /// </summary>
    internal bool BulletIsInMotion
    {
        get
        {
            return playerBullet != null;
        }
    }

    /// <summary>
    /// Moves the players bullet.
    /// </summary>
    internal void MoveBullet()
    {
        if (playerBullet is null) return;

        if (playerBullet.State == PlayerBullet.BulletState.explosionInProgress)
            HandleBulletExplosion();
        else
        {
            // rather than plot individual pixels, the original has a 8 pixel high sprite, of which half are set.
            playerBullet.EraseSprite(videoScreen);

            // bullet goes upwards
            playerBullet.Position.Y -= OriginalDataFrom1978.c_playerBulletSpeedPX;

            // if the bullet reaches the top, we cancel it (it hit nothing). Player can fire another afterwards.
            if (playerBullet.Position.Y < OriginalDataFrom1978.c_verticalPointWherePlayerBulletsStopPX)
            {
                ExplosionTime();
            }
        }

        DrawBulletSprite();        
    }

    /// <summary>
    /// 
    /// </summary>
    internal void ExplosionTime()
    {
        if (playerBullet is null) return;

        playerBullet.State = PlayerBullet.BulletState.explosionInProgress;
    }

    /// <summary>
    /// When the bullet hits the top of the screen, we draw an explosion, then pause for 16 frames (16*16.6ms = 266ms), then cancel the bullet.
    /// Why did I bother implementing? This changes the game slightly, because it penalises the play for missing. They cannot fire for 266ms.
    /// </summary>
    private void HandleBulletExplosion()
    {
        Debug.Assert(playerBullet is not null);

        // we'll cancel the bullet when the timer reaches zero.        
        if (--playerBullet.BlowUpTimer <= 0)
        {
            // removal, destroy bullet.
            CancelBullet();
        }
    }

    /// <summary>
    /// Detect whether bullet hits player.
    /// </summary>
    /// <param name="alienBulletPosition"></param>
    /// <returns></returns>
    internal bool BulletHitPlayer(Point alienBulletPosition)
    {
        Rectangle hitbox = HitBox();

        // bullet has not reached ship vertically
        if (alienBulletPosition.Y + 5 < hitbox.Top) return false;

        // bullet is left or right of ship.
        if (alienBulletPosition.X < hitbox.Left || alienBulletPosition.X > hitbox.Right) return false;

        // bullet is within ship
        return true;
    }

    /// <summary>
    /// This is the rectangle around the player. We use pixel detection, so this is rudimentary.
    /// </summary>
    /// <returns></returns>
    private Rectangle HitBox()
    {
        return new Rectangle(Position.X - Dimensions.Width / 2 + 2, Position.Y, Dimensions.Width-3, Dimensions.Height);
    }

    /// <summary>
    /// Removes the bullet object, so it can be fired again.
    /// </summary>
    internal void CancelBullet()
    {
        playerBullet?.EraseSprite(videoScreen);
        playerBullet = null;
    }

    /// <summary>
    /// If user requested a bullet to be fired, and we are not already firing a bullet, we create
    /// one.
    /// </summary>
    internal bool FireBulletIfRequested()
    {
        if (PlayerWaitTimer > 0) return false; // don't fire until timer is complete and player is visible

        // bullet is already in motion, or user hasn't requested one
        if (!FireBulletRequested || BulletIsInMotion) return false;

        // creates the bullet
        playerBullet = new PlayerBullet(Position.X, Position.Y - 4);

        // we have fired the bullet, cancel the request
        FireBulletRequested = false;
        
        return true;
    }

    /// <summary> 
    /// Draw player ship centred on X.
    /// </summary>
    internal void DrawSprite()
    {
        /*
            Player width: 16 X height: 8
                    █       
                   ███      
                   ███      
               ███████████  
              █████████████ 
              █████████████ 
              █████████████ 
              █████████████ 
        */

        if (PlayerWaitTimer > 0) return; // don't draw until timer is complete. When the game starts there is a pause before the player appears.

        if (DebugSettings.c_debugPlayerShipDrawHitBox) videoScreen.DrawRectangle(Color.Blue, HitBox());

        DrawPlayerSpriteAt(videoScreen, Position);
    }

    /// <summary>
    /// Draws the bullet.
    /// </summary>
    internal void DrawBulletSprite()
    {
        if (playerBullet is not null && playerBullet.DrawSprite(videoScreen)) BulletHitSomething = true;
    }

    /// <summary>
    /// Draws the ship centred on the designated location. 
    /// Used for the player ship in the game, and the lives indicators.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="position"></param>
    internal static void DrawPlayerSpriteAt(VideoDisplay videoScreen, Point position)
    {
        videoScreen.DrawSprite(OriginalSpritesFrom1978.Get("Player"), position.X - Dimensions.Width / 2, position.Y);
    }

    /// <summary>
    /// Removes player ship from screen.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="position"></param>
    internal void EraseSprite()
    {
        ErasePlayerSpriteAt(videoScreen, Position);

        if (DebugSettings.c_debugPlayerShipDrawHitBox) videoScreen.DrawRectangle(Color.Black, HitBox());
    }

    /// <summary>
    /// Erases the player sprite.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="position"></param>
    internal static void ErasePlayerSpriteAt(VideoDisplay videoScreen, Point position)
    {
        videoScreen.EraseSprite(OriginalSpritesFrom1978.Get("Player"), position.X - Dimensions.Width / 2, position.Y);
    }
}