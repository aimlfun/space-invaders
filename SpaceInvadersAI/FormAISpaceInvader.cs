using SpaceInvadersAI.Learning.AIPlayerAndController;
using SpaceInvadersAI.Learning.Forms;
using SpaceInvadersAI.Learning.Configuration;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using Windows.Devices.Usb;
using SpaceInvadersAI.Graphing;
using SpaceInvadersAI.Learning;

namespace SpaceInvadersAI
{
    /// <summary>
    /// Space Invaders game form, where multiple AI "brains" get to play and learn.
    /// </summary>
    public partial class FormSpaceInvaders : Form
    {
        //    █      ███             ███    ████      █      ███    █████            ███    █   █   █   █     █     ████    █████   ████            █   █    ███
        //   █ █      █             █   █   █   █    █ █    █   █   █                 █     █   █   █   █    █ █    █   █   █       █   █           █   █     █
        //  █   █     █             █       █   █   █   █   █       █                 █     ██  █   █   █   █   █   █   █   █       █   █           █   █     █
        //  █   █     █              ███    ████    █   █   █       ████              █     █ █ █   █   █   █   █   █   █   ████    ████            █   █     █
        //  █████     █                 █   █       █████   █       █                 █     █  ██   █   █   █████   █   █   █       █ █             █   █     █
        //  █   █     █             █   █   █       █   █   █   █   █                 █     █   █    █ █    █   █   █   █   █       █  █            █   █     █
        //  █   █    ███             ███    █       █   █    ███    █████            ███    █   █     █     █   █   ████    █████   █   █            ███     ███

        /// <summary>
        /// This app logs generations to this path.
        /// </summary>
        const string c_logFilePath = @"c:\temp\space-invaders.log";

        /// <summary>
        /// Results of AI rounds are output to the log file.
        /// </summary>
        private StreamWriter? log;

        /// <summary>
        /// Each AI gets its own picture box to display its video screen.
        /// </summary>
        private readonly Dictionary<int, PictureBox> videoScreenPictureBoxes = new();

        /// <summary>
        /// Contains a PictureBox for each graph.
        /// </summary>
        private readonly Dictionary<int, PictureBox> graphPictureBoxes = new();

        /// <summary>
        /// This is the pen used to draw the generation line in picture box that the user clicked on.
        /// </summary>
        private readonly Pen penGenerationUserClickedOn = new(Color.FromArgb(200, 190, 190, 190));

        /// <summary>
        /// Constructor.
        /// </summary>
        public FormSpaceInvaders()
        {
            InitializeComponent();

            penGenerationUserClickedOn.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

            Console.WriteLine($"Logging to: {c_logFilePath}");
            log = new(c_logFilePath)
            {
                AutoFlush = true
            };

        }

        /// <summary>
        /// On form loading, we request the user to configure the AI settings.
        /// If they cancel, we cannot start learning, so we close the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            Show();

            // offer the user the chance to change AI settings before learning begins
            FormAIConfig form = new();

            try
            {
                // user has chance to accept / cancel. 
                // if they cancel, we cannot start learning.
                if (form.ShowDialog() != DialogResult.OK)
                {
                    Close(); // will hit finally and dispose
                    return;
                }
            }
            finally
            {
                // details are transferred to "config", if user clicked ok. So we don't need the form anymore.
                form.Dispose();
            }

            // user confirmed settings, so we can start learning
            StartToPlayOrLearn();
        }

        /// <summary>
        /// User decides are we playing using a templated (saved) brain or learning.
        /// </summary>
        private void StartToPlayOrLearn()
        {
            // are we "learning" or "playing"?
            if (PersistentConfig.Settings.Mode == PersistentConfig.FrameworkMode.learning)
            {
                // using image recognition, the brains take a short while to instantiate
                RemoveAllPictureBoxesAndDisplayMessage("Please wait... Generating AI brains.");
                Application.DoEvents();

                // start the "learning" process
                LearningController.CreateGameController(DisplayResultsOnMainThread, DisplayGraphOnMainThread);

                // enable logging
                LearningController.SetLoggingTo(Log);
            }
            else
            {
                // start the "play" process
                AIPlayController.CreateGameController(DisplayResultsOnMainThread);
            }
        }

