using SpaceInvadersAI.Learning.AIPlayerAndController;
using SpaceInvadersAI.Learning.Configuration;
using SpaceInvadersAI.AI;
using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.AI.ExternalInterface;
using SpaceInvadersAI.AI.Visualisation;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using SpaceInvadersAI.Graphing;

namespace SpaceInvadersAI.Learning;
/// <summary>
/// Framework to play games, and hopefully learns to play them better.
/// </summary>
internal class LearningFramework
{
    //  █       █████     █     ████    █   █    ███    █   █    ████           █████   ████      █     █   █   █████   █   █    ███    ████    █   █
    //  █       █        █ █    █   █   █   █     █     █   █   █               █       █   █    █ █    ██ ██   █       █   █   █   █   █   █   █  █
    //  █       █       █   █   █   █   ██  █     █     ██  █   █               █       █   █   █   █   █ █ █   █       █   █   █   █   █   █   █ █
    //  █       ████    █   █   ████    █ █ █     █     █ █ █   █               ████    ████    █   █   █ █ █   ████    █ █ █   █   █   ████    ██
    //  █       █       █████   █ █     █  ██     █     █  ██   █  ██           █       █ █     █████   █   █   █       █ █ █   █   █   █ █     █ █
    //  █       █       █   █   █  █    █   █     █     █   █   █   █           █       █  █    █   █   █   █   █       ██ ██   █   █   █  █    █  █
    //  █████   █████   █   █   █   █   █   █    ███    █   █    ████           █       █   █   █   █   █   █   █████   █   █    ███    █   █   █   █

    /// <summary>
    /// This tracks the data used for the graphs. The best brain at the end of a generation is added to this list.
    /// </summary>
    internal readonly List<StatisticsForGraphs> performanceOfBestPlayerPerGeneration = new();

    /// <summary>
    /// Contains the details of the best player.
    /// </summary>
    internal string bestBrainInfo = "";

    #region DELEGATES
    /// <summary>
    /// Delegate for "ResetGame" event.
    /// </summary>
    internal delegate void NotifyResetGame();

    /// <summary>
    /// Delegate for "StartGame" event.
    /// </summary>
    /// <param name="generation"></param>
    internal delegate void NotifyStartGame(int generation);

    /// <summary>
    /// Delegate for a function that returns the fitness of player #id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    internal delegate float FitnessFunction(int id);

    /// <summary>
    /// Delegate for a function that creates a player.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="brain"></param>
    internal delegate void PlayerCreationDelegate(int id, Brain brain);
    #endregion

    #region CONSTANTS
    /// <summary>
    /// This is the maximum allow number of cells in a brain. We don't have to limit this, but it's a good idea.
    /// </summary>
    private const int c_maximumAllowedCellsInBrain = 10000; // 56x64= 3584 cells, that's INPUT so 10000 is a good limit.

    /// <summary>
    /// This is the maximum allow number of connections in a brain. We don't have to limit this, but it's a good idea.
    /// Too many connections and the brain will take too long to process.
    /// </summary>
    private const int c_maximumAllowedConnectionsInBrain = 100000; // every cell must be connected at l

    /// <summary>
    /// This is the likelihood 0..1 (0%..100%) that a brain will be picked from the selection list.
    /// </summary>
    private const float c_tournamentSelectionProbability = 0.5f; // 50% chance of being picked.

    /// <summary>
    /// This is the size of the tournament selection list. It picks this many brains at random and sorts in order of best to worst.
    /// Then using chance it picks one.
    /// </summary>
    private const int c_tournamentSelectionSize = 5;

    /// <summary>
    /// Determines how big the selection list is for the power selection.
    /// The 0..1 (indicating 0..population size) is raised to this power, shrinking the size of the pool of best brains to pick from.
    /// </summary>
    private const int c_selectionPower = 4;
    #endregion

    #region PRIVATE PROPERTIES
    /// <summary>
    /// This is the best brain so far.
    /// </summary>
    private Brain? bestBrain;

    /// <summary>
    /// Stores the best true score (not the one based on the fitness function).
    /// </summary>
    private int bestEverRealScore = 0;

    /// <summary>
    /// Specifies how many brains we want to create.
    /// </summary>
    private readonly int DesiredBrainPopulationSize;

    /// <summary>
    /// The "elite" (successful/max fitness) the rows should be preserved defined as a %age of total population.
    /// </summary>
    private readonly int PercentageOfElitesToPreserve;

    /// <summary>
    /// Specifies the %age of the populate that are created from the template.
    /// </summary>
    private readonly int PercentageCreatedFromTemplate;

    /// <summary>
    /// If a random number (0-100) is LESS than this we will mutate the neuron.
    /// </summary>
    private readonly int PercentageChanceOfMutation;

    /// <summary>
    /// If randomly picked to mutate this controls how many changes we do.
    /// </summary>
    private readonly int HowManyTimesToMutateANeuron;

    /// <summary>
    /// How many neurons the brain should be created with.
    /// </summary>
    private readonly int DesiredBrainSizeInNeurons;

    /// <summary>
    /// This stores a list of named parameters that the brain will use as input.
    /// </summary>
    private readonly string[] brainInputParameters;

