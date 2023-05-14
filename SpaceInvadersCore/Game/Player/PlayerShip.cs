using SpaceInvadersCore;
using SpaceInvadersCore.Game;
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
    internal static Size Dimensions = new()
    {
        Width = 16,
        Height = 8
    };

    /// <summary>
    /// Debugging. Shows the hit box for the player.
    /// </summary>
    private const bool c_showHitBox = false;

    /// <summary>
    /// Until this reaches zero, the player does not appear.
    /// </summary>
    internal byte PlayerWaitTimer = 120;
    
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
            if(playerBullet == null) throw new Exception("No bullet in flight.");

            return playerBullet.Position;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="screen"></param>
    internal PlayerShip(VideoDisplay screen)
    {
        Position.X = Dimensions.Width / 2 + 1; // player starts on the left of screen.
        Position.Y = OriginalDataFrom1978.c_yOfBaseLineAboveWhichThePlayerShipIsDrawnPX; // player position vertically is fixed
        
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

        // stop player moving off screen
        if (Position.X - widthDiv2 + XDirection < 1) return;
        if (Position.X + widthDiv2 + XDirection > OriginalDataFrom1978.c_screenWidthPX - 2) return;

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

        // rather than plot individual pixels, the original has a 8 pixel high sprite, of which half are set.
        videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["PlayerShotSpr"], playerBullet.Position.X, playerBullet.Position.Y - 2);

        // bullet goes upwards
        playerBullet.Position.Y -= OriginalDataFrom1978.c_playerBulletSpeedPX;

        // if the bullet reaches the top, we cancel it (it hit nothing). Player can fire another afterwards.
        if (playerBullet.Position.Y < OriginalDataFrom1978.c_verticalPointWherePlayerBulletsStopPX)
        {
            CancelBullet();
        }
        else
        {
            DrawBulletSprite();
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
        if (alienBulletPosition.Y < hitbox.Top) return false;

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
        return new Rectangle(Position.X - Dimensions.Width / 2 + 2, Position.Y - 7, Dimensions.Width-3, Dimensions.Height);
    }

    /// <summary>
    /// Removes the bullet object, so it can be fired again.
    /// </summary>
    internal void CancelBullet()
    {
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
        playerBullet = new PlayerBullet(Position.X, Position.Y - 9);

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

#pragma warning disable CS0162 // Unreachable code detected
        if (c_showHitBox) videoScreen.DrawRectangle(Color.Blue, HitBox());
#pragma warning restore CS0162 // Unreachable code detected

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
    /// Used for the game, and the lives indicators.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="position"></param>
    internal static void DrawPlayerSpriteAt(VideoDisplay videoScreen, Point position)
    {
        videoScreen.DrawSprite(OriginalSpritesFrom1978.Sprites["Player"], position.X - Dimensions.Width / 2, position.Y - 7);
    }

    /// <summary>
    /// Removes player ship from screen.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="position"></param>
    internal void EraseSprite()
    {
        ErasePlayerSpriteAt(videoScreen, Position);

#pragma warning disable CS0162 // Unreachable code detected
        if (c_showHitBox) videoScreen.DrawRectangle(Color.Black, HitBox());
#pragma warning restore CS0162 // Unreachable code detected
    }

    /// <summary>
    /// Erases the player sprite.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="position"></param>
    internal static void ErasePlayerSpriteAt(VideoDisplay videoScreen, Point position)
    {
        videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites["Player"], position.X - Dimensions.Width / 2, position.Y - 7);
    }
}