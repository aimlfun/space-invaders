using SpaceInvadersAI.Learning.Configuration;
using SpaceInvadersAI.AI;
using System.Diagnostics;
using SpaceInvadersAI.AI.Utilities;
using System;
using SpaceInvadersCore;

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
            // uncomment this to test the rule matching. It didn't warrant a unit test per se.
            // AIPlayController.TestTemplateRuleMatching();

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
            LoadBrainConfiguredForLevel(PersistentConfig.Settings.AIStartLevel, PersistentConfig.Settings.AIStartScore);
        }

        /// <summary>
        /// It matches the level to a rule, and returns the template filename for that rule.
        /// The rule can be a single level, a range of levels, or a sequence of levels.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="lastScore"></param>
        /// <param name="startScore"></param>
        /// <returns></returns>
        private static string GetTemplateFileNameForLevel(int level, int lastScore, out int startScore)
        {
            foreach (RuleLevelBrain rule in PersistentConfig.Settings.BrainTemplates.Values)
            {
                if (IsLevelInRule(level, rule.LevelRule))
                {
                    if (lastScore == 0) startScore = rule.StartingScore; else startScore = lastScore;

                    return rule.BrainTemplateFileName;
                }
            }

            startScore = lastScore;

            // last one is the default
            return PersistentConfig.Settings.BrainTemplates.Values.ToArray()[^1].BrainTemplateFileName;
        }
        
        /// <summary>
        /// Rather minimal testing of the rule matching.
        /// Tests it for 1.80, which more than proves it works (rules start at less than 11 an rely on "+n").
        /// </summary>
        internal static void TestTemplateRuleMatching()
        {
            // test GetTemplateFileNameForLevel() for level=1..80
            for (int level = 1; level <= 80; level++)
            {
                string template = GetTemplateFileNameForLevel(level, 0, out int startScore);
                Debug.WriteLine($"Level {level} = {template} (start score = {startScore})");
            }

            Debugger.Break();
        }

        /// <summary>
        /// Determine if the level matches the rule.
        /// Rules can be
        /// * a single level (1)
        /// * a range of levels (1-5)
        /// * a sequence of levels (1,5,8)
        /// * a sequence with repeating offsets ( 12,+1,+1,+8,..   meaning 12,13,14,22,23,24,32,..)
        /// </summary>
        /// <param name="level"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        private static bool IsLevelInRule(int level, string rule)
        {
            rule = rule.Replace(" ", ""); // remove spaces otherwise exact match won't work

            // e.g. a specific level such as "1"
            if (rule == level.ToString())
            {
                return true;
            }

            // e.g. level 1-5
            // if it's a range, then check if the level is in the range
            if (rule.Contains('-'))
            {
                // 1-5
                string[] rangeParts = rule.Split('-');

                // [0] = 1
                // [1] = 5
                int rangeStart = int.Parse(rangeParts[0]);
                int rangeEnd = int.Parse(rangeParts[1]);

                // is level in range?
                if (level >= rangeStart && level <= rangeEnd)
                {
                    // yes, so return the template
                    return true;
                }
            }

            // it's not a specific level, and it's not a range, so it must be a list. But it's not a list if it doesn't contain a comma.
            if (!rule.Contains(',')) return false;

            // e.g. level 1,5,8 ...                
            // and more advanced rules, meaning whatever whole number, compute the next number based on the offsets
            // 12,+1,+1,+8,..   meaning 12,13,14,22,23,24,32,.. etc

            // split the rule into parts separated by commas
            string[] parts = rule.Split(',');

            // the first part must be a number that the offsets are for.
            // the 2nd part could be a number or an offset
            int baseNumber = int.Parse(parts[0]); // first must be a number

            // the first number in the rule matches the level, so no need to process the parts
            if (baseNumber == level)
            {
                return true;
            }

            // if the base number is greater than the level, then the rule doesn't match, no need to process the parts
            if (baseNumber < level)
            {
                // parse the part and expand the +n to the actual levels
                // 12,+1,+1,+8,..   meaning 12,13,14,22,23,24,32,.. etc

                // make an array of offsets from part string then we'll start with the base number and repeatedly add the offsets
                // to it until we get to a level matching or greater than the level we're looking for
                List<int> offsets = new();

                foreach (string part in parts)
                {
                    if (part == "..." || part == "..") continue; // ignore these, they are just for readability

                    // e.g 5,6,+1,+4,.. => matches 5, matches 6, 7,11,15. 6 becomes our base number
                    if (!part.StartsWith("+"))
                    {
                        // maybe the number is the level we're looking for
                        if (level.ToString() == part)
                        {
                            return true;
                        }

                        // it wasn't, so it's now our base number
                        baseNumber = int.Parse(part);
                        
                        offsets.Clear(); // offsets have to be off the last number, because repeat doesn't work unless we start doing complicated things

                        // such as "6" in the above example
                        continue;
                    }

                    string number = part.TrimStart('+');

                    offsets.Add(int.Parse(number)); // build offsets from base number
                }

                // if we have no offsets, then we're done - we can't match the level using offsets
                if (offsets.Count == 0) return false;

                // we have a "baseNumber" (starting number) and a list of offsets that apply there after.

                // now we can start adding the offsets to the base number until we get to the level we're looking for, or go past it
                while (baseNumber < level)
                {
                    // each time we add the offsets to the base number, we get a new base number
                    for (int k = 0; k < offsets.Count; k++)
                    {
                        baseNumber += offsets[k];

                        // matches the one we want?
                        if (baseNumber == level) return true;

                        // no need to continue if we've gone past the level we're looking for
                        if (baseNumber > level) break;
                    }
                }

                // the search is inefficient, but it's only done once per level and I am ok for now.
                // one could cache the next 50 numbers or something, but I don't think it's worth it.
            }

            return false; // didn't match
        }

        /// <summary>
        /// Rather than have 1 brain for all levels, we can have a brain for each level or one for a range of levels.
        /// The level is passed in and we match it to the correct brain template based on configured rules.
        /// </summary>
        /// <param name="level"></param>
        private static void LoadBrainConfiguredForLevel(int level, int initialScore = -1)
        {
            string levelFile = GetTemplateFileNameForLevel(level, s_player is null || s_player.gameController is null ? 0 : s_player.brainControllingPlayerShip.AIPlayer.gameController.Score, out int startScore);
            Debug.WriteLine($"level: {level} filename: {levelFile}");

            Brain brain = Brain.CreateFromTemplate(File.ReadAllText(levelFile));

            brain.Name = $"AI";
            brain.Provenance = $"TRAINED";

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

            // ensure that if the user asked for an initial score, we honour their request.
            if (initialScore != -1) startScore = initialScore;

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
            
            if(s_player.gameController.IsGameOver)
            {
                s_timerMove.Stop(); // that ends everything moving
                s_player.gameController.WriteGameOverToVideoScreen(); // last draw will include game over
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