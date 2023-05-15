using SpaceInvadersAI.AI;
using SpaceInvadersAI.Learning.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.Graphing
{
    /// <summary>
    /// Keep a list of interesting metrics for graphs.
    /// </summary>
    internal class StatisticsForGraphs
    {
        //   ████   ████      █     ████    █   █            ███    █████     █     █████    ███     ███    █████    ███     ███     ███
        //  █       █   █    █ █    █   █   █   █           █   █     █      █ █      █       █     █   █     █       █     █   █   █   █
        //  █       █   █   █   █   █   █   █   █           █         █     █   █     █       █     █         █       █     █       █
        //  █       ████    █   █   ████    █████            ███      █     █   █     █       █      ███      █       █     █        ███
        //  █  ██   █ █     █████   █       █   █               █     █     █████     █       █         █     █       █     █           █
        //  █   █   █  █    █   █   █       █   █           █   █     █     █   █     █       █     █   █     █       █     █   █   █   █
        //   ████   █   █   █   █   █       █   █            ███      █     █   █     █      ███     ███      █      ███     ███     ███

        /// <summary>
        /// What score the AI reached (score per the game).
        /// </summary>
        internal int Score;

        /// <summary>
        /// What level the AI got to.
        /// </summary>
        internal int Level = 0;

        /// <summary>
        /// How many invaders the AI killed.
        /// </summary>
        internal int InvadersKilled = 0;

        /// <summary>
        /// How many saucers the AI killed.
        /// </summary>
        internal int SaucersKilled = 0;

        /// <summary>
        /// How many shots the AI fired.
        /// </summary>
        internal int Shots = 1;

        /// <summary>
        /// Hpw many times the AI avoided being killed.
        /// </summary>
        internal int KillsAvoided = 0;

        /// <summary>
        /// How many times the shields were shot by the AI.
        /// </summary>
        internal int ShieldsShot = 0;

        /// <summary>
        /// How many AI shots missed saucers and invaders.
        /// </summary>
        internal int Missed = 0;

        /// <summary>
        /// How we scored the AI based on all the inputs about performance.
        /// </summary>
        internal float FitnessScore = 0;

        /// <summary>
        /// How many lives the AI had.
        /// </summary>
        internal int Lives = 0;

        /// <summary>
        /// The number of game frames that have passed before death.
        /// </summary>
        internal int GameFrames = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="bestBrain"></param>
        public StatisticsForGraphs(Brain bestBrain)
        {
            Score = bestBrain.RealScore; // we always know this

            if (bestBrain.AIPlayer is null) // not compulsory, and has no stats
            {
                return;
            }

            // stats about this player put into this object (used for graphs)
            Level = bestBrain.AIPlayer.gameController.Level;
            InvadersKilled = bestBrain.AIPlayer.gameController.NumberOfInvadersKilled;
            SaucersKilled = bestBrain.AIPlayer.gameController.NumberOfSaucersKilled;
            Shots = bestBrain.AIPlayer.gameController.Shots;
            KillsAvoided = bestBrain.AIPlayer.gameController.KillsAvoided;
            ShieldsShot = bestBrain.AIPlayer.gameController.NumberOfTimesShieldsWereShotByPlayer;
            FitnessScore = bestBrain.Score; // .Fitness is pre-adjustment for genome size
            Lives = bestBrain.AIPlayer.gameController.Lives;
            GameFrames = bestBrain.AIPlayer.gameController.FramesPlayed;

            // if it didn't kill anything or destroy the shields then I guess it missed...
            Missed = Shots - (InvadersKilled + SaucersKilled);

            // this is impossible, but it happened once during testing - because I zeroed player shots at start of level
            // it meant that we compared a level total with overall game total. 
            Debug.Assert(Missed >= 0);
        }
    }
}