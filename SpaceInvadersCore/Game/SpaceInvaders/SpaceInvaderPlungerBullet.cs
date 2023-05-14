using SpaceInvadersCore;
using SpaceInvadersCore.Game;
using System.Drawing;

namespace SpaceInvadersCore.Game.SpaceInvaders
{
    /// <summary>
    /// Represents a plunger bullet.
    /// </summary>
    internal class SpaceInvaderPlungerBullet : SpaceInvaderBulletBase
    {
        //  ████    █       █   █   █   █    ████   █████   ████            ████    █   █   █       █       █████   █████
        //  █   █   █       █   █   █   █   █       █       █   █           █   █   █   █   █       █       █         █
        //  █   █   █       █   █   ██  █   █       █       █   █           █   █   █   █   █       █       █         █
        //  ████    █       █   █   █ █ █   █       ████    ████            ████    █   █   █       █       ████      █
        //  █       █       █   █   █  ██   █  ██   █       █ █             █   █   █   █   █       █       █         █
        //  █       █       █   █   █   █   █   █   █       █  █            █   █   █   █   █       █       █         █
        //  █       █████    ███    █   █    ████   █████   █   █           ████     ███    █████   █████   █████     █

        /*  PlungerShot-1 width: 3 X height: 8

             █ 
             █ 
             █ 
             █ 
             █ 
            ███
   
             █ 
             █ 
             █ 
            ███
             █ 
             █ 
   
             █ 
             █ 
            ███
             █ 
             █ 
             █ 
   
            ███
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
        internal SpaceInvaderPlungerBullet(VideoDisplay videoScreen, int x, int y) : base(videoScreen, x, y, OriginalDataFrom1978.s_plungerBulletIndexedByFrame)
        {
        }

        /// <summary>
        /// When a bullet hits, we use this to confirm it was this bullet.
        /// </summary>
        /// <returns></returns>
        protected override Rectangle HitBox()
        {
            return new Rectangle(Position.X - 1, Position.Y - 3, 3, 6);
        }
    }
}