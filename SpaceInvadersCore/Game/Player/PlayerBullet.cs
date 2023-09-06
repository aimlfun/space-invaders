using System.Drawing;

namespace SpaceInvadersCore.Game.Player;

/// <summary>
/// Represents a bullet fired by the player.
/// </summary>
internal class PlayerBullet
{
    //  ████    █         █     █   █   █████   ████            ████    █   █   █       █       █████   █████
    //  █   █   █        █ █    █   █   █       █   █           █   █   █   █   █       █       █         █
    //  █   █   █       █   █    █ █    █       █   █           █   █   █   █   █       █       █         █
    //  ████    █       █   █     █     ████    ████            ████    █   █   █       █       ████      █
    //  █       █       █████     █     █       █ █             █   █   █   █   █       █       █         █
    //  █       █       █   █     █     █       █  █            █   █   █   █   █       █       █         █
    //  █       █████   █   █     █     █████   █   █           ████     ███    █████   █████   █████     █

    internal enum BulletState { normalMovement, explosionInProgress };

    /// <summary>
    /// When exploding, it sets a timer, and the bullet only disappears when the timer reaches 0.
    /// </summary>
    internal int BlowUpTimer;

    /// <summary>
    /// 
    /// </summary>
    internal BulletState State;

    ///   █
    ///   █   <<<--- bullet
    ///   █
    ///   █
    ///   
    /// <summary>
    /// Position of bullet on screen.
    /// </summary>
    internal Point Position;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    internal PlayerBullet(int x, int y)
    {
        Position = new Point(x, y - 2); // -2 ensures it doesn't erase part of the player ship
        State = BulletState.normalMovement;
        BlowUpTimer = 16;
    }

    /// <summary>
    /// Draws the bullet (a line).
    /// </summary>
    /// <param name="videoScreen"></param>
    internal bool DrawSprite(VideoDisplay videoScreen)
    {
        /*
            PlayerShotSpr width: 1 X height: 8
            .
            .  <- top half is transparent
            .
            .
            █
            █
            █
            █            
         */
        switch (State)
        {
            case BulletState.normalMovement:
                return videoScreen.DrawSpriteWithCollisionDetection(OriginalSpritesFrom1978.Get("PlayerShotSpr"), Position.X, Position.Y - 2, 255 /*249*/);

            case BulletState.explosionInProgress:
                videoScreen.DrawSprite(OriginalSpritesFrom1978.Get("ShotExploding"), Position.X - 5, Position.Y, 255 /*249*/);
                return false; // no collision detection for explosion
        }

        throw new ApplicationException("unknown state?");
    }

    /// <summary>
    /// Erases the sprite (explosion or bullet).
    /// </summary>
    /// <param name="videoScreen"></param>
    internal void EraseSprite(VideoDisplay videoScreen)
    {
        switch (State)
        {
            case BulletState.normalMovement:
                // rather than plot individual pixels, the original has a 8 pixel high sprite, of which half are set.
                videoScreen.EraseSprite(OriginalSpritesFrom1978.Get("PlayerShotSpr"), Position.X, Position.Y - 2);
                break;

            case BulletState.explosionInProgress:
                videoScreen.EraseSprite(OriginalSpritesFrom1978.Get("ShotExploding"), Position.X - 5, Position.Y);
                break;
        }
    }
}