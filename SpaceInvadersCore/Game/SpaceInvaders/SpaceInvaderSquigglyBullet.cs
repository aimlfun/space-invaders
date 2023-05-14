using SpaceInvadersCore;
using SpaceInvadersCore.Game;
using System.Drawing;

namespace SpaceInvadersCore.Game.SpaceInvaders;

/// <summary>
/// Represents a squiggly bullet.
/// </summary>
internal class SpaceInvaderSquigglyBullet : SpaceInvaderBulletBase
{
    //   ███     ███    █   █    ███     ████    ████   █       █   █           ████    █   █   █       █       █████   █████
    //  █   █   █   █   █   █     █     █       █       █       █   █           █   █   █   █   █       █       █         █
    //  █       █   █   █   █     █     █       █       █        █ █            █   █   █   █   █       █       █         █
    //   ███    █   █   █   █     █     █       █       █         █             ████    █   █   █       █       ████      █
    //      █   █ █ █   █   █     █     █  ██   █  ██   █         █             █   █   █   █   █       █       █         █
    //  █   █   █  █    █   █     █     █   █   █   █   █         █             █   █   █   █   █       █       █         █
    //   ███     ██ █    ███     ███     ████    ████   █████     █             ████     ███    █████   █████   █████     █
    
    /*
    SquigglyShot-1 width: 3 X height: 8
     █ 
    █  
     █ 
      █
     █ 
    █  
     █ 

    █  
     █ 
      █
     █ 
    █  
     █ 
      █

     █ 
      █
     █ 
    █  
     █ 
      █
     █ 

      █
     █ 
    █  
     █ 
      █
     █ 
    █              
     */

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>

    internal SpaceInvaderSquigglyBullet(VideoDisplay videoScreen, int x, int y) : base(videoScreen, x, y, OriginalDataFrom1978.s_squigglyBulletIndexedByFrame)
    {
    }

    /// <summary>
    /// When a bullet hits, we use this to confirm it was this bullet.
    /// </summary>
    /// <returns></returns>
    protected override Rectangle HitBox()
    {
        return new Rectangle(Position.X - 1, Position.Y - 3, 3, 7);
    }
}