    /// <summary>
    /// This stores a list of named parameters that the brain will use as output.
    /// </summary>
    private readonly string[] brainOutputParameters;

    /// <summary>
    /// This stores the mutation methods that the caller has dictate the brains are allowed to use.
    /// </summary>
    private readonly MutationMethod[] MutationMethods;

    /// <summary>
    /// Caller can provide a brain template on which a number of new brains will be created.
    /// </summary>
    private readonly string? TemplateOfBrain = null;

    /// <summary>
    /// This allows the caller to hookup a logging function.
    /// </summary>
    internal Action<string> LogWriter;

    /// <summary>
    /// The algorithm used to select the brains for the next generation.
    /// </summary>
    private readonly SelectionType Selection;

    /// <summary>
    /// The list of allowed cell types that can be created during mutation.
    /// </summary>
    private readonly Dictionary<CellType, int> cellTypesAllowedForMutation = new();

    /// <summary>
    /// The list of allowed activation functions that can be used during cell creation.
    /// </summary>
    private readonly ActivationFunction[] AllowedActivationFunctions;

    /// <summary>
    /// Use a random network instead of a perceptron network.
    /// </summary>
    private readonly bool UseRandomNetwork;

    /// <summary>
    /// The definition for a perceptron network.
    /// </summary>
    private readonly int[]? NeuronLayerDefinition;

    /// <summary>
    /// The number of brains that are created randomly during mutation, represented as a %age of the total population.
    /// </summary>
    private readonly int PercentRandomToCreateDuringMutation;

    /// <summary>
    /// Tracks the best fitness for these brains.
    /// </summary>
    private float bestEverFitnessAchievedByABrain = -1;

    /// <summary>
    /// By default it logs to the console.
    /// </summary>
    /// <param name="text"></param>
    private void LogStub(string text)
    {
        Debug.WriteLine(text);
    }
    #endregion

    #region INTERNAL PROPERTIES
    /// <summary>
    /// There are a number of ways to select the brains for the next generation. This is the options we have so far.
    /// </summary>
    internal enum SelectionType { FITNESS_PROPORTIONATE, POWER, TOURNAMENT }

    /// <summary>
    /// Tracks the brains being trained.
    /// </summary>
    internal Dictionary<int, Brain> BrainsBeingTrained = new();

    /// <summary>
    /// The "generation" or "epoch" we are currently on.
    /// </summary>
    internal int Generation = 0;

    /// <summary>
    /// Default list of cell types.
    /// </summary>
    internal static readonly CellType[] DefaultCellTypes = new CellType[] {
                    CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON,
                    CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON,
                    CellType.TRANSISTOR, CellType.TRANSISTOR,
                    CellType.AND, CellType.AND,
                    CellType.MAX, CellType.MIN,
                    CellType.IF, CellType.IF };

    /// <summary>
    /// Default list of mutation methods.
    /// </summary>
    internal static readonly MutationMethod[] DefaultMutationMethods = new MutationMethod[] {
                                                                             MutationMethod.ModifyActivationFunction, MutationMethod.ModifyBias,
                                                                             MutationMethod.ModifyWeight, MutationMethod.ModifyCellType,
                                                                             MutationMethod.AddConnection, MutationMethod.RemoveConnection,
                                                                             MutationMethod.AddCell, MutationMethod.RemoveCell, MutationMethod.RemoveConnection,
                                                                             MutationMethod.AddSelfConnection, MutationMethod.RemoveSelfConnection};


    /// <summary>
    /// Used by the graph to know how much we've reduced the data points by to keep them in range.
    /// </summary>
    internal static float GenerationMultiplier = 1;

    /// <summary>
    /// Used to set the logging function.
    /// </summary>
    /// <param name="writeToLog"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void SetUILog(Action<string> writeToLog)
    {
        LogWriter = writeToLog;
    }
    #endregion

    /// <summary>
    /// This framework is about games, and games require players (albeit AI players). This is the delegate that will be called to create a player.
    /// </summary>
    private readonly PlayerCreationDelegate PlayerCreationFunction;

    /// <summary>
    /// For this to be useful, the controller attaches the logic to initialise a game.
    /// </summary>
    internal event NotifyStartGame? StartGame;

    /// <summary>
    /// For this to be useful, the controller attaches the logic to reset the game for the next round (generation).
    /// </summary>
    internal event NotifyResetGame? ResetGame;

