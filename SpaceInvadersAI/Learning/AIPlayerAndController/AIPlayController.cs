using SpaceInvadersAI.Learning.Configuration;
using SpaceInvadersAI.AI;
using System.Diagnostics;
using SpaceInvadersAI.AI.Utilities;
using System;

namespace SpaceInvadersAI.Learning.AIPlayerAndController
{
    /// <summary>
    /// The play controller.
    /// It plays the game using what it has learnt and you have configured.
    /// </summary>
    internal static class AIPlayController
    {
        //  ████    █         █     █   █            ███     ███    █   █   █████   ████     ███    █       █       █████   ████
        //  █   █   █        █ █    █   █           █   █   █   █   █   █     █     █   █   █   █   █       █       █       █   █
        //  █   █   █       █   █    █ █            █       █   █   ██  █     █     █   █   █   █   █       █       █       █   █
        //  ████    █       █   █     █             █       █   █   █ █ █     █     ████    █   █   █       █       ████    ████
        //  █       █       █████     █             █       █   █   █  ██     █     █ █     █   █   █       █       █       █ █
        //  █       █       █   █     █             █   █   █   █   █   █     █     █  █    █   █   █       █       █       █  █
        //  █       █████   █   █     █              ███     ███    █   █     █     █   █    ███    █████   █████   █████   █   █

        /// <summary>
        /// Used to display the Space Invaders AI games on the UI.
        /// </summary>
        /// <param name="bitmaps"></param>
        internal delegate void DisplayResultsOnMainThread(List<Bitmap> bitmaps);

        /// <summary>
        /// Tracks the player in the "game".
        /// </summary>
        internal static AIPlayer s_player;

        /// <summary>
        /// Used as a protection against the timer firing multiple times in quick succession causing re-entrance.
        /// </summary>
        private static bool blockTick = false;

        /// <summary>
        /// Frame by frame moving / drawing by using a timer.
        /// </summary>
        internal static System.Windows.Forms.Timer s_timerMove = new();

        /// <summary>
        /// This is used to update the display in non quiet mode to show the game.
        /// </summary>
        internal static event DisplayResultsOnMainThread? UIThreadInterfaceForGameScreens;

        /// <summary>
        /// Creates a game controller.
        /// </summary>
        /// <param name="canvas"></param>
        internal static void CreateGameController(DisplayResultsOnMainThread displayResultsOnMainThread)
        {
            // interfaces to UI thread
            UIThreadInterfaceForGameScreens = displayResultsOnMainThread;

            StopTimer();

            s_timerMove.Interval = 10;
            s_timerMove.Tick += TimerMove_Tick;

            InitialisePlayer();
            s_timerMove.Start();
        }

        /// <summary>
        /// Event callback when the level changes.
        /// </summary>
        /// <param name="level"></param>
        private static void GameController_OnLevelChanged(int level)
        {
            LoadBrainConfiguredForLevel(level);
        }

        /// <summary>
        /// Initialise the player for level 1.
        /// </summary>
        private static void InitialisePlayer()
        {
            LoadBrainConfiguredForLevel(PersistentConfig.Settings.AIStartLevel);
        }

        /// <summary>
        /// Matches our level rules to return the correct template.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static string GetTemplateFileNameForLevel(int level, int lastScore, out int startScore)
        {
            foreach (int i in PersistentConfig.Settings.BrainTemplates.Keys)
            {
                RuleLevelBrain rule = PersistentConfig.Settings.BrainTemplates[i];

                // e.g. level 1
                if (rule.LevelRule == level.ToString())
                {
                    startScore = rule.StartingScore;
                    return rule.BrainTemplateFileName;
                }

                // e.g. level 1-5
                // if it's a range, then check if the level is in the range
                if (rule.LevelRule.Contains('-'))
                {
                    string[] parts = rule.LevelRule.Split('-');
                    int start = int.Parse(parts[0]);
                    int end = int.Parse(parts[1]);

                    if (level >= start && level <= end)
                    {
                        if (level == start) startScore = rule.StartingScore; else startScore = lastScore;
                        return rule.BrainTemplateFileName;
                    }
                }

                // e.g. level 1,5,8 ...
                if (rule.LevelRule.Contains(','))
                {
                    string[] parts = rule.LevelRule.Split(',');
                    foreach (string part in parts)
                    {
                        if (part == level.ToString())
                        {
                            if (part == parts[0]) startScore = rule.StartingScore; else startScore = lastScore;
                            return rule.BrainTemplateFileName;
                        }
                    }
                }
            }

            startScore = lastScore;

            // last one is the default
            return PersistentConfig.Settings.BrainTemplates.Values.ToArray()[^1].BrainTemplateFileName;
        }