        #region PRETTY GRAPHS FOR QUICK LEARNING MODE
        /// <summary>
        /// Draws a graph plotting the performance of each best AI brain over generations.
        /// </summary>
        /// <param name="dataPoints"></param>
        /// <param name="topPerformerInfo"></param>
        private void DisplayGraphOnMainThread(List<StatisticsForGraphs> dataPoints, string topPerformerInfo)
        {
            List<Bitmap> graphs = GetGraphsFromDataPoints(dataPoints, topPerformerInfo);

            // if no picture boxes have been created yet, create them, add them to the flow layout panel, and set their images
            if (graphPictureBoxes.Count == 0)
            {
                for (int i = 0; i < graphs.Count; i++)
                {
                    PictureBox pictureBox = new()
                    {
                        Size = new Size(300, 300),
                        Image = graphs[i]
                    };

                    pictureBox.Click += PictureBox_MouseClick;
                    pictureBox.Paint += PictureBox_Paint;
                    pictureBox.Cursor = Cursors.Cross;

                    graphPictureBoxes.Add(i, pictureBox);
                }
            }
            else
            {
                // update the images of the picture boxes (after disposing of the old ones)
                for (int i = 0; i < graphs.Count; i++)
                {
                    graphPictureBoxes[i].Image?.Dispose();
                    graphPictureBoxes[i].Image = graphs[i];
                }
            }

            // if the flow layout panel doesn't contain the picture boxes, add them
            if (!flowLayoutPanelGames.Controls.Contains(graphPictureBoxes[0]))
            {
                flowLayoutPanelGames.SuspendLayout();
                flowLayoutPanelGames.Controls.Clear();

                for (int i = 0; i < graphs.Count; i++)
                {
                    flowLayoutPanelGames.Controls.Add(graphPictureBoxes[i]);
                }

                flowLayoutPanelGames.ResumeLayout();
            }
        }

        #region DISPLAY GENERATION LINE WHERE USER CLICKS
        /// <summary>
        /// Tracks which generation the user clicked on.
        /// </summary>
        int generationClickedOn = -1;

        /// <summary>
        /// When user clicks on graph, we draw a line to show the generation they clicked on.
        /// As the graph gets more data, the line will move.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox_MouseClick(object? sender, EventArgs e)
        {
            if (sender is null)
            {
                return;
            }

            // work out where the user clicked on the graph
            int x = Cursor.Position.X - ((PictureBox)sender).Left;

            // translate that to a generation number
            generationClickedOn = (int)Math.Round((float)(x - SimpleGraph.s_origin.X) / SimpleGraph.s_xToGeneration);

            // force a repaint of all the graphs, and the line will be drawn on each one
            foreach (PictureBox pictureBox in flowLayoutPanelGames.Controls)
            {
                pictureBox.Invalidate();
            }
        }

        /// <summary>
        /// If user clicked on a generation, we draw a line to show where they clicked, this is the "paint" that adds it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox_Paint(object? sender, PaintEventArgs e)
        {
            // if user hasn't clicked on a generation, we don't draw a line. We know it's not clicked on because it's -1.
            // if sender us null, something is wrong - it should be the PictureBox that we are drawing on. Without it, we don't
            // know the height of it.
            if (generationClickedOn < 0 || sender is null)
            {
                return;
            }

            // Where on the chart does the line go? Origin is the bottom left. We put the line where they clicked. However we
            // want the line to move with the chart (stationary in generation). To do that we store generation rather than X 
            // and compute it.
            // Alas because we remove half the data-points every time we have 1000 points, we have to scale it and correct the
            // generation label accordingly.
            int x = (int)Math.Round(SimpleGraph.s_origin.X + SimpleGraph.s_xToGeneration * generationClickedOn);

            // vertical line where the user clicked (appears on all the graphs)
            e.Graphics.DrawLine(penGenerationUserClickedOn, x, 0, x, ((PictureBox)sender).Height - SimpleGraph.c_marginPXForLabels + 10);

            // Add generation label between labels, and title. It is centred on the vertical line.
            string label = (generationClickedOn +LearningFramework.GenerationMultiplier).ToString();
      
            SizeF size = e.Graphics.MeasureString(label, SimpleGraph.s_defaultAxisLabelFont);
            e.Graphics.DrawString(label, SimpleGraph.s_defaultAxisLabelFont, Brushes.White, x - size.Width / 2, ((PictureBox)sender).Height - size.Height - 13);
        }
        #endregion