    /// <summary>
    /// Constructor. Creates the framework for the game.
    /// TODO: It would be better in my view if the caller passed in an "object" containing the learning framework inputs (it validated), than do it as part of this constructor.
    /// </summary>
    /// <param name="inputParameterNames">The list of named inputs for the brain.</param>
    /// <param name="outputParameterNames">The list of named outputs for the brain.</param>
    /// <param name="playerCreation">Method to call to create players.</param>
    /// <param name="template">The template the brain should use.</param>
    /// <param name="createRandomNetwork">Indicates whether it's a random network or perceptron network.</param>
    /// <param name="suppliedLayers">If not random, this contains the layers definition.</param>
    /// <param name="population">How many players aka "population".</param>
    /// <param name="topBrainsToPreservePercentage">Defines the %age of elite brains are preserved.</param>
    /// <param name="randomBrainsToCreatePercentage">Defines the %age of players created using random brains.</param>
    /// <param name="brainsCreatedFromTemplatePercentage">Defines the %age of players created from a template.</param>
    /// <param name="numberOfNeurons">The number of neurons, if a random brain.</param>
    /// <param name="cellTypeRatios">Defines the ratio of different cell types.</param>
    /// <param name="mutationMethods">Defines the allows mutation methods.</param>
    /// <param name="chanceOfMutationPercentage">Defines the chance of mutation 1-100%.</param>
    /// <param name="selectionType">Defines the method used to choose offspring.</param>
    /// <param name="mutationAmount">Defines the number of times each chosen brain gets mutated in a generation.</param>
    /// <exception cref="ArgumentNullException"></exception>
    internal LearningFramework(
                        string[] inputParameterNames,
                        string[] outputParameterNames,
                        PlayerCreationDelegate playerCreation,
                        string? template = null,
                        bool createRandomNetwork = true,
                        int[]? suppliedLayers = null,
                        int population = 50,
                        int topBrainsToPreservePercentage = 50,
                        int randomBrainsToCreatePercentage = 10,
                        int brainsCreatedFromTemplatePercentage = 5,
                        int numberOfNeurons = 20,
                        Dictionary<CellType, int>? cellTypeRatios = null,
                        MutationMethod[]? mutationMethods = null,
                        ActivationFunction[]? allowedFunctions = null,
                        int chanceOfMutationPercentage = 1,
                        SelectionType selectionType = SelectionType.FITNESS_PROPORTIONATE,
                        int mutationAmount = 1)
    {
        LogWriter = LogStub;

        // necessary validation to avoid checking as the settings are used.

        if (playerCreation is null) throw new ArgumentNullException(nameof(playerCreation), "no mechanism to create players provided.");
        if (topBrainsToPreservePercentage < 1 || topBrainsToPreservePercentage > 100) throw new ArgumentNullException(nameof(topBrainsToPreservePercentage), "Must be between 1 and 100%.");
        if (randomBrainsToCreatePercentage < 1 || randomBrainsToCreatePercentage > 100) throw new ArgumentNullException(nameof(randomBrainsToCreatePercentage), "Must be between 1 and 100%.");
        if (brainsCreatedFromTemplatePercentage < 0 || brainsCreatedFromTemplatePercentage > 100) throw new ArgumentNullException(nameof(brainsCreatedFromTemplatePercentage), "Must be between 0 and 100%."); // 0 is allowed
        if (chanceOfMutationPercentage < 1 || chanceOfMutationPercentage > 100) throw new ArgumentNullException(nameof(chanceOfMutationPercentage), "Must be between 1 and 100%.");
        if (mutationAmount < 1) throw new ArgumentNullException(nameof(mutationAmount), "1..n, number of times to mutate must be 1 or more.");

        // apply defaults

        cellTypeRatios ??= new()
            {
                { CellType.PERCEPTRON, 1 }
            };

        mutationMethods ??= DefaultMutationMethods;

        brainInputParameters = inputParameterNames;
        brainOutputParameters = outputParameterNames;
        PercentageChanceOfMutation = chanceOfMutationPercentage;
        HowManyTimesToMutateANeuron = mutationAmount;
        Generation = 1;
        UseRandomNetwork = createRandomNetwork;
        NeuronLayerDefinition = suppliedLayers;
        PercentRandomToCreateDuringMutation = randomBrainsToCreatePercentage;
        AllowedActivationFunctions = allowedFunctions is null ? new ActivationFunction[] { ActivationFunction.Sigmoid, ActivationFunction.TanH, ActivationFunction.ReLU, ActivationFunction.LeakyReLU,
                                                                                           ActivationFunction.SeLU, ActivationFunction.PReLU, ActivationFunction.Logistic, ActivationFunction.Identity,
                                                                                           ActivationFunction.Step, ActivationFunction.SoftSign, ActivationFunction.SoftPlus, ActivationFunction.Gaussian,
                                                                                           ActivationFunction.BENTIdentity, ActivationFunction.Bipolar, ActivationFunction.BipolarSigmoid, ActivationFunction.HardTanH,
                                                                                           ActivationFunction.Absolute, ActivationFunction.Not } : allowedFunctions;

        if (NeuronLayerDefinition is not null)
        {
            // default the neuron layers to the same as the input if user puts "0".
            for (int i = 0; i < NeuronLayerDefinition.Length; i++) if (NeuronLayerDefinition[i] == 0) NeuronLayerDefinition[i] = inputParameterNames.Length; // 0=same as input
        }

        TemplateOfBrain = template;
        DesiredBrainPopulationSize = population;
        PercentageOfElitesToPreserve = topBrainsToPreservePercentage;
        PercentageCreatedFromTemplate = (template is null) ? 0 : brainsCreatedFromTemplatePercentage;
        PlayerCreationFunction = playerCreation;
        DesiredBrainSizeInNeurons = numberOfNeurons;
        MutationMethods = mutationMethods;
        cellTypesAllowedForMutation = cellTypeRatios;
        Selection = selectionType;

        LogWriter.Invoke($"population size: {DesiredBrainPopulationSize}");
        LogWriter.Invoke($"cellTypesAllowedForMutation: {string.Join("|", cellTypesAllowedForMutation.Keys)}");
        LogWriter.Invoke($"% elite to preserve: {topBrainsToPreservePercentage}");
        LogWriter.Invoke($"Mutation methods: {string.Join("|", mutationMethods)}");
        LogWriter.Invoke($"% chance of mutation: {PercentageChanceOfMutation}");
        LogWriter.Invoke($"# mutations per neuron: {HowManyTimesToMutateANeuron}");
        LogWriter.Invoke($"SelectionType: {Selection}");

        InitialiseBrains();
    }

