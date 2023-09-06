using System.Drawing;

namespace SpaceInvadersCore.Game.SpaceInvaders
{
    /// <summary>
    /// Base class for all three Space Invader bullets, featuring core functionality.
    /// </summary>
    internal class SpaceInvaderBulletBase
    {
        internal enum BulletState { normalMovement, explosionInProgress, dead };

        //   ███    █   █   █   █     █     ████    █████   ████            ████    █   █   █       █       █████   █████    ███
        //    █     █   █   █   █    █ █    █   █   █       █   █           █   █   █   █   █       █       █         █     █   █
        //    █     ██  █   █   █   █   █   █   █   █       █   █           █   █   █   █   █       █       █         █     █
        //    █     █ █ █   █   █   █   █   █   █   ████    ████            ████    █   █   █       █       ████      █      ███
        //    █     █  ██   █   █   █████   █   █   █       █ █             █   █   █   █   █       █       █         █         █
        //    █     █   █    █ █    █   █   █   █   █       █  █            █   █   █   █   █       █       █         █     █   █
        //   ███    █   █     █     █   █   ████    █████   █   █           ████     ███    █████   █████   █████     █      ███

        // 
        //    Plunger   Rolling   Squiggly   AShotExplo
        //    
        //       █         █         █          █ 
        //       █         █          █       █   █ 
        //       █        ██           █        ██ █
        //      ███        ██         █        ████ 
        //       █         █         █        █ ███ 
        //       █        ██          █        █████
        //                 ██          █      █ ███ 
        //                                     █ █ █

        /// <summary>
        /// Indicates whether the bullet is moving or exploding (or dead).
        /// </summary>
        internal BulletState State;

        /// <summary>
        /// When it reaches the bottom, it explodes. This timer determines how long it is visible for.
        /// </summary>
        internal int BulletExplodeTimer;

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
            State = BulletState.normalMovement;
            BulletExplodeTimer = 4;
        }

        /// <summary>
        /// Draws the bullet at the current position.
        /// </summary>
        internal virtual bool DrawSprite()
        {
            LastPosition = Position;

            switch (State)
            {
                case BulletState.normalMovement:
                    frameOfImage = (frameOfImage + 1) % 4; // step thru the frames of animation
                    lastFrameDrawn = frameOfImage; // store so we can erase it later

                    if (DebugSettings.c_debugSpaceInvaderBulletDrawHitBox) videoScreen.DrawRectangle(Color.Blue, HitBox());

                    BulletHitSomething = videoScreen.DrawSpriteWithCollisionDetection(OriginalSpritesFrom1978.Get(frames[frameOfImage]), Position.X - 1, Position.Y - 3);
                    break;

                case BulletState.explosionInProgress:
                    videoScreen.DrawSprite(OriginalSpritesFrom1978.Get("AShotExplo"), Position.X - 2, Position.Y - 3, 255);
                    BulletHitSomething = false;
                    break;

                case BulletState.dead:
                    BulletHitSomething = false; // dead sprites/bullets should not be drawn!
                    break;
            }

            return BulletHitSomething; // return true if sprite collided with something
        }

        /// <summary>
        /// Move bullet downwards at the desired speed. When there are less invaders, the bullets fall faster.
        /// </summary>
        /// <param name="SpaceInvadersRemaining"></param>
        internal void Move(int SpaceInvadersRemaining)
        {
            EraseSprite();

            if (State == BulletState.explosionInProgress && BulletExplodeTimer-- == 0)
            {
                State = BulletState.dead;
            }

            if (State != SpaceInvaderBulletBase.BulletState.normalMovement) return;

            /*
              SpeedShots:
                ; With less than 9 aliens on the screen the alien shots get a tad bit faster. Probably
                ; because the advancing rack can catch them.
                
                08D8: 3A 82 20        LD      A,(numAliens)       ; Number of aliens on screen
                08DB: FE 09           CP      $09                 ; More than 8?
                08DD: D0              RET     NC                  ; Yes ... leave shot speed alone [CP $09 sets carry flag if A >= $09]
                08DE: 3E FB           LD      A,$FB               ; Normally FF (-4) ... now FB (-5)
                08E0: 32 7E 20        LD      (alienShotDelta),A  ; Speed up alien shots
                08E3: C9              RET                         ; Done
             */

            // The disassembly contradicts itself in the attribute definitions, it says...
            // @ $207E	alienShotDelta	Alien shot speed. Normally -1 but set to -4 with less than 9 aliens

            // The normal delta Y for the shots is a constant 4 pixels down per step.
            // The "-4"/"-5" above is because Cartesian (0,0) the invader screen uses, is bottom right and ours is top left

            // But when there are 8 or fewer aliens on the screen the delta changes up
            // to 5 pixels per step (5*60/3 = 100 pixels per second).

            Position.Y += SpaceInvadersRemaining >= 9 ? 4 : 5; // corrected on 22 May 23, to be "9" not 8.       

            ++Steps;
        }

        /// <summary>
        /// When a bullet hits, we use this to confirm it was this bullet.
        /// </summary>
        /// <returns></returns>
        protected virtual Rectangle HitBox()
        {
            throw new ApplicationException("override this method");
        }

        /// <summary>
        /// Detects whether a bullet hit or not.
        /// </summary>
        /// <param name="bulletLocation"></param>
        /// <returns></returns>
        internal bool BulletHit(Point bulletLocation)
        {
            if (State == SpaceInvaderBulletBase.BulletState.dead) return false; // dead bullet don't hit anything.

            Rectangle hitbox = HitBox();

            return hitbox.Contains(new Point(bulletLocation.X, bulletLocation.Y + 2)) || hitbox.Contains(new Point(bulletLocation.X, bulletLocation.Y + 5));
        }

        /// <summary>
        /// Erases the bullet.
        /// </summary>
        internal void EraseSprite()
        {
            if (LastPosition.Y == 0) return; // nothing to erase

            if (DebugSettings.c_debugSpaceInvaderBulletDrawHitBox) videoScreen.DrawRectangle(Color.Black, HitBox());

            switch (State)
            {
                case BulletState.normalMovement:
                    videoScreen.EraseSprite(OriginalSpritesFrom1978.Get(frames[lastFrameDrawn]), LastPosition.X - 1, LastPosition.Y - 3);
                    break;

                case BulletState.explosionInProgress:
                    videoScreen.EraseSprite(OriginalSpritesFrom1978.Get("AShotExplo"), Position.X - 2, Position.Y - 3);
                    break;

                case BulletState.dead:
                    break;
            }
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