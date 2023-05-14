using System.Security.Cryptography;
using System.ComponentModel;
using SpaceInvadersAI.AI;
using SpaceInvadersAI.Learning.Fitness;
using SpaceInvadersAI.AI.Visualisation;
using SpaceInvadersAI.Learning.Configuration;
using System.Diagnostics;
using SpaceInvadersAI.Graphing;
using Microsoft.VisualBasic.Logging;

namespace SpaceInvadersAI.Learning.AIPlayerAndController
{
    /// <summary>
    /// A learning "controller".
    /// 
    /// The controller is responsible for:
    /// * Defining the games parameters such as what brain inputs and outputs are used.
    ///   - it provides the create game logic
    ///   - it provides the reset game logic
    /// * Support quick learning mode (no visuals, faster performing generations) and visual mode (slower performing generations)
    ///   - that includes the game loop (includes parallel game play), via a timer (or thread)
    ///   - the background worker thread for quick mode
    /// * Providing a hook up from game to the display
    /// </summary>
    internal static class LearningController
    {
        //  █       █████     █     ████    █   █    ███    █   █    ████            ███     ███    █   █   █████   ████     ███    █       █       █████   ████
        //  █       █        █ █    █   █   █   █     █     █   █   █               █   █   █   █   █   █     █     █   █   █   █   █       █       █       █   █
        //  █       █       █   █   █   █   ██  █     █     ██  █   █               █       █   █   ██  █     █     █   █   █   █   █       █       █       █   █
        //  █       ████    █   █   ████    █ █ █     █     █ █ █   █               █       █   █   █ █ █     █     ████    █   █   █       █       ████    ████
        //  █       █       █████   █ █     █  ██     █     █  ██   █  ██           █       █   █   █  ██     █     █ █     █   █   █       █       █       █ █
        //  █       █       █   █   █  █    █   █     █     █   █   █   █           █   █   █   █   █   █     █     █  █    █   █   █       █       █       █  █
        //  █████   █████   █   █   █   █   █   █    ███    █   █    ████            ███     ███    █   █     █     █   █    ███    █████   █████   █████   █   █                                                                                                                                                      

        #region DELEGATES   
        /// <summary>
        /// Used to provide the UI with a graph.
        /// </summary>
        /// <param name="dataPoints"></param>
        /// <param name="topPerformerInfo"></param>
        internal delegate void NotifyUIGraph(List<StatisticsForGraphs> dataPoints, string topPerformerInfo);

        /// <summary>
        /// Used to display the Space Invaders AI games on the UI.
        /// </summary>
        /// <param name="bitmaps"></param>
        internal delegate void DisplayResultsOnMainThread(List<Bitmap> bitmaps);
        #endregion

        #region PRIVATE PROPERTIES
        /// <summary>
        /// true = game is running in quick learn mode (no visuals, faster performing generations).
        /// </summary>
        private static bool s_inQuickLearningMode = false;

        /// <summary>
        /// Whilst running in "quick learn" mode, we use a background worker.
        /// Normally I do not, but in this instance spurious errors arose regarding Bitmaps being disposed.
        /// </summary>
        private static BackgroundWorker? workerForQuickLearningMode;

        /// <summary>
        /// true = game is paused (timer stopped).
        /// </summary>
        private static bool s_isPaused = false;

        /// <summary>
        /// Used as a protection against the timer firing multiple times in quick succession causing re-entrance of the timer method.
        /// </summary>
        private static bool blockTick = false;
        #endregion

        #region INTERNAL PROPERTIES
        /// <summary>
        /// Represents the learning framework that handles the brains with mutation.
        /// </summary>
        internal static LearningFramework? s_learningFramework;

        /// <summary>
        /// When this reaches 0, generation has run it's time and fitness assessment with mutation occurs
        /// prior to next generation.
        /// </summary>
        internal static int s_movesBeforeMutation = 0;