    /// <summary>
    /// Learn for this generation. It starts a game.
    /// </summary>
    internal void StartLearning()
    {
        ResetGame?.Invoke();

        // create one player per brain
        foreach (int id in BrainsBeingTrained.Keys)
        {
            PlayerCreationFunction(id, BrainsBeingTrained[id]);
        }

        StartGame?.Invoke(Generation);
    }

    /// <summary>
    /// Saves the best brain as a template (upon request from the UI).
    /// </summary>
    internal void SaveBestBrainAsTemplate()
    {
        if (LearningController.s_learningFramework is null || bestBrain is null) return;

        StatisticsForGraphs bestPlayerAI = LearningController.s_learningFramework.performanceOfBestPlayerPerGeneration[^1];

        // get unique filename in .\Templates\
        File.WriteAllText($@".\Templates\{PersistentConfig.Settings.InputToAI} {(PersistentConfig.Settings.UseActionFireApproach ? "action" : "position")} s={bestPlayerAI.Score} lvl={bestPlayerAI.Level} ik={bestPlayerAI.InvadersKilled} sk={bestPlayerAI.SaucersKilled}.tem", bestBrain.GetAsTemplate());
    }

    /// <summary>
    /// End the evaluation of the current generation.
    /// We score each AI, and rank them.
    /// Then we work out the next generation based on configuration on how many to preserve, how many to create randomly, and how many to create from the template.
    /// </summary>
    internal void EndLearning()
    {
        const bool DrawCohortsDuringVisualisation = false;

        CalculateScoringOfAIPlayers();

        // draw a PNG of the best brain
        if (Visualiser.s_visualisationsEnabled)
        {
            new Visualiser(BrainsBeingTrained.Values.ElementAt(0)).RenderAndSaveDiagramToPNG($@"c:\temp\Best brain - Generation {Generation}.png");
            MessageBox.Show(@$"Visualisation saved to c:\temp\Best brain - Generation {Generation}.png");
        }

        // make our new population of brains
        CreateNextGenerationOfBrains(out List<Brain> newBrainsList);

        // explicitly dispose of brains that are not in the new generation
        RemoveLastGenerationBrainsThatAreNotPresentInThisGeneration(newBrainsList);

        // We now have a new population of brains, so we can clear the previous list of brains being trained and add the new population
        BrainsBeingTrained.Clear();

        int cohortIndexNumber = 0;

        foreach (Brain thisBrain in newBrainsList)
        {
            BrainsBeingTrained.Add(thisBrain.Id, thisBrain);

            if (DrawCohortsDuringVisualisation && Visualiser.s_visualisationsEnabled)
            {
                new Visualiser(thisBrain).RenderAndSaveDiagramToPNG($@"c:\temp\Cohort - Generation {Generation} #{cohortIndexNumber++} - {thisBrain.Name}.png");
                File.WriteAllText(Path.Combine($@"d:\temp\cohort g{Generation} {thisBrain.Id}-{thisBrain.Name}.txt"), thisBrain.GetAsTemplate());
            }
        }

        LogWriter.Invoke($"New Generation: {Generation}, READY TO LEARN");

        Visualiser.s_visualisationsEnabled = false;
        StartLearning();
    }

    /// <summary>
    /// Compare this generation's brains to the previous generation's brains, and dispose of those that are no longer required.
    /// </summary>
    /// <param name="newBrainsList"></param>
    private void RemoveLastGenerationBrainsThatAreNotPresentInThisGeneration(List<Brain> newBrainsList)
    {
        // replace the old population with the new population, means those we didn't want are disposed of.
        foreach (Brain b in BrainsBeingTrained.Values)
        {
            // what matters is that the brain is disposed of, not that it is removed from the list, because it has quite a few resources.
            if (!newBrainsList.Contains(b))
            {
                b.Dispose();
            }
        }
    }