        /// <summary>
        /// Rather than have 1 brain for all levels, we can have a brain for each level or one for a range of levels.
        /// The level is passed in and we match it to the correct brain template based on configured rules.
        /// </summary>
        /// <param name="level"></param>
        private static void LoadBrainConfiguredForLevel(int level)
        {
            string levelFile = GetTemplateFileNameForLevel(level, s_player is null || s_player.gameController is null?0: s_player.brainControllingPlayerShip.AIPlayer.gameController.Score,  out int startScore);
            Debug.WriteLine($"level: {level} filename: {levelFile}");

            Brain brain = Brain.CreateFromTemplate(File.ReadAllText(levelFile));

            brain.Name = $"AI";
            brain.Provenance = "TRAINED";


            // WARNING: score has to be zeroed if trained with it zeroed, OR set to the last score at the end of the level before

            if (s_player is null)
            {
                s_player = new AIPlayer(brain, 1);
                s_player.gameController.OnLevelChanged += GameController_OnLevelChanged;
            }
            else
            {
                s_player.brainControllingPlayerShip?.Dispose();
                s_player.brainControllingPlayerShip = brain;
                brain.AIPlayer = s_player;
            }

            s_player.gameController.AISetScore(startScore);
        }

        /// <summary>
        /// Stops the game timer.
        /// </summary>
        internal static void StopTimer()
        {
            s_timerMove.Stop();
        }

        /// <summary>
        /// Pressing "S" slows things down, 2x slower, 5x slower, 10x slower, then back to normal speed.
        /// </summary>
        internal static void StepThroughSpeeds()
        {
            var newInterval = s_timerMove.Interval switch
            {
                10 => 20,
                20 => 50,
                50 => 100,
                _ => 10,
            };

            s_timerMove.Interval = newInterval;
        }

        /// <summary>
        /// Timer tick moves the game forwards.
        /// </summary>
        private static void TimerMove_Tick(object? sender, EventArgs e)
        {
            if (blockTick) return;

            blockTick = true;

            // in visual mode, move and if none left move to next generation
            if (!AIPlayController.Move())
            {
                s_timerMove.Stop();

                EndGame();

                s_timerMove.Start();

                blockTick = false;
                return;
            }

            // paint
            Draw();

            blockTick = false;
        }

        /// <summary>
        /// At the end of the game, we need to assess brain fitness (determine which perform the task well
        /// and which do not).
        /// </summary>
        private static void EndGame()
        {
            s_timerMove.Stop();
        }

        /// <summary>
        /// Moves all the AI player ships.
        /// </summary>
        /// <returns>True - able to move at least one player.</returns>
        internal static bool Move()
        {
            s_player.Move();

            return !s_player.IsDead;
        }

        /// <summary>
        /// Draws the players / games.
        /// </summary>
        internal static void Draw()
        {
            // if we don't have an handler assigned, there is no point in drawing anything.
            if (UIThreadInterfaceForGameScreens is null)
            {
                return;
            }

            // force each player to draw their game screen, which we'll then pass to the event handler.
            List<Bitmap> gameScreensForEachAIPlayer = new()
            {
                s_player.Draw()
            };

            // updates all the game screens on the main UI thread.
            UIThreadInterfaceForGameScreens.Invoke(gameScreensForEachAIPlayer);

            // emptying won't free, because the bitmaps are in the PictureBox's.
            gameScreensForEachAIPlayer.Clear();
        }
    }
}