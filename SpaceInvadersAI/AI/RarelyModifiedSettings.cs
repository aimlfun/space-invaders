using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Devices;

namespace SpaceInvadersAI.AI;

/// <summary>
/// Settings that impact the AI.
/// </summary>
internal static class RarelyModifiedSettings
{
    //   ███    █████   █████   █████    ███    █   █    ████    ███
    //  █   █   █         █       █       █     █   █   █       █   █
    //  █       █         █       █       █     ██  █   █       █
    //   ███    ████      █       █       █     █ █ █   █        ███
    //      █   █         █       █       █     █  ██   █  ██       █
    //  █   █   █         █       █       █     █   █   █   █   █   █
    //   ███    █████     █       █      ███    █   █    ████    ███

    // These are something you probably won't want to change often, but it's here if you want to.

    /// <summary>
    /// It is the minimum delta applied to a bias when mutating a cell.
    /// </summary>
    internal const float MutationCellBiasMinDelta = 0.05f;

    /// <summary>
    /// It is the maximum delta applied to a bias when mutating a cell.
    /// </summary>
    internal const float MutationCellBiasMaxDelta = 0.5f;

    /// <summary>
    /// It is the minimum delta applied to a weight when mutating a cell.
    /// </summary>
    internal const float MutationCellWeightMinDelta = 0.05f;
    
    /// <summary>
    /// It is the maximum delta applied to a weight when mutating a cell.
    /// </summary>
    internal const float MutationCellWeightMaxDelta = 0.5f;

    /// <summary>
    /// LeakyReLU et al, alpha.
    /// </summary>
    internal const double Alpha = 0.01f;

    /// <summary>
    /// True to biological brains, I thought we'd introduce the "threshold" concept.
    /// If activation does not exceed this in magnitude (Math.Abs()), the output is 0.
    /// Our default is 0.65.
    /// This enables on > 0.5, off < 0.5 like behaviour (if set to 0.5).
    /// This enables clipping behaviour, e.g. set to 0 anything negative is discarded.
    /// The concept is simple: Sometimes we need an exact answer. Data-scientists may live in a probability world, which is
    /// how it works, but at some point it's a "yes" or "no" in a classification process. For example vowels, returns 1 for vowel, not 0.87.
    /// </summary>
    internal const double DefaultCellActivationThreshold = 0.65f;

    /// <summary>
    /// Genomes have the potential to grow very large. Large might not be bad, but larger are slower and the increase in neurons might not
    /// be worth the increase in processing time. This is the threshold after which it will penalise if the increase is not proportionally
    /// an improvement in score. 
    /// This is multiplied by the number of inputs. 
    /// So if you have 10 inputs, and this is 1.5, then the genome size must be 15 or more before.
    /// </summary>
    internal const float MinimumGenomeSizeNotToBePunishedForGrowthDisproportionateToScoreImprovement = 1.5f;
}