    /// <summary>
    /// We need to construct the next population from some of the current population (elite) plus offspring, plus random.
    /// </summary>
    /// <param name="newBrainsList"></param>
    private void CreateNextGenerationOfBrains(out List<Brain> newBrainsList)
    {
        newBrainsList = new();
        Brain[] brainArray = BrainsBeingTrained.Values.ToArray();

        // get the best brains
        List<Brain> elitists = CreateEliteBrainsThenAddToNextGenerationPopulation(brainArray);

        int pctToTemplate = CreateTemplatedBrainsThenAddToNextGenerationPopulation(newBrainsList, brainArray);

        int randomPct = (int)((float)PercentRandomToCreateDuringMutation / 100f * brainArray.Length);

        // determine how many random brains we need to create to make up the numbers
        if (elitists.Count + pctToTemplate + randomPct > DesiredBrainPopulationSize)
        {
            randomPct = DesiredBrainPopulationSize - elitists.Count - (int)pctToTemplate;
        }

        CreateOffSpringFromParentsAndMutateThemALittleThenAddToNextGenerationPopulation(newBrainsList, DesiredBrainPopulationSize - elitists.Count - pctToTemplate - randomPct);

        CreateRandomBrainsAndAddToNextGenerationPopulation(newBrainsList, randomPct);

        LogWriter.Invoke($"Best score: [{bestEverRealScore,6}]");
        LogWriter.Invoke($"Generation: {Generation} - COMPLETE");
        LogWriter.Invoke($"===================================================");

        Generation++;

        // We add these back in the elites after (avoids mutation of perfectly good brains).
        // insert at the start of the list, so they are first on screen.
        foreach (Brain b in elitists) newBrainsList.Insert(0, b);

        if (newBrainsList.Count != PersistentConfig.Settings.ConcurrentGames) Debugger.Break();
    }

    /// <summary>
    /// Using the algorithm configured, generate offspring.
    /// Some offspring might work fine as they are, but sometimes mutation improves them further.
    /// </summary>
    /// <param name="newBrainsList"></param>
    /// <param name="numberOfOffspring"></param>
    /// 
    private void CreateOffSpringFromParentsAndMutateThemALittleThenAddToNextGenerationPopulation(List<Brain> newBrainsList, int numberOfOffspring)
    {
        InitialiseBrainSelection(out Brain[] population, out float totalFitness, out float minimalFitness);

        // This is where we breed the next individuals.
        // The new ones are picked from select groups to breed (cross over of neurons/connections etc), and that
        // includes the best ones.

        for (int i = 0; i < numberOfOffspring; i++)
        {
            Brain newBrain = GetOffspring(population, totalFitness, minimalFitness);

            newBrain.Provenance = "Offspring";
            newBrainsList.Add(newBrain);
        }

        // We mutate the brains we have so far (excludes the best performing), in the hope to find better ones.

        // The scope includes offspring (cross-over). The thing we ought to prove is whether they should indeed 
        // all be mutated, or left alone. i.e. you cross-over the 2 best, and use that vs. you cross-over the 2 best, and mutate the result. 

        Mutate(newBrainsList);
    }

    /// <summary>
    /// Add some random brains to the mix. They might prove to be better than the offspring.
    /// Without randomness, the population can get stuck in a local maximum.
    /// </summary>
    /// <param name="newBrainsList"></param>
    /// <param name="randomPct"></param>
    private void CreateRandomBrainsAndAddToNextGenerationPopulation(List<Brain> newBrainsList, int randomPct)
    {
        // store the top % of brains
        for (int i = 0; i < randomPct; i++)
        {
            Brain newBrain = CreateNewBrain(); // this is always random (neuron type bias, weights etc), unless parameters are used.

            newBrain.Provenance = "Random";
            newBrainsList.Add(newBrain);
            LogWriter.Invoke($"Random Brain \"{newBrain.Name}\" added");
        }
    }

    /// <summary>
    /// Stores the templated brains (if any) in the new population.
    /// </summary>
    /// <param name="newBrainsList"></param>
    /// <param name="brainArray"></param>
    /// <returns></returns>
    private int CreateTemplatedBrainsThenAddToNextGenerationPopulation(List<Brain> newBrainsList, Brain[] brainArray)
    {
        int pctToTemplate = 0;

        // some could be seeded via a template (a prior saved brain)
        if (TemplateOfBrain is null) return pctToTemplate;

        pctToTemplate = (int)((float)PercentageCreatedFromTemplate / 100f * brainArray.Length);

        for (int i = 0; i < pctToTemplate; i++)
        {
            Brain newBrain = Brain.CreateFromTemplate(TemplateOfBrain);
            newBrain.Provenance = "Templated";
            newBrain.lineage = "(templated) ";
            newBrainsList.Add(newBrain);

            LogWriter.Invoke($"Brain \"{newBrain.Name}\" is templated");
        }

        return pctToTemplate;
    }

    /// <summary>
    /// These are stored as is, and not mutated. That means they are a base for offspring in future generations.
    /// </summary>
    /// <param name="brainArray"></param>
    /// <returns></returns>
    private List<Brain> CreateEliteBrainsThenAddToNextGenerationPopulation(Brain[] brainArray)
    {
        List<Brain> elitists = new(); // store the top % of brains

        int pctToPreserve = (int)((float)PercentageOfElitesToPreserve * (float)brainArray.Length / 100); // how many to preserve
        int eliteAddedCount = 0; // how many we've added so far

        // store the top % of brains
        for (int i = 0; i < brainArray.Length; i++)
        {
            if (brainArray[i].Fitness > 0)
            {
                elitists.Add(brainArray[i]);

                brainArray[i].GenerationOfLastMutation++;
                brainArray[i].LastFitness = brainArray[i].Fitness;
                brainArray[i].Provenance = "Elite";
                brainArray[i].Reset(); // we're reusing this brain, we need to set state & activations to 0.

                LogWriter.Invoke($"Brain \"{brainArray[i].Name}\" is elite");

                // we've preserved enough, so stop
                if (++eliteAddedCount >= pctToPreserve) break;
            }
        }

        return elitists;
    }

