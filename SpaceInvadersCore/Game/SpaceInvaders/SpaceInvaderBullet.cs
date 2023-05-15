using SpaceInvadersCore;
using SpaceInvadersCore.Game;
using System.Drawing;

namespace SpaceInvadersCore.Game.SpaceInvaders
{
    /// <summary>
    /// Base class for all three Space Invader bullets, featuring core functionality.
    /// </summary>
    internal class SpaceInvaderBulletBase
    {
        //   ███    █   █   █   █     █     ████    █████   ████            ████    █   █   █       █       █████   █████    ███
        //    █     █   █   █   █    █ █    █   █   █       █   █           █   █   █   █   █       █       █         █     █   █
        //    █     ██  █   █   █   █   █   █   █   █       █   █           █   █   █   █   █       █       █         █     █
        //    █     █ █ █   █   █   █   █   █   █   ████    ████            ████    █   █   █       █       ████      █      ███
        //    █     █  ██   █   █   █████   █   █   █       █ █             █   █   █   █   █       █       █         █         █
        //    █     █   █    █ █    █   █   █   █   █       █  █            █   █   █   █   █       █       █         █     █   █
        //   ███    █   █     █     █   █   ████    █████   █   █           ████     ███    █████   █████   █████     █      ███

        // 
        //    Plunger   Rolling   Squiggly
        //    
        //       █         █         █ 
        //       █         █          █ 
        //       █        ██           █
        //      ███        ██         █ 
        //       █         █         █ 
        //       █        ██          █ 
        //                 ██          █ 
        //                 

        /// <summary>
        /// Set this to aid debugging bullet hitboxes.
        /// </summary>
        internal const bool c_showHitBox = false;

        /// <summary>
        /// Indicates whether the bullet is dead.
        /// </summary>
        internal bool IsDead = false;

        /// <summary>
        /// Indicates when the bullet has hit something (collision detection drawing sprite).
        /// </summary>
        internal bool BulletHitSomething = false;

        /// <summary>
        /// Indicates the player was close enough to a bullet to have been hit, but they moved out of the way.
        /// </summary>
        internal bool BulletAvoided = false;

        /// <summary>
        /// We count steps to determine when the reload.
        /// </summary>
        internal int Steps = 0;

        /// <summary>
        /// Position of bullet on screen.
        /// </summary>
        internal Point Position;

        /// <summary>
        /// The last position of the bullet on screen.
        /// </summary>
        internal Point LastPosition;

        /// <summary>
        /// There are 4 frames of animation for each bullet type. This is used to step thru them.
        /// </summary>        
        protected int frameOfImage = 0;

        /// <summary>
        /// Stores the last frame drawn so we can erase it.
        /// </summary>
        protected int lastFrameDrawn = 0;

        /// <summary>
        /// The frames of animation for the bullet.
        /// </summary>
        protected string[] frames;

        /// <summary>
        /// This is the video screen we are drawing the bullet on.
        /// </summary>
        private readonly VideoDisplay videoScreen;

        /// <summary>
        /// Constructor for all alien bullets.
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="frames"></param>
        internal SpaceInvaderBulletBase(VideoDisplay screen, int x, int y, string[] frames)
        {
            y -= 2; // move it up closer to the alien

            videoScreen = screen;
            Position = new Point(x, y);
            LastPosition= new Point(0, 0); // prevents erasing the bullet before it moves; it is only drawn after moving.

            this.frames = frames;
        }

        /// <summary>
        /// Draws the bullet at the current position.
        /// </summary>
        internal virtual bool DrawSprite()
        {
            if (IsDead) return false; // dead sprites/bullets should not be drawn!

            LastPosition = Position;
            frameOfImage = (frameOfImage + 1) % 4; // step thru the frames of animation
            lastFrameDrawn = frameOfImage; // store so we can erase it later

#pragma warning disable CS0162 // Unreachable code detected
            if (c_showHitBox) videoScreen.DrawRectangle(Color.Blue, HitBox());
#pragma warning restore CS0162 // Unreachable code detected

            BulletHitSomething = videoScreen.DrawSpriteWithCollisionDetection(OriginalSpritesFrom1978.Sprites[frames[frameOfImage]], Position.X - 1, Position.Y - 3);

            return BulletHitSomething; // return true if sprite collided with something
        }

        /// <summary>
        /// Move bullet downwards at the desired speed. When there are less invaders, the bullets fall faster.
        /// </summary>
        /// <param name="SpaceInvadersRemaining"></param>
        internal void Move(int SpaceInvadersRemaining)
        {
            EraseSprite();

            if (IsDead) return;

            // The normal delta Y for the shots is a constant 4 pixels down per step. 

            // But when there are 8 or fewer aliens on the screen the delta changes up
            // to 5 pixels per step (5*60/3 = 100 pixels per second).
            Position.Y += SpaceInvadersRemaining < 8 ? 5 : 4;

            ++Steps;
        }

        /// <summary>
        /// When a bullet hits, we use this to confirm it was this bullet.
        /// </summary>
        /// <returns></returns>
        protected virtual Rectangle HitBox()
        {
            throw new Exception("override this method");
        }

        /// <summary>
        /// Detects whether a bullet hit or not.
        /// </summary>
        /// <param name="bulletLocation"></param>
        /// <returns></returns>
        internal bool BulletHit(Point bulletLocation)
        {
            if (IsDead) return false; // dead bullet don't hit anything.

            Rectangle hitbox = HitBox();
            return hitbox.Contains(new Point(bulletLocation.X, bulletLocation.Y + 2)) || hitbox.Contains(new Point(bulletLocation.X, bulletLocation.Y + 5));
        }

        /// <summary>
        /// Erases the bullet.
        /// </summary>
        internal void EraseSprite()
        {
            if (LastPosition.Y == 0) return; // nothing to erase

#pragma warning disable CS0162 // Unreachable code detected
            if (c_showHitBox) videoScreen.DrawRectangle(Color.Black, HitBox());
#pragma warning restore CS0162 // Unreachable code detected

            videoScreen.EraseSprite(OriginalSpritesFrom1978.Sprites[frames[lastFrameDrawn]], LastPosition.X - 1, LastPosition.Y - 3);
        }

        /// <summary>
        /// When the bullet explodes, we remove an explosion area.
        /// </summary>
        internal void ExplodeBullet()
        {
            EraseSprite();
        }
    }
}