        /// <summary>
        /// Generation being evaluated.
        /// </summary>
        internal static int s_generation = 0;

        /// <summary>
        /// Tracks the players in the "game".
        /// </summary>
        internal readonly static Dictionary<int, AIPlayer> s_players = new();

        /// <summary>
        /// Method to handle logging.
        /// </summary>
        internal static Action<string>? s_log;

        /// <summary>
        /// Frame by frame moving / drawing by using a timer.
        /// </summary>
        internal static System.Windows.Forms.Timer s_timerMove = new();

        /// <summary>
        /// This is used to update the display in non quick learn mode to show the game.
        /// </summary>
        internal static event DisplayResultsOnMainThread? UIThreadInterfaceForGameScreens;

        /// <summary>
        /// This is used for quick learning mode to plot progress graphs after each generation.
        /// </summary>
        internal static event NotifyUIGraph? UIThreadInterfaceForDisplayingGraphs;
        #endregion

        #region GETTERS AND SETTERS
        /// <summary>
        /// Setter/getter for quick learning mode.
        /// </summary>
        internal static bool InQuickLearningMode
        {
            get
            {
                return s_inQuickLearningMode;
            }

            set
            {
                // no change?
                if (s_inQuickLearningMode == value) return;

                SetQuickLearningMode(value);
            }
        }

        /// <summary>
        /// Get or set the "paused" status.
        /// </summary>
        internal static bool Paused
        {
            get
            {
                return s_isPaused;
            }

            set
            {
                if (s_isPaused == value) return; // no change
                if (s_inQuickLearningMode) return; // what's the point of pausing in quick mode?

                s_isPaused = value;

                // "P" pauses the timer (and what's happening)
                s_timerMove.Enabled = !s_isPaused; // fyi, all the MS code for these: ".Start() => Enabled = true;" etc.

                Draw(); // force it to draw
            }
        }
        #endregion

        /// <summary>
        /// Creates a game controller.
        /// </summary>
        /// <param name="canvas"></param>
        internal static void CreateGameController(DisplayResultsOnMainThread displayResultsOnMainThread, NotifyUIGraph graphDisplayOnMainThread)
        {
            workerForQuickLearningMode = new BackgroundWorker();
            workerForQuickLearningMode.DoWork += WorkerForQuickLearningMode_DoWork;
            workerForQuickLearningMode.WorkerSupportsCancellation = true;
            workerForQuickLearningMode.ProgressChanged += WorkerForQuickLearningMode_ProgressChanged;
            workerForQuickLearningMode.WorkerReportsProgress = true;

            // interfaces to UI thread
            UIThreadInterfaceForGameScreens = displayResultsOnMainThread;
            UIThreadInterfaceForDisplayingGraphs = graphDisplayOnMainThread;

            s_timerMove.Enabled = false;
            s_timerMove.Interval = 10;
            s_timerMove.Tick += TimerMove_Tick;

            s_generation = 0;

            // depending on the options chosen, the inputs vary.
            GetAIInputOutputs(out string[] inputParameters, out string[] outputParameters);

            string template = "";

            if (PersistentConfig.Settings.Template is not null && File.Exists(PersistentConfig.Settings.Template)) template = File.ReadAllText(PersistentConfig.Settings.Template);

            s_learningFramework = new LearningFramework(
                inputParameterNames: inputParameters,
                outputParameterNames: outputParameters, // what we require "out" for controlling player
                playerCreation: CreateNewPlayer,
                suppliedLayers: PersistentConfig.Settings.AIHiddenLayers,
                template: template,
                population: PersistentConfig.Settings.ConcurrentGames,
                topBrainsToPreservePercentage: PersistentConfig.Settings.PercentOfBrainsPreservedDuringMutation,
                randomBrainsToCreatePercentage: PersistentConfig.Settings.PercentOfBrainsToCreateAsNewRandomDuringMutation,
                brainsCreatedFromTemplatePercentage: string.IsNullOrWhiteSpace(template) ? 0 : PersistentConfig.Settings.PercentOfBrainsToCreateFromTemplate,
                mutationAmount: PersistentConfig.Settings.NumberOfTimesASingleBrainIsMutatedInOneGeneration,
                mutationMethods: PersistentConfig.Settings.AllowedMutationMethods,
                cellTypeRatios: PersistentConfig.Settings.CellTypeRatios,
                chanceOfMutationPercentage: PersistentConfig.Settings.PercentChanceABrainIsPickedForMutation,
                numberOfNeurons: PersistentConfig.Settings.DesiredRandomNeurons,
                createRandomNetwork: PersistentConfig.Settings.CreatingARandomNetwork,
                allowedFunctions: PersistentConfig.Settings.AllowedActivationFunctions,
                selectionType: PersistentConfig.Settings.SelectionType);

            s_learningFramework.ResetGame += LearnManager_GameReset;
            s_learningFramework.StartGame += LearnManager_StartGame;

            s_generation = 0;

            s_learningFramework.StartLearning();
            s_timerMove.Start();
        }

