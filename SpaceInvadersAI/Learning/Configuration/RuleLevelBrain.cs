using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.Learning.Configuration
{
    /// <summary>
    /// Provides the settings to enable us to use the AI on a specific level.
    /// </summary>
    public class RuleLevelBrain
    {
        //  ████    █   █   █       █████           █       █████   █   █   █████   █               ████    ████      █      ███    █   █
        //  █   █   █   █   █       █               █       █       █   █   █       █               █   █   █   █    █ █      █     █   █
        //  █   █   █   █   █       █               █       █       █   █   █       █               █   █   █   █   █   █     █     ██  █
        //  ████    █   █   █       ████    █████   █       ████    █   █   ████    █       █████   ████    ████    █   █     █     █ █ █
        //  █ █     █   █   █       █               █       █       █   █   █       █               █   █   █ █     █████     █     █  ██
        //  █  █    █   █   █       █               █       █        █ █    █       █               █   █   █  █    █   █     █     █   █
        //  █   █    ███    █████   █████           █████   █████     █     █████   █████           ████    █   █   █   █    ███    █   █

        /// <summary>
        /// For a brain to run on a level, it needs to know the score it was trained with.
        /// Background: Space Invaders fire faster as the score increases. So if you train a level 4 starting on score 0,
        /// it will not be able to play level 4 when the score is 3*990. That's because the AI learns the pattern and that
        /// pattern includes the bombs/bullets being dropped. As they will be more rapid that what it trained on it will
        /// appear the brain hasn't saved properly
        /// </summary>
        public int StartingScore { get; set; } = 0;

        /// <summary>
        /// 1-3 or 1,2,3 or simply 5.
        /// </summary>
        public string LevelRule { get; set; } = "";

        /// <summary>
        /// This is a filename of a brain template.
        /// </summary>
        public string BrainTemplateFileName { get; set; } = "";
    }
}
