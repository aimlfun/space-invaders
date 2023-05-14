using SpaceInvadersCore;
using SpaceInvadersCore.Game;
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
        Position = new Point(x, y - 3); // -3, because top half of bullet is transparent
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
        return videoScreen.DrawSpriteWithCollisionDetection(OriginalSpritesFrom1978.Sprites["PlayerShotSpr"], Position.X, Position.Y - 2);
    }
}