        /// <summary>
        /// Toggles the game between quick and non quick learning mode.
        /// </summary>
        internal static void SetQuickLearningMode(bool quickLearnMode)
        {
            if (workerForQuickLearningMode is null) throw new Exception("Worker for quick learning mode is null");

            if (quickLearnMode)
            {
                // we're going from non-quick mode to quick mode (no updating picture boxes every 8ms).

                // we need to stop the timer and then start the background worker.
                s_inQuickLearningMode = true; // <-- this needs to be BEFORE the timer is stopped.
                s_timerMove.Stop();
                Application.DoEvents(); // clear up any pending UI events.

                workerForQuickLearningMode.RunWorkerAsync();
            }
            else
            {
                // we're going from quick mode to non quick mode.
                // we need to stop the background worker and then start the timer.

                workerForQuickLearningMode.CancelAsync();

                // because it is async, we need to wait for it to finish.
                while (workerForQuickLearningMode.IsBusy)
                {
                    Application.DoEvents();
                }

                blockTick = false;
                s_timerMove.Start();

                s_inQuickLearningMode = false; // <-- this needs to be AFTER the timer is started.
            }
        }

        /// <summary>
        /// Depending on settings provide the correct input and output parameters for the AI.
        /// </summary>
        /// <param name="inputParameters"></param>
        /// <param name="outputParameters"></param>
        private static void GetAIInputOutputs(out string[] inputParameters, out string[] outputParameters)
        {
            if (PersistentConfig.Settings.AIAccessInternalData)
            {
                // labels for the input neurons
                List<string> inputs = new()
                {
                    $"player-position-x",

                    // AI is informed where the player bullet is. I am not convinced this helps.
                    $"player-bullet-y",

                    // AI is informed of the whereabout of any bullets (danger)
                    $"alien-rolling-bullet-x",
                    $"alien-rolling-bullet-y",

                    $"alien-plunger-bullet-x",
                    $"alien-plunger-bullet-y",

                    $"alien-squiggly-bullet-x",
                    $"alien-squiggly-bullet-y"
                };

                // indicator for each of alive or dead
                for (int alienIndex = 0; alienIndex < 55; alienIndex++)
                {
                    inputs.Add($"alien-alive{alienIndex}");
                }

                // everything is relative to this "reference" alien
                inputs.Add($"reference-alien-x");
                inputs.Add($"reference-alien-y");

                // AI is informed of the direction/speed the aliens are moving.
                inputs.Add($"alien-x-direction");

                // AI is informed of the location of the saucer, and direction (l2r or r2l)
                inputs.Add($"saucer-x-direction");
                inputs.Add($"saucer-x");
                inputs.Add($"is-under-shield");

                inputParameters = inputs.ToArray();

            }
            else
            {
                // just a bunch of pixels. 3584 of them representing the screen.
                List<string> inputs = new();

                // indicator for each of alive or dead
                for (int pixelIndex= 0; pixelIndex < 56* 64; pixelIndex++)
                {
                    inputs.Add($"{pixelIndex}");
                }

                inputParameters = inputs.ToArray();
            }

            if (!PersistentConfig.Settings.UseActionFireApproach)
                outputParameters = new string[] { "desired-position", "fire" }; // what we require "out" for controlling player
            else
                outputParameters = new string[] { "action" }; // what we require "out" for controlling player
        }