        /// <summary>
        /// Render graphs from our game data points.
        /// </summary>
        /// <param name="dataPoints"></param>
        /// <param name="topPerformerInfo"></param>
        /// <returns></returns>
        private static List<Bitmap> GetGraphsFromDataPoints(List<StatisticsForGraphs> dataPoints, string topPerformerInfo)
        {
            // slice up the data points into separate lists of data for the graphs.

            List<float> scores = dataPoints.Select(x => (float)x.Score).ToList(); // the overall game score (what humans judge each other on)
            List<float> killsAvoided = dataPoints.Select(x => (float)x.KillsAvoided).ToList(); // player was in the path of a bullet when it was fired
            List<float> playerShotsFired = dataPoints.Select(x => (float)x.Shots).ToList(); // how many times the player shot at something
            List<float> level = dataPoints.Select(x => (float)x.Level).ToList(); // what level the player reached
            List<float> invadersKilled = dataPoints.Select(x => (float)x.InvadersKilled).ToList(); // how many invaders the player slaughtered
            List<float> saucersKilled = dataPoints.Select(x => (float)x.SaucersKilled).ToList(); // how many saucers the player managed to kill
            List<float> shields = dataPoints.Select(x => (float)x.ShieldsShot).ToList(); // how many times the AI shot its own shields
            List<float> fitness = dataPoints.Select(x => (float)x.FitnessScore).ToList(); // how fit the AI was
            List<float> missed = dataPoints.Select(x => (float)x.Missed).ToList(); // how many times the AI fired but missed everything
            List<float> lives = dataPoints.Select(x => (float)x.Lives).ToList(); // what level the player reached
            List<float> frames = dataPoints.Select(x => (float)x.GameFrames).ToList(); // how many frames the player reached

            // there are 55 invaders per level, so we can use the number of invaders killed to work out how far through the level the player reached,
            // this makes for a more interesting graph than just the level number.
            for (int i = 0; i < level.Count; i++)
            {
                // add the "part" of level based of the fact there is 55 invaders per level. However if they kill all 55, and we're doing one level we don't want to make it "0" progress hence the +1
                level[i] += (float)Math.Round((invadersKilled[i] % (55 + (PersistentConfig.Settings.AIOneLevelOnly ? 1 : 0))) / 55, 2);
            }

            // graph 1 - the score graph, with details about the top performer. In reality if you want AI to complete every level, it's more about lives and invaders killed than score.
            using SimpleGraph scoreGraph = new(300, 300, topPerformerInfo);
            scoreGraph.AddDataSet(scores, Color.FromArgb(100, 255, 255, 255), "score");
            Bitmap graphOfScore = scoreGraph.Plot();

            // graph 2 - compares shots fired vs. kills etc, all on the same graph. 
            using SimpleGraph shotsGraph = new(300, 300, "Measure Of Accuracy", 5);
            shotsGraph
                .AddDataSet(playerShotsFired, Color.FromArgb(100, 0, 255, 0), "shots")
                .AddDataSet(missed, Color.FromArgb(100, 255, 100, 160), "missed")
                .AddDataSet(invadersKilled, Color.FromArgb(100, 255, 0, 0), "invaders")
                .AddDataSet(saucersKilled, Color.FromArgb(100, 255, 255, 0), "saucers");

            // if not playing with shields, don't show the shields graph
            if (PersistentConfig.Settings.AIPlaysWithShields)
            {
                shotsGraph.AddDataSet(shields, Color.FromArgb(100, 160, 100, 10), "shields");
            }
            Bitmap graphOfShots = shotsGraph.Plot();

            // graph 3 - how many times the player was in the path of a bullet when it was fired, and avoided it
            using SimpleGraph killsAvoidedGraph = new(300, 300, "Measure of Bullet Avoidance", 1);
            killsAvoidedGraph.AddDataSet(killsAvoided, Color.FromArgb(100, 0, 100, 255), "avoided");
            Bitmap graphOfKillsAvoided = killsAvoidedGraph.Plot();

            // graph 4 - shows the evolution of the level reached over time
            using SimpleGraph levelGraph = new(300, 300, "Measure of Level", 1);
            levelGraph.AddDataSet(level, Color.Yellow, "level");
            Bitmap graphLevel = levelGraph.Plot();

            // graph 5 - fitness graph, showing incremental improvements that aren't just score
            using SimpleGraph scoreFitnessGraph = new(300, 300, "Fitness");
            scoreFitnessGraph.AddDataSet(fitness, Color.FromArgb(100, 255, 255, 255), "fitness");
            Bitmap graphOfFitness = scoreFitnessGraph.Plot();

            // graph 6 - lives graph, which is definitely important
            using SimpleGraph livesGraph = new(300, 300, "Lives", 1);
            livesGraph.AddDataSet(lives, Color.Pink, "lives");
            Bitmap graphLives = livesGraph.Plot();

            // graph 7 - frames graph, less frames = quicker winning
            using SimpleGraph framesGraph = new(300, 300, "Frames", 100);
            framesGraph.AddDataSet(frames, Color.CadetBlue, "frames");
            Bitmap graphFrames = framesGraph.Plot();

            // the graphs as bitmaps to display in the picture box.
            return new() { graphOfScore, graphOfFitness, graphOfShots, graphOfKillsAvoided, graphLevel, graphLives, graphFrames };
        }
       