    /// <summary>
    /// Score each of them, and sort them by score, with the best one is at the top.
    /// "Score" is the thing we sorted by, not the fitness.
    /// Output the list of brains as scored to a log file.
    /// </summary>
    private void CalculateScoringOfAIPlayers()
    {
        SortNetworkByFitnessAssigningItToScore(); // largest "Score" (best performing) goes to the top

        // 500 is the rough number for the graph to look good. If you have a lot of generations,
        // it will be slow to draw and eat memory for no good reason. So we remove the oldest.
        if (performanceOfBestPlayerPerGeneration.Count > 500)
        {
            performanceOfBestPlayerPerGeneration.RemoveAt(0);
            ++GenerationMultiplier; // compensate for the missing one
        }

        // track the last 5 performance scores per brain
        foreach (Brain b in BrainsBeingTrained.Values)
        {
            b.Performance.Insert(0, (int)(b.Fitness));

            if (b.Score > bestEverFitnessAchievedByABrain) bestEverFitnessAchievedByABrain = b.Score;
            if (b.RealScore > bestEverRealScore) bestEverRealScore = b.RealScore;
            if (b.Performance.Count > 5) b.Performance.RemoveAt(5);
        }

        LogWriter.Invoke($"Generation: {Generation,6} >>> best score: [{bestEverRealScore,6}]  average fitness: [{Math.Round(GetAverage())}]  best ever fitness: [{bestEverFitnessAchievedByABrain}]"); // average requires sorting (assigns score)

        foreach (int id in BrainsBeingTrained.Keys)
        {
            LogWriter.Invoke(
                $"{BrainsBeingTrained[id].Name,10} - DNA: {BrainsBeingTrained[id].DNA} Genome size: [{BrainsBeingTrained[id].GenomeSize}] Moves to mutation: {PersistentConfig.Settings.MovesBeforeMutation} Game Score: [{BrainsBeingTrained[id].RealScore,6}]  " +
                $"High Score: [{BrainsBeingTrained[id].RealBestScore,6}]  AI Fitness: [{Math.Round(BrainsBeingTrained[id].Fitness),6}] AI Last Fitness: [{Math.Round(BrainsBeingTrained[id].LastFitness),6}]  " +
                $"AI Score (rank-by): [{Math.Round(BrainsBeingTrained[id].Score),6}] - Fitness Metrics: [{GetLast5PerformanceScoresForBrain(BrainsBeingTrained[id])}] " +
                $"Lives: {BrainsBeingTrained[id].AIPlayer.gameController.Lives}");
            LogWriter.Invoke($"      calculation: {BrainsBeingTrained[id].FitnessSummary.Replace("\n", " ")}\n");
        }

        bestBrain = BrainsBeingTrained.Values.ToArray()[0]; // top is best
        bestBrainInfo = bestBrain.PlayerSummary;

        performanceOfBestPlayerPerGeneration.Add(new StatisticsForGraphs(bestBrain));
    }

    /// <summary>
    /// Returns the last 5 performances for this brain (or less, if it hasn't lasted that long).
    /// </summary>
    /// <param name="brain"></param>
    /// <returns></returns>
    private static string GetLast5PerformanceScoresForBrain(Brain brain)
    {
        int[] perf = brain.Performance.ToArray();
        string lastResults = "";

        if (perf.Length > 0)
        {
            for (int i = perf.Length - 1; i >= Math.Max(0, perf.Length - 5); i--)
            {
                lastResults += perf[i].ToString() + ",";
            }

            // we're appending "," after each metric, so last result will have a ",", so we need to trim it here.
            lastResults = lastResults.TrimEnd(',');
        }

        return lastResults;
    }

    /// <summary>
    /// FITNESS_PROPORTIONATE selection process require us it to compute minimal and total fitness. 
    /// </summary>
    /// <param name="population"></param>
    /// <param name="totalFitness"></param>
    /// <param name="minimalFitness"></param>
    /// <exception cref="Exception"></exception>
    private void InitialiseBrainSelection(out Brain[] population, out float totalFitness, out float minimalFitness)
    {
        totalFitness = 0;
        minimalFitness = 0;

        population = BrainsBeingTrained.Values.ToArray();

        // ensure scores are sorted! (no guarantee, but an indicator).
        if (population[0].Score < population[1].Score) Debugger.Break();

        // If we're not using fitness proportionate selection, we don't need to calculate the total /MINIMAL fitness.
        if (Selection != SelectionType.FITNESS_PROPORTIONATE) return;

        for (int i = 0; i < population.Length; i++)
        {
            float score = population[i].Score;

            // minimalFitness = Min(score, minimalFitness), giving us the lowest score.
            minimalFitness = score < minimalFitness ? score : minimalFitness;
            totalFitness += score; // totalFitness = SUM( score), used to calculate the average fitness
        }

        minimalFitness = Math.Abs(minimalFitness); // ensure it's positive
        totalFitness += minimalFitness * population.Length;
    }