        /// <summary>
        /// Fires at the end of a game, to update the graph with the learning progress.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void WorkerForQuickLearningMode_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            // plot a graph of performance over time
            if (s_learningFramework is not null) UIThreadInterfaceForDisplayingGraphs?.Invoke(s_learningFramework.performanceOfBestPlayerPerGeneration, s_learningFramework.bestBrainInfo);
        }

        /// <summary>
        /// Background worker to run the game in "quick learn mode" (no drawing).
        /// This is the same thing that happens when we use the timer, but hugely faster as it doesn't draw to bitmap and isn't moving ever 8ms (it's moving as quick as the CPU can manage).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void WorkerForQuickLearningMode_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (sender is null || sender is not BackgroundWorker) throw new Exception("sender is null or not a BackgroundWorker");

            BackgroundWorker bw = (BackgroundWorker)sender;

            // until the user takes it out of "quick learn mode" we run in a closed high performant loop (move without paint)
            while (!bw.CancellationPending)
            {
                // move the player until end of game
                while (!bw.CancellationPending && LearningController.Move())
                {
                    // do nothing, just move
                }

                // We exit the above loop if they cancel, rather than because it's the end of game, so we have to be careful here and
                // check if cancelled.
                if (!bw.CancellationPending)
                {
                    EndGame();
                    bw.ReportProgress(0);
                }
            }