        #endregion

        /// <summary>
        /// This called when the game changes to display the new "updated" screens abd is surprisingly crazy fast.
        /// It is called every 8ms. 
        /// I wanted to be able to see all 100 games at once,  so I added 100 PictureBox's to the form. 
        /// After each game frame, it calls this and it updates the PictureBox's with the new video screen.
        /// I am running on an i7-9750H 2.60Ghz that bursts up 3Ghz+. If you look in the Video display class
        /// you will see that I have optimised the video update code significantly. First iterations used
        /// Graphics.DrawUnscaledImage() to paint the invaders and bullets. Aside from slower, that became 
        /// impractical when support for removing chunks of the shield when hit by bullets.
        /// </summary>
        /// <param name="bitmaps">List of Bitmaps, one bitmap per AI brain (a video screen each)</param>
        private void DisplayResultsOnMainThread(List<Bitmap> bitmaps)
        {
            // Make a copy of the list to avoid modifying the original while the UI thread is accessing it

            List<Bitmap> bitmapsCopy = bitmaps.ToList();

            CreatePictureBoxesToDisplayGameScreens(bitmapsCopy);

            DisplayGameVideoScreensInPictureBoxes(bitmapsCopy);

            bitmapsCopy.Clear();
        }

        /// <summary>
        /// Displays a video screen for each AI in their respective PictureBox.
        /// </summary>
        /// <param name="bitmapsCopy"></param>
        private void DisplayGameVideoScreensInPictureBoxes(List<Bitmap> bitmapsCopy)
        {
            // now we update the picture boxes with the new bitmaps
            int screenNumber = 0;

            foreach (var bitmap in bitmapsCopy)
            {
                PictureBox pictureBox = videoScreenPictureBoxes[screenNumber];

                if (pictureBox.Image != bitmap) // if the image is the same, don't bother updating it (for example AI is dead)
                {
                    pictureBox.Image?.Dispose();
                    pictureBox.Image = bitmap;
                }

                ++screenNumber;
            }
        }