    /// <summary>
    /// Sorts the brains by score, so we can select the best brains for breeding.
    /// </summary>
    private void SortNetworkByFitnessAssigningItToScore()
    {
        float max = -1;

        // determine max value of ALL brains
        foreach (Brain brain in BrainsBeingTrained.Values)
        {
            max = Math.Max(brain.Fitness, max);

            if (brain.RealScore > brain.RealBestScore) brain.RealBestScore = brain.RealScore;
        }

        // if "0" was the best it could do, then ordering is unimportant
        if (max == 0) return;

        foreach (Brain n in BrainsBeingTrained.Values)
        {
            n.Score = n.Fitness;
        }

        // sort so that highest fitness is the 0 entry, and worst is at the bottom.
        BrainsBeingTrained = BrainsBeingTrained.OrderByDescending(x => x.Value.Score).ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// We calculate the average to see whether the brains are improving or not. The number should increase.
    /// Remember it isn't %age as we rarely even know the maximum fitness possible for a brain.
    /// SUM( Brain[].Fitness ) / COUNT ( Brain[] )
    /// </summary>
    /// <returns></returns>
    private float GetAverage()
    {
        return BrainsBeingTrained.Values.Average(x => x.Fitness);
    }

    /// <summary>
    /// Create the initial pool of brains from a template, or random.
    /// </summary>
    void InitialiseBrains()
    {
        BrainsBeingTrained.Clear();

        for (var i = 0; i < DesiredBrainPopulationSize; i++)
        {
            Brain newBrain = CreateNewBrain();

            BrainsBeingTrained.Add(newBrain.Id, newBrain);
        }
    }

    /// <summary>
    /// Creates a new brain. If a template is specified, it will be used. Otherwise a random brain will be created.
    /// </summary>
    /// <returns></returns>
    private Brain CreateNewBrain()
    {
        Brain newBrain;

        if (!string.IsNullOrWhiteSpace(TemplateOfBrain))
        {
            newBrain = Brain.CreateFromTemplate(TemplateOfBrain, Brain.NextUniqueBrainID.ToString());
            newBrain.lineage = "Templated";
        }
        else
        {
            newBrain = new Brain(Brain.NextUniqueBrainID.ToString(), AllowedActivationFunctions, brainInputParameters, brainOutputParameters);
            newBrain.AddNetworkWithConnectedInputOutputs("network", AllowedActivationFunctions);
            newBrain.AllowedCellTypes = cellTypesAllowedForMutation;

            // a random network 
            if (UseRandomNetwork)
            {
                Architecture.CreateRandomNetwork(newBrain.Networks["network"], brainInputParameters, brainOutputParameters, DesiredBrainSizeInNeurons); // TODO: specifying params
            }
            else
            {
                Debug.Assert(NeuronLayerDefinition is not null, "NeuronLayerDefinition is null - it should be an array defining the neuron structure");

                // or a start with perception layers
                Architecture.CreatePerceptronNetwork(newBrain.Networks["network"], brainInputParameters, NeuronLayerDefinition, brainOutputParameters);
            }

            new Visualiser(newBrain).RenderAndSaveDiagramToPNG($@"c:\temp\Initial Brain - Generation {Generation} {newBrain.Name}.png");
            newBrain.lineage = "Random";
        }

        newBrain.LastOverallGenomeSize = newBrain.OverallGenomeSize;

        return newBrain;
    }

    /// <summary>
    /// Selects a random mutation method for a genome according to the parameters
    /// </summary>
    /// <param name="brain"></param>
    private MutationMethod? SelectMutationMethod(Brain brain)
    {
        // pick one of the methods at random.
        var mutationMethod = MutationMethods[RandomNumberGenerator.GetInt32(0, MutationMethods.Length)];

        if (mutationMethod == MutationMethod.AddCell)
        {
            if (brain.GenomeSize >= c_maximumAllowedCellsInBrain)
            {
                // if (config.warnings) 
                //Debug.WriteLine("maxNodes exceeded!");
                return null;
            }
            else
            {
                // stop it exceeding the size.
                if (brain.GenomeSize >= PersistentConfig.Settings.MaximumNumberOfNeurons)
                {
                    //Debug.WriteLine("AddCell mutation blocked as it has reached the maximum number of neurons allowed in a network.");
                    return null;
                }
            }
        }

        if (mutationMethod == MutationMethod.RemoveCell)
        {
            // stop it exceeding the size.
            if (brain.GenomeSize <= PersistentConfig.Settings.MinimumNumberOfNeurons)
            {
                //Debug.WriteLine("AddCell mutation blocked as it has reached the minimum number of neurons allowed in a network.");
                return null;
            }
        }

        if (mutationMethod == MutationMethod.AddConnection && brain.ConnectionsSize >= c_maximumAllowedConnectionsInBrain)
        {
            // if (config.warnings)
            // Debug.WriteLine("maxConns exceeded!");
            return null;
        }

        return mutationMethod;
    }

    /// <summary>
    /// Mutates the given (or current) population
    /// </summary>
    /// <param name="population"></param>
    private void Mutate(List<Brain> population)
    {
        // Elitist genomes should not be included
        foreach (Brain b in population)
        {
            if (RandomNumberGenerator.GetInt32(0, 100) <= PercentageChanceOfMutation)
            {
                for (var j = 0; j < HowManyTimesToMutateANeuron; j++)
                {
                    MutationMethod? mutationMethod = SelectMutationMethod(b);

                    if (mutationMethod is not null)
                    {
                        LogWriter.Invoke($"{b.Name} mutate using {mutationMethod}");
                        b.Mutate((MutationMethod)mutationMethod);
                    }
                }
            }
            else
            {
                LogWriter.Invoke($"{b.Name} escaped mutation");
            }
        }
    }

    /// <summary>
    /// Creates a new brain from two parents, swapping genes at random.
    /// </summary>
    /// <param name="population"></param>
    /// <param name="totalFitness"></param>
    /// <param name="minimalFitness"></param>
    /// <returns></returns>
    private Brain GetOffspring(Brain[] population, float totalFitness, float minimalFitness)
    {
        // our offspring is the child of 2 parent brains
        Brain parent1 = GetParent(population, totalFitness, minimalFitness);
        Brain parent2 = GetParent(population, totalFitness, minimalFitness);

        Brain b = Inheritance.GenesCrossOver(parent1, parent2, true);
        b.lineage = parent1.Name == parent2.Name ? parent1.Name : $"{parent1.Name} + {parent2.Name}";

        b.LastOverallGenomeSize = (parent1.OverallGenomeSize + parent2.OverallGenomeSize) / 2;
        b.LastFitness = (parent1.Fitness + parent2.Fitness) / 2; // average of parents

        //b.Performance.Add((int)b.LastFitness);

        LogWriter.Invoke($"{b.Name} is GetOffspring() based on {parent1.Name} with {parent2.Name}");

        // the importance of this is that it renders the visualisation despite 2 networks.
        new Visualiser(b).RenderAndSaveDiagramToPNG($@"c:\temp\Offspring - Generation {Generation} {b.Name}.png");

        return b;
    }

    /// <summary>
    /// Gets a genome based on the selection function.
    /// </summary>
    /// <param name="population"></param>
    /// <param name="totalFitness"></param>
    /// <param name="minimalFitness"></param>
    private Brain GetParent(Brain[] population, float totalFitness, float minimalFitness)
    {
        int i;

        switch (Selection)
        {
            case SelectionType.POWER:
                // POWER SELECTION
                // Selects a random genome from the population, where the chance to pick a genome is proportional to its rank.
                // The random number is 0..1, and the rank is 0..N. The rank is then raised to a power, which determines the selection pressure.
                // Because the number is <1, .Pow makes it smaller. e.g 0.1 ^ 4 = 0.0001. So the higher the power, the more likely the first genomes are to be selected.

                int index = (int)Math.Floor(Math.Pow((float)(RandomNumberGenerator.GetInt32(0, 100000)) / 100000f, c_selectionPower) * population.Length);

                return population[index];

            case SelectionType.FITNESS_PROPORTIONATE:
                // FITNESS PROPORTIONATE SELECTION
                // Selects a random genome from the population, where the chance to pick a genome is proportional to its fitness.

                // choose a random number between 0 and total fitness count
                float random = ((float)RandomNumberGenerator.GetInt32(0, 100000)) / 100000 * totalFitness;
                float value = 0;

                // walk down the complete population until we find the genome that corresponds to the random number
                for (i = 0; i < population.Length; i++)
                {
                    Brain genome = population[i];
                    value += genome.Score + minimalFitness;

                    if (random < value) return genome;
                }

                // if all scores equal, return random genome
                return population[RandomNumberGenerator.GetInt32(0, population.Length)];


            case SelectionType.TOURNAMENT:
                // Makes an entirely random selection of brains from the population (up to the selection size).
                // It sorts the "selection" of brains in descending score order.
                // It tries from best to worst order, picking that brain if the random number generator picks it.
                // if none are selected, it returns the worst.

                if (c_tournamentSelectionSize > DesiredBrainPopulationSize) throw new Exception("Tournament size should be lower than the population size.");

                // make a smaller list picked completely at random from the population regardless of fitness; we then pick from
                // those in order of fitness, with a greater chance of picking higher ranking above lower
                List<Brain> individuals = new();

                for (i = 0; i < c_tournamentSelectionSize; i++)
                {
                    Brain randomBrain = population[RandomNumberGenerator.GetInt32(0, population.Length)];
                    individuals.Add(randomBrain);
                }

                // sort the small list of individuals by score (highest score first)
                individuals = individuals.OrderByDescending(x => x.Score).ToList();

                // select an individual
                for (i = 0; i < c_tournamentSelectionSize; i++)
                {
                    // going from BEST to WORST, spin the wheel and if it lands on "pick me", return that individual.
                    if (((double)RandomNumberGenerator.GetInt32(0, 100000)) / 100000 < c_tournamentSelectionProbability)
                    {
                        return individuals[i];
                    }
                }

                return individuals[c_tournamentSelectionSize - 1];
        }

        throw new Exception("logic error");
    }
}