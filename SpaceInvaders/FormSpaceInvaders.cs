using SpaceInvadersCore;
using SpaceInvadersCore.Game;
using System.Diagnostics;
using Timer = System.Windows.Forms.Timer;

namespace SpaceInvaders
{
    /// <summary>
    /// Form for playing Space Invaders.
    /// </summary>
    public partial class FormSpaceInvaders : Form
    {
        //   ███    ████      █      ███    █████            ███    █   █   █   █     █     ████    █████   ████     ███            █   █    ███
        //  █   █   █   █    █ █    █   █   █                 █     █   █   █   █    █ █    █   █   █       █   █   █   █           █   █     █
        //  █       █   █   █   █   █       █                 █     ██  █   █   █   █   █   █   █   █       █   █   █               █   █     █
        //   ███    ████    █   █   █       ████              █     █ █ █   █   █   █   █   █   █   ████    ████     ███            █   █     █
        //      █   █       █████   █       █                 █     █  ██   █   █   █████   █   █   █       █ █         █           █   █     █
        //  █   █   █       █   █   █   █   █                 █     █   █    █ █    █   █   █   █   █       █  █    █   █           █   █     █
        //   ███    █       █   █    ███    █████            ███    █   █     █     █   █   ████    █████   █   █    ███             ███     ███

        /// <summary>
        /// The game controller (creates the invaders, the player, moves, draws etc).
        /// </summary>
        private readonly GameController gameController;

        /// <summary>
        /// The timer that controls the game loop.
        /// </summary>
        private readonly Timer gamePlayAnimationTimer;

        /// <summary>
        /// This is the video screen that the game is drawn on.
        /// </summary>
        private readonly VideoDisplay videoScreen;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FormSpaceInvaders()
        {
            InitializeComponent();

            // every 8ms, the game will be updated and drawn.
            gamePlayAnimationTimer = new Timer
            {
                Interval = 8 // ms
            };

            gamePlayAnimationTimer.Tick += PlayGameFrameByFrame;

            videoScreen = new();

            // this is the "controller" for the game, and handles everything
            gameController = new(videoScreen, 0);
        }

        /// <summary>
        /// Upon loading, it starts the game loop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            // start the game loop
            gamePlayAnimationTimer.Start();
        }

        /// <summary>
        /// Plays the game, until it returns "IsGameOver=true".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayGameFrameByFrame(object? sender, EventArgs e)
        {
            gameController.Play();

            // show the back-buffered game image on the screen
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = videoScreen.GetVideoDisplayContent();

            // sure I didn't complete everything on the game. This can be added to later if you want.
            if (gameController.IsGameOver)
            {
                Debug.WriteLine("GAME OVER");
                gamePlayAnimationTimer.Stop();
                return;
            }
        }

        /// <summary>
        /// Handle keyboard controls.
        /// [LEFT]  <- player
        /// [RIGHT] -> player
        /// [SPACE] Fire
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormSpaceInvaders_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    gameController.SetPlayerMoveDirectionToLeft();
                    break;

                case Keys.Right:
                    gameController.SetPlayerMoveDirectionToRight();
                    break;

                case Keys.Space:
                    gameController.RequestPlayerShipFiresBullet();
                    break;

                // pause <> un-pause the game
                case Keys.P:
                    gamePlayAnimationTimer.Enabled = !gamePlayAnimationTimer.Enabled;
                    break;

                // steps thru speeds, making the game slower to aid with debugging.
                case Keys.S:
                    gamePlayAnimationTimer.Interval *= 2;

                    if (gamePlayAnimationTimer.Interval > 1000)
                    {
                        gamePlayAnimationTimer.Interval = 8; // ms
                    }

                    break;

                case Keys.Escape:
                    Close();
                    break;
            }
        }

        /// <summary>
        /// User can hold down keys to move left / right.
        /// On letting go of those, we stop moving the "player" ship.
        /// We don't stop moving just because the space bar is key-up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormSpaceInvaders_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    gameController.CancelMove();
                    break;

                case Keys.Right:
                    gameController.CancelMove();
                    break;
            }
        }
    }
}