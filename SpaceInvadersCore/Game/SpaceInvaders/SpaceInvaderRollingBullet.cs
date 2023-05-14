using SpaceInvadersCore;
using SpaceInvadersCore.Game;
using System.Drawing;

namespace SpaceInvadersCore.Game.SpaceInvaders;

/// <summary>
/// Represents a rolling shot bullet.
/// </summary>
internal class SpaceInvaderRollingBullet : SpaceInvaderBulletBase
{
    //  ████     ███    █       █        ███    █   █    ████           ████    █   █   █       █       █████   █████
    //  █   █   █   █   █       █         █     █   █   █               █   █   █   █   █       █       █         █
    //  █   █   █   █   █       █         █     ██  █   █               █   █   █   █   █       █       █         █
    //  ████    █   █   █       █         █     █ █ █   █               ████    █   █   █       █       ████      █
    //  █ █     █   █   █       █         █     █  ██   █  ██           █   █   █   █   █       █       █         █
    //  █  █    █   █   █       █         █     █   █   █   █           █   █   █   █   █       █       █         █
    //  █   █    ███    █████   █████    ███    █   █    ████           ████     ███    █████   █████   █████     █
    
    /*
    RollShot-1 width: 3 X height: 8
     █ 
     █ 
     █ 
     █ 
     █ 
     █ 
     █ 

     █ 
     █ 
    ██ 
     ██
     █ 
    ██ 
     ██

     █ 
     █ 
     █ 
     █ 
     █ 
     █ 
     █ 

     ██
    ██ 
     █ 
     ██
    ██ 
     █ 
     █ 
     
     */

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="videoScreen"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    internal SpaceInvaderRollingBullet(VideoDisplay videoScreen, int x, int y) : base(videoScreen, x, y, OriginalDataFrom1978.s_rollingBulletIndexedByFrame)
    {
    }

    /// <summary>
    /// When a bullet hits, we use this to confirm it was this bullet.
    /// </summary>
    /// <returns></returns>
    protected override Rectangle HitBox()
    {
        if (lastFrameDrawn == 0 || lastFrameDrawn == 2)
            return new Rectangle(
                x: Position.X,
                y: Position.Y - 3,
                width: 1,
                height: 7);
        else
            return new Rectangle(
                x: Position.X - 1,
                y: Position.Y - 3,
                width: 3,
                height: 7);
    }
}