        /// <summary>
        /// Each AI brain gets its own picture box, we know how many because there will be one per item in List<Bitmap>.
        /// Creating a PictureBox for each brain every paint would be slow, so we create them once and reuse them.
        /// </summary>
        /// <param name="bitmapsCopy"></param>
        private void CreatePictureBoxesToDisplayGameScreens(List<Bitmap> bitmapsCopy)
        {
            int screenNumber = 0;

            // when it's "play" mode we create a picture box double height/width.
            int width = 224 * (bitmapsCopy.Count == 1 ? 2 : 1); // dimension of SI screen 224 x 256
            int height = 256 * (bitmapsCopy.Count == 1 ? 2 : 1);

            // first time we add picture boxes to the flow layout panel one per AI brain
            if (videoScreenPictureBoxes.Count == bitmapsCopy.Count) return;

            // we need to add more picture boxes to the flow layout panel

            flowLayoutPanelGames.SuspendLayout();
            flowLayoutPanelGames.Controls.Clear();

            foreach (var bitmap in bitmapsCopy)
            {
                PictureBox pictureBox = new()
                {
                    Width = width, // dimension of SI screen 224 x 256
                    Height = height,
                    BorderStyle = BorderStyle.FixedSingle,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };

                flowLayoutPanelGames.Controls.Add(pictureBox);

                videoScreenPictureBoxes.Add(screenNumber, pictureBox);
                ++screenNumber;
            }

            flowLayoutPanelGames.ResumeLayout();
        }

        /// <summary>
        /// Keyboard handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormSpaceInvaders_KeyDown(object sender, KeyEventArgs e)
        {
            // the keys don't do anything if we are in playing mode...
            if (PersistentConfig.Settings.Mode == PersistentConfig.FrameworkMode.playing) return;

            switch (e.KeyCode)
            {
                // quick learning mode
                case Keys.Q:
                    if (!LearningController.InQuickLearningMode)
                    {
                        RemoveAllPictureBoxesAndDisplayMessage("Please wait... Quick learning mode initiated - progress graphs will appear at the end of each generation."); // unless you want a spurious error on wake-up/hot-reload.");
                    }

                    LearningController.InQuickLearningMode = !LearningController.InQuickLearningMode;
                    break;

                // draw visualisations at *next* mutation
                case Keys.V:
                    LearningController.CreateVisualisation();
                    break;

                // captures the best brain and saves it as a template int the /Templates folder
                case Keys.B:
                    LearningController.s_learningFramework?.SaveBestBrainAsTemplate();
                    MessageBox.Show("Brain visualisation saved as template in /Templates folder.");
                    break;

                // pause the game, although ignored if in quick learning mode
                case Keys.P:
                    LearningController.Paused = !LearningController.Paused;
                    break;
            }
        }

        /// <summary>
        /// We destroy the game PictureBox's on screen *before* entering quick learning mode.
        /// </summary>
        private void RemoveAllPictureBoxesAndDisplayMessage(string message)
        {
            flowLayoutPanelGames.SuspendLayout();
            flowLayoutPanelGames.Controls.Clear();
            flowLayoutPanelGames.ResumeLayout();

            videoScreenPictureBoxes.Clear();

            Label labelPleaseWait = new()
            {
                Text = message,
                ForeColor = Color.White,
                Font = new Font("Arial", 18),
                AutoSize = true
            };

            flowLayoutPanelGames.Controls.Add(labelPleaseWait);
        }

        /// <summary>
        /// Writes a line of text to the log file.
        /// </summary>
        /// <param name="text"></param>
        private void Log(string text)
        {
            log?.WriteLine(text);
        }

        /// <summary>
        /// Form closing handler. Closes the log file, and stops any background threads.
        /// We have to catch this. If we don't, the form will close but the background threads will continue to run, and
        /// it won't actually close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormSpaceInvaders_FormClosing(object sender, FormClosingEventArgs e)
        {
            // exit quick learning mode before closing the form
            if (LearningController.InQuickLearningMode)
            {
                LearningController.SetQuickLearningMode(false);

                Application.DoEvents();
            }

            // finish our logging
            CloseLogFile();
        }

        /// <summary>
        /// Flush, close, dispose...
        /// </summary>
        private void CloseLogFile()
        {
            log?.Flush();
            log?.Close();
            log?.Dispose();
            
            log = null;
        }
    }
}