            // If the operation was cancelled by the user, set the .Cancel property to true.
            if (bw.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Defines where logging goes to.
        /// </summary>
        /// <param name="writeToLog"></param>
        internal static void SetLoggingTo(Action<string> writeToLog)
        {
            if (s_learningFramework is null) return;

            s_log = writeToLog;
            s_learningFramework.SetUILog(writeToLog);
        }

        /// <summary>
        /// true = output brains (best in generation) along with cohorts (if uncommented). Don't leave it on or it will fill the disk!
        /// </summary>
        internal static void CreateVisualisation()
        {
            Visualiser.s_visualisationsEnabled = true;
        }

        /// <summary>
        /// Forces a premature end to the game.
        /// Available to a UI to force it.
        /// </summary>
        internal static void ForceNextGeneration()
        {
            EndGame();
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
            if (blockTick)
            {
                s_movesBeforeMutation++; // ensure this doesn't count as a move.
                return;
            }

            blockTick = true;

            // in visual mode, move and if none left move to next generation
            if (!LearningController.Move())
            {
                s_timerMove.Stop();

                EndGame();

                s_timerMove.Start();

                blockTick = false;
                return;
            }

            // paint (non quick learn mode)
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

            int fitness = 0;

            foreach (var x in s_players.Values)
            {
                x.brainControllingPlayerShip.RealScore = x.gameController.Score;

                // update high score for this player
                if (x.gameController.Score > x.brainControllingPlayerShip.HighScore)
                {
                    x.brainControllingPlayerShip.HighScore = x.gameController.Score;
                }

                // update brain fitness            
                x.brainControllingPlayerShip.Fitness = FitnessScoring.GetFitness(x.gameController, out string fitnessExplained);
                x.brainControllingPlayerShip.FitnessSummary = fitnessExplained;

                AssignPlayerSummaryToTopPerformingBrain(x);

                // we don't want the brain adding lots of pointless neurons unless it has a proportionate affect on fitness
                // with a low number, it may be fairly impossible for it to grow healthily, so we don't penalise it until 12 or more
                if (x.brainControllingPlayerShip.Fitness > 0 &&
                    x.brainControllingPlayerShip.GenomeSize > (float)x.brainControllingPlayerShip.BrainInputs.Count * RarelyModifiedSettings.MinimumGenomeSizeNotToBePunishedForGrowthDisproportionateToScoreImprovement)
                {
                    s_learningFramework?.LogWriter($"{x.brainControllingPlayerShip.Name} genome size exceeded 1.x input count");

                    float improvementRatio = x.brainControllingPlayerShip.LastFitness == 0 ? 1 : x.brainControllingPlayerShip.Fitness / x.brainControllingPlayerShip.LastFitness; // increase in fitness > 1, decrease <1
                    float growthRatio = x.brainControllingPlayerShip.LastOverallGenomeSize == 0 ? 0 : x.brainControllingPlayerShip.OverallGenomeSize / x.brainControllingPlayerShip.LastOverallGenomeSize; // increase in size >1, decrease <1

                    // Compare growth to improvement, if growth is disproportionately greater than improvement, optionally penalise the brain.
                    // Optionality allows some to proceed, and this may result in long term gain.

                    // 50% chance of penalising
                    if (improvementRatio < growthRatio && growthRatio != 0 && growthRatio != 1 && RandomNumberGenerator.GetInt32(0, 100) < 50)
                    {
                        s_learningFramework?.LogWriter($"{x.brainControllingPlayerShip.Name} fitness penalised");
                        x.brainControllingPlayerShip.Fitness *= x.brainControllingPlayerShip.OverallGenomeSize / growthRatio;
                    }
                }

                // >0 means it had "some" fitness, increment the counter
                if (x.brainControllingPlayerShip.Fitness > 0) ++fitness;
            }

            // now let the AI pick what to do next (mutation wise).
            s_learningFramework?.EndLearning();
        }

        /// <summary>
        /// This appears on the top-left of the graph.
        /// </summary>
        /// <param name="aiPlayer"></param>
        private static void AssignPlayerSummaryToTopPerformingBrain(AIPlayer aiPlayer)
        {
            aiPlayer.brainControllingPlayerShip.PlayerSummary = $"EPOCH: {s_generation}\nNEURONS: {aiPlayer.brainControllingPlayerShip.GenomeSize}\n" +
                $"SCORE: {aiPlayer.brainControllingPlayerShip.RealScore}\n" +
                $"{aiPlayer.gameController.KillsAvoided} bullet(s) avoided\n" +
                ((aiPlayer.gameController.Shots == 0) ? "" : $"{Math.Round(((float)aiPlayer.gameController.NumberOfInvadersKilled / (float)aiPlayer.gameController.Shots) * 100)}% accuracy") + // add player Shots/Kills ratio/hit base etc.
                $"\n{aiPlayer.gameController.NumberOfTimesShieldsWereShotByPlayer} shot(s) hit shield";
        }

        /// <summary>
        /// Handle anything related to resetting the game.
        /// </summary>
        private static void LearnManager_GameReset()
        {
            s_timerMove.Stop();
            
            foreach (var x in s_players.Values) x.Dispose();

            s_players.Clear();
        }

        /// <summary>
        /// Starts the game.
        /// </summary>
        /// <param name="generation"></param>
        private static void LearnManager_StartGame(int generation)
        {
            s_generation++;

            if (PersistentConfig.Settings.MovesBeforeMutation < -2) PersistentConfig.Settings.MovesBeforeMutation = -1;

            // start the timer
            s_movesBeforeMutation = PersistentConfig.Settings.MovesBeforeMutation;
            PersistentConfig.Settings.MovesBeforeMutation = (int)Math.Round((float)PersistentConfig.Settings.MovesBeforeMutation * 102f / 100f);

            if (!s_inQuickLearningMode) s_timerMove.Start();
        }

        /// <summary>
        /// Creates a new player with an assigned brain and id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="brain"></param>
        private static void CreateNewPlayer(int id, Brain brain)
        {
            AIPlayer newPlayer = new(brain, id);
            newPlayer.gameController.SetHighScore(brain.HighScore);

            s_players.Add(newPlayer.brainControllingPlayerShip.Id, newPlayer);
            
            // WARNING: score has to be zeroed if trained on zeroed OR set to the last score at the end of the level before
            newPlayer.gameController.AISetScore(PersistentConfig.Settings.AIStartScore);
        }

        /// <summary>
        /// Moves all the AI player ships.
        /// </summary>
        /// <returns>True - able to move at least one player.</returns>
        internal static bool Move()
        {
            bool parallel = true; // change to false to debug

            // -1 means no limit
            if ( s_movesBeforeMutation != -1 && --s_movesBeforeMutation < 1)
            {
                return false; // kill them all to "end" game
            }

            if (parallel)
            {
                // iterate over concurrent games in parallel
                Parallel.ForEach(s_players, s => { s.Value.Move(); });
            }
            else
            {
                foreach (var x in s_players.Values)
                {
                    x.Move();
                }
            }

            // we have to do this outside of the parallel loop.
            int alive = 0;

            // find the highest score of the dead players
            int maxDeadScore = 0;

            foreach (var x in s_players.Values)
            {
                if (x.IsDead && x.gameController.Score > maxDeadScore)
                {
                    maxDeadScore = x.gameController.Score;
                }
            }

            // determine a threshold score based on how far the dead players reached.
            maxDeadScore = (int)Math.Round((float)maxDeadScore * PersistentConfig.Settings.PercentageOfDeadScoreThreshold);

            // kill off the players that are not doing well, i.e. < 90% of the dead players score.
            foreach (var x in s_players.Values)
            {
                if (!x.IsDead)
                {
                    // if it hasn't achieve 90% of best score, and its prior high score is not 90% of best score, kill it.
                    // this runs the risk of killing off a slow shooting brain that may end up with a high score, but it's
                    // a trade off to speed up processing. We only cull if it's on its last life.
                    if (x.gameController.Score < maxDeadScore && 
                        x.brainControllingPlayerShip.HighScore < maxDeadScore && 
                        x.brainControllingPlayerShip.Provenance != "Elite" &&  // not usually wise to kill off an elite
                        x.gameController.Lives == 1) // only kill off if it's on its last life
                    {
                        Debug.WriteLine($"Killing off brain: {x.brainControllingPlayerShip.Name} score: {x.gameController.Score} high-score: {x.brainControllingPlayerShip.HighScore} provenance: {x.brainControllingPlayerShip.Provenance}");
                        x.gameController.AbortGame();
                    }
                    else
                        ++alive;
                }
            }

            return (alive > 0);
        }

        /// <summary>
        /// Draws the players / games.
        /// </summary>
        internal static void Draw()
        {
            // in quick learn mode, we do not draw the screens, this speeds up processing.
            if (s_inQuickLearningMode)
            {
                return;
            }

            // if we don't have an handler assigned, there is no point in drawing anything.
            if (UIThreadInterfaceForGameScreens is null)
            {
                return;
            }

            // force each player to draw their game screen, which we'll then pass to the event handler.
            List<Bitmap> gameScreensForEachAIPlayer = new();

            foreach (var x in s_players.Values)
            {
                gameScreensForEachAIPlayer.Add(x.Draw());
            }

            // updates all the game screens on the main UI thread.
            UIThreadInterfaceForGameScreens.Invoke(gameScreensForEachAIPlayer);

            // emptying won't free, because the bitmaps are in the PictureBox's.
            gameScreensForEachAIPlayer.Clear();
        }
    }
}