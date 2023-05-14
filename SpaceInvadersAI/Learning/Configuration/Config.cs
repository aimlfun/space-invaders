using SpaceInvadersAI.AI;
using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.Learning.Fitness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpaceInvadersAI.Learning.Configuration;

/// <summary>
/// Configuration.
/// </summary>
internal class PersistentConfig
{
    //   ███     ███    █   █   █████    ███     ████   █   █   ████      █     █████    ███     ███    █   █
    //  █   █   █   █   █   █   █         █     █       █   █   █   █    █ █      █       █     █   █   █   █
    //  █       █   █   ██  █   █         █     █       █   █   █   █   █   █     █       █     █   █   ██  █
    //  █       █   █   █ █ █   ████      █     █       █   █   ████    █   █     █       █     █   █   █ █ █
    //  █       █   █   █  ██   █         █     █  ██   █   █   █ █     █████     █       █     █   █   █  ██
    //  █   █   █   █   █   █   █         █     █   █   █   █   █  █    █   █     █       █     █   █   █   █
    //   ███     ███    █   █   █        ███     ████    ███    █   █   █   █     █      ███     ███    █   █                                                                                       

    internal static PersistentConfig Settings = new();

    /// <summary>
    /// "learning" mode is when all the AIs are competing, and evolving.
    /// "playing" mode is when brains have been selected to play the game. This mode only makes sense
    /// when you've got one or more brains used that can beat levels.
    /// </summary>
    public enum FrameworkMode { learning, playing };

    /// <summary>
    /// Determines whether it is single AI player trying to beat the game, or multiple AIs competing to learn the game.
    /// </summary>
    public FrameworkMode Mode { get; set; } = FrameworkMode.learning;

    /// <summary>
    /// Determines which brain plays at each level. It could be one brain, or 10. It does not need to be any more, as
    /// games wrap around at level 10.
    /// </summary>
    public Dictionary<int, RuleLevelBrain /*comma / hyphen delimited levels => Brain */> BrainTemplates { get; set; } = new();

    /// <summary>
    /// Two AI approaches, 
    /// true => choice of 7 actions (left/right/fire combinations)
    /// false => pick location to place ship, and fire.
    /// </summary>
    public bool UseActionFireApproach { get; set; } = false;

    #region Configurable Settings in the UI, that are persisted to disk
    /// <summary>
    /// Layers for the perceptron network. 
    /// </summary>
    public int[] AIHiddenLayers { get; set; } = { 0, 0, 0 };

    /// <summary>
    /// This is the number of frames of the game before the first mutation is run.
    /// </summary>
    public int MovesBeforeMutation { get; set; } = 300;

    /// <summary>
    /// This decides which algorithm is used to choose offspring.
    /// </summary>
    public LearningFramework.SelectionType SelectionType { get; set; } = LearningFramework.SelectionType.FITNESS_PROPORTIONATE;

    /// <summary>
    /// Flag indicates whether we are creating a random network or a standard perceptron network.
    /// </summary>
    public bool CreatingARandomNetwork { get; set; } = false;

    /// <summary>
    /// How many AI's are playing games at once.
    /// </summary>
    public int ConcurrentGames { get; set; } = 100;

    /// <summary>
    /// This is the number of desired neurons in the network.  The actual number will be kept between the minimum and maximum.
    /// </summary>
    public int DesiredRandomNeurons { get; set; } = 5;

    /// <summary>
    /// Minimum number of neurons in the network.  The desired can shrink the number, this ensures it has a bottom limit.
    /// </summary>
    public int MinimumNumberOfNeurons { get; set; } = 3;

    /// <summary>
    /// Maximum number of neurons in the network. It is often ok to let it grow indefinitely as it will be dropped if the extra neurons did not benefit the AI.
    /// </summary>
    public int MaximumNumberOfNeurons { get; set; } = 20;

    /// <summary>
    /// When mutation occurs a certain percentage of brains are created as new random brains. This is the percentage.
    /// </summary>
    public int PercentOfBrainsToCreateAsNewRandomDuringMutation { get; set; } = 25;

    /// <summary>
    /// This is the percentage of brains that are created from a template. The template can be used to ensure it starts from a specific point,
    /// </summary>
    public int PercentOfBrainsToCreateFromTemplate { get; set; } = 0;

    /// <summary>
    /// When mutation occurs a certain percentage of "elite" brains are preserved. This is the percentage.
    /// </summary>
    public int PercentOfBrainsPreservedDuringMutation { get; set; } = 5;

    /// <summary>
    /// When a brain is mutated, it could be once, or more. This decides.
    /// </summary>
    public int NumberOfTimesASingleBrainIsMutatedInOneGeneration { get; set; } = 1;

    /// <summary>
    /// When a brain is identified for mutation, there is a chance it will/will not get mutated.
    /// </summary>
    public int PercentChanceABrainIsPickedForMutation { get; set; } = 50;

    /// <summary>
    /// All living players must have scored at least this percentage of the dead players score to continue.
    /// If they have not, they are killed off prematurely.
    /// </summary>
    public float PercentageOfDeadScoreThreshold { get; set; } = 0.9f;

    /// <summary>
    /// By default there should be shields, but the AI doesn't need them. If anything they are a hindrance.
    /// You can turn shields off here.
    /// </summary>
    public bool AIPlaysWithShields { get; set; } = true;

    /// <summary>
    /// Ideally we want the AI to ace all the levels and get an infinite score. But a cheaper quicker approach is
    /// train it on the 10 levels. This takes very little time. It's then a case of you can chain all 10 together
    /// to make an AI that can get an infinite score.
    /// This defines whether to only train on one level.
    /// </summary>
    public bool AIOneLevelOnly { get; set; } = true;

    /// <summary>
    /// Ideally we want the AI to ace all the levels and get an infinite score. But a cheaper quicker approach is
    /// train it on the 10 levels. This takes very little time. It's then a case of you can chain all 10 together
    /// to make an AI that can get an infinite score.
    /// This defines which level to start on.
    /// </summary>
    public int AIStartLevel { get; set; } = 9;

    /// <summary>
    /// When training the AI on one level, this is the score the game needs to start with (the amount the previous one scored).
    /// </summary>
    public int AIStartScore { get; set; } = 0;

    /// <summary>
    /// This contains what activation functions are allowed when creating brains or mutation.
    /// </summary>
    public ActivationFunction[] AllowedActivationFunctions { get; set; } = { ActivationFunction.TanH };

    /// <summary>
    /// This contains what mutation methods are allowed when creating brains or mutation.
    /// </summary>
    public MutationMethod[] AllowedMutationMethods { get; set; } = new MutationMethod[] { MutationMethod.AddCell, MutationMethod.AddConnection,
                                                                                          MutationMethod.RemoveCell, MutationMethod.RemoveConnection,
                                                                                          MutationMethod.ModifyBias, MutationMethod.ModifyWeight};

    /// <summary>
    /// This contains the ratio of cell types when creating a new brain. It does not affect mutation.
    /// </summary>
    public Dictionary<CellType, int> CellTypeRatios { get; set; } = new();

    /// <summary>
    /// Contains the template to use when creating a new brain.
    /// </summary>
    public string? Template { get; set; } = null;

    /// <summary>
    /// If false, it uses the screen. If true, it uses the internal data.
    /// </summary>
    public bool AIAccessInternalData { get; set; } = true;
    #endregion

    /// <summary>
    /// Ensures these are defaulted.
    /// </summary>
    public PersistentConfig()
    {
        CellTypeRatios.Clear();
        CellTypeRatios.Add(CellType.PERCEPTRON, 100);
        CellTypeRatios.Add(CellType.AND, 3);
        CellTypeRatios.Add(CellType.TRANSISTOR, 3);
        CellTypeRatios.Add(CellType.IF, 3);
        CellTypeRatios.Add(CellType.MAX, 3);
        CellTypeRatios.Add(CellType.MIN, 3);
    }

    /// <summary>
    /// Serialises the settings to JSON for saving to disk.
    /// </summary>
    /// <returns></returns>
    private static string Serialise()
    {
        // create "Settings" object as JSON and return it
        return JsonSerializer.Serialize(Settings);
    }

    /// <summary>
    /// De-serialises the settings from JSON for loading from disk.
    /// </summary>
    /// <param name="JSONAsString"></param>
    private static void Deserialise(string JSONAsString)
    {
        // create "Settings" object from JSON and return it
        PersistentConfig? settings = JsonSerializer.Deserialize<PersistentConfig>(JSONAsString);

        // de-serialise can result in a null object, so only assign if it's not null
        if (settings is not null) Settings = settings;
    }

    /// <summary>
    /// Loads the settings from a file.
    /// </summary>
    /// <param name="filename"></param>
    internal static void Load(string filename)
    {
        if (!File.Exists(filename)) return;

        Deserialise(File.ReadAllText(filename));
    }

    /// <summary>
    /// Saves the settings to a file.
    /// </summary>
    /// <param name="filename"></param>
    internal static void Save(string filename)
    {
        File.WriteAllText(filename, Serialise());
    }
}