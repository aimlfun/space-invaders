using SpaceInvadersAI.Learning.AIPlayerAndController;
using SpaceInvadersAI.Learning.Configuration;
using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.AI.ExternalInterface;
using System.Diagnostics;
using System.Text;

namespace SpaceInvadersAI.AI;

/// <summary>
/// Made up of 1 or more "connected" networks.
/// Each brain has "inputs" and "outputs". These belong to the brain, not the network.
/// </summary>
internal class Brain : IDisposable
{
    //  ████    ████      █      ███    █   █
    //  █   █   █   █    █ █      █     █   █
    //  █   █   █   █   █   █     █     ██  █
    //  ████    ████    █   █     █     █ █ █
    //  █   █   █ █     █████     █     █  ██
    //  █   █   █  █    █   █     █     █   █
    //  ████    █   █   █   █    ███    █   █                                   

    /// <summary>
    /// This is the next brain id to be used, they are numbered sequentially.
    /// </summary>
    private static int s_nextBrainIdSequence = 0;

    /// <summary>
    /// Indicates whether the brain has been optimised.
    /// </summary>
    private bool optimised = false;

    /// <summary>
    /// This provides a unique id for each brain, starting at 0.
    /// </summary>
    internal static int NextUniqueBrainID
    {
        get
        {
            return s_nextBrainIdSequence++;
        }
    }

    #region BRAIN REPRESENTATION PROPERTIES (DEFINES THE "SMARTS, AND REQUIRES SERIALISATION TO PERSIST)
    /// <summary>
    /// Name of the brain.
    /// </summary>
    internal string Name;

    /// <summary>
    /// Unique identifier for the brain.
    /// </summary>
    internal int Id;

    /// <summary>
    /// List of allowed activation functions for this brain during creation or mutation. This is configured in the UI.
    /// </summary>
    internal ActivationFunction[] AllowedActivationFunctions;

    /// <summary>
    /// Contains the INPUTs of the brain, keyed by identifier.
    /// </summary>
    internal Dictionary<string, BrainInput> BrainInputs = new();

    /// <summary>
    /// Keyed on "name"/"id" of network.
    /// Tracks the "Networks" the brain is made of.
    /// </summary>
    internal Dictionary<string, NeuralNetwork> Networks = new();

    /// <summary>
    /// Contains the OUTPUTs of the brain, keyed by identifier.
    /// </summary>
    internal readonly Dictionary<string, BrainOutput> BrainOutputs = new();

    /// <summary>
    /// Returns the total number of connections.
    /// </summary>
    internal int ConnectionsSize
    {
        get
        {
            int size = 0;

            foreach (NeuralNetwork n in Networks.Values) size += n.AllNetworkConnections.Count;

            return size;
        }
    }
    #endregion

    #region MUTATION RELATED PROPERTIES
    /// <summary>
    /// These are the allowed cell types for the brain. It needs to know this for subsequent mutation.
    /// </summary>
    internal Dictionary<CellType, int> AllowedCellTypes = new();

    /// <summary>
    /// This is the high score the brain has achieved.
    /// </summary>
    internal int RealBestScore;

    /// <summary>
    /// Indicates how well this brain fits the intended model. 
    /// Bigger = better.
    /// 
    /// Genomes can get additional blocks, and will grow indefinitely. You might want to restrict if 
    /// only to find an optimal requiring less neurons. To do that, it's best to reduce the the fitness based on size.
    /// That means out of two equally fit networks, it will "prefer" the one that works with less neurons.
    /// 
    /// e.g. subtract GenomeSize * growth from the fitness.; where you determine growth (0.001 or something)
    /// </summary>
    internal float Fitness;

    /// <summary>
    /// The explanation (stats) used to derive the fitness are placed here
    /// </summary>
    internal string FitnessSummary = "";

    /// <summary>
    /// Used to identify the source - Elite, Templated, Random, Offspring etc.
    /// </summary>
    internal string Provenance = "n/a";

    /// <summary>
    /// This is the fitness of the brain last time it played the game.
    /// </summary>
    internal float LastFitness = 0;

    /// <summary>
    /// Size of the brain (sum of network(s) brain cells).
    /// We don't include input/output as they are required for all brains.
    /// </summary>
    internal int GenomeSize
    {
        get
        {
            int size = 0;

            foreach (NeuralNetwork n in Networks.Values) size += n.Neurons.Count - n.Inputs.Count - n.Outputs.Count; // size excluding the non optional; input/output

            return size;
        }
    }

    /// <summary>
    /// This is the size of the genome - input + output + hidden + connections.
    /// </summary>
    internal int OverallGenomeSize
    {
        get
        {
            int size = 0;

            foreach (NeuralNetwork n in Networks.Values) size += n.Neurons.Count + n.AllNetworkConnections.Count - n.Inputs.Count - n.Outputs.Count;

            return size;
        }
    }

    /// <summary>
    /// This is the last overall genome size, used to determine if the genome has grown.
    /// </summary>
    internal int LastOverallGenomeSize = 0;

    /// <summary>
    /// This tracks where the brain came from, parent1 - parent2.
    /// </summary>
    internal string lineage = "";

    /// <summary>
    /// Tracks the last time this brain was mutated.
    /// </summary>
    internal int GenerationOfLastMutation = 0;

    /// <summary>
    /// This tracks the performance of the brain over time. We limit to last 5 games.
    /// </summary>
    internal List<int> Performance = new();

    /// <summary>
    /// Allow player to be graded, and given a commentary.
    /// </summary>
    internal string PlayerSummary = "";

    /// <summary>
    /// 
    /// </summary>
    internal string DNA
    {
        get
        {
            StringBuilder dna = new();
            
            foreach(NeuralNetwork n in Networks.Values)
            {
                dna.Append(n.DNA.GetHashCode() + "~");
            }

            return dna.ToString().TrimEnd('~');
        }
    }
    /// <summary>
    /// Returns the average fitness.
    /// </summary>
    /// <returns></returns>
    internal float AverageFitness()
    {
        if (Performance.Count < 1)
        {
            return Fitness;
        }

        // to avoid average of average, we create a copy and add the new value.
        List<int> perf = new(Performance)
        {
            (int)Math.Round(Fitness)
        };

        return (float)perf.Average();
    }
    #endregion

    #region PLAYER / GAME RELATED PROPERTIES
    /// <summary>
    /// Tracks the AI player that owns this brain. By populating it, we can get info about the player.
    /// </summary>
    public AIPlayer? AIPlayer { get; internal set; }

    /// <summary>
    /// Used to enable us to kill misbehaving brains.
    /// </summary>
    internal bool IsDead = false;

    /// <summary>
    /// This is the real score, not the adjusted score. i.e. the score the player achieved in the game, and not the fitness score.
    /// </summary>
    internal int RealScore = 0;

    /// <summary>
    /// This is the adjusted score, based on the fitness function.
    /// </summary>
    internal float Score;

    /// <summary>
    /// The high score achieved by this brain throughout its lifetime.
    /// </summary>
    internal int HighScore = 0;
    #endregion

    /// <summary>
    /// Static Constructor.
    /// </summary>
    static Brain()
    {
    }

    /// <summary>
    /// Creates a "brain", with specified inputs and outputs.
    /// </summary>
    /// <param name="brainName">Name to identify the brain.</param>
    /// <param name="inputParameters">Array of parameter names representing inputs.</param>
    /// <param name="outputParameters">Array of parameter names representing outputs.</param>
    /// <param name="optionalNetworkIdentifiers">If provided will create networks with these identifier.</param>
    internal Brain(string brainName, ActivationFunction[] allowedActivationFunctions, string[]? inputParameters = null, string[]? outputParameters = null, string[]? optionalNetworkIdentifiers = null)
    {
        Id =  NextUniqueBrainID;

        Name = brainName;
        AllowedActivationFunctions = allowedActivationFunctions;

        // create an input based off the parameter names. Will throw an exception if not unique.
        if (inputParameters is not null) foreach (string name in inputParameters) BrainInputs.Add(name, new BrainInput(this, name));

        // create an output based off the parameter names. Will throw an exception if not unique.
        if (outputParameters is not null) foreach (string name in outputParameters) BrainOutputs.Add(name, new BrainOutput(this, name));

        // optionally add network(s)
        if (optionalNetworkIdentifiers != null) foreach (string identifier in optionalNetworkIdentifiers) AddEmptyNetwork(identifier);
    }

    /// <summary>
    /// Removes any networks belonging to the brain.
    /// </summary>
    internal void Clear()
    {
        // kill all the networks.
        foreach (NeuralNetwork n in Networks.Values) n.Dispose();

        Performance.Clear();
        BrainInputs.Clear();
        BrainOutputs.Clear();
        Networks.Clear();
    }

    #region SERIALISATION
    /// <summary>
    /// Gets the brain represented as a template that can be used to create a new brain.
    /// i.e. if you want to create a new brain with the same inputs/outputs/networks, you can use the template it generates.
    /// </summary>
    /// <returns></returns>
    internal string GetAsTemplate()
    {
        StringBuilder serializedData = new(100);
        serializedData.AppendLine($"# BRAIN AS OF UTC {DateTime.Now.ToUniversalTime()}");
        serializedData.AppendLine($"# GAME SCORE: {RealScore}");
        serializedData.AppendLine($"NAME {Name}");

        // These are INPUT/OUTPUTs to the BRAIN not the NETWORK. Important differentiation.
        // A game could have ONE brain, 3 networks all 3 sharing the same input/output.

        serializedData.AppendLine("START INPUTS TO BRAIN");
        foreach (string id in BrainInputs.Keys) serializedData.AppendLine(BrainInputs[id].Serialise());
        serializedData.AppendLine("END INPUTS TO BRAIN");

        serializedData.AppendLine("START OUTPUTS TO BRAIN");
        foreach (string id in BrainOutputs.Keys) serializedData.AppendLine(BrainOutputs[id].Serialise());
        serializedData.AppendLine("END OUTPUTS TO BRAIN");

        // this is where we write out network specific data.

        serializedData.AppendLine("START NETWORKS");
        foreach (string id in Networks.Keys) // often this is a loop of ONE entry, brain:network but could be 1:many
        {
            // one per neural network in the brain
            serializedData.Append(Networks[id].Serialise());
        }

        serializedData.AppendLine("END NETWORKS");

        return serializedData.ToString();
    }

    /// <summary>
    /// Constructs a brain from a template.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="brainName"></param>
    /// <returns></returns>
    internal static Brain CreateFromTemplate(string x, string? brainName = null)
    {
        brainName ??= "unknown";

        string[] tokens = x.Split("\r\n");
        Dictionary<string, List<string>> networkTemplateLines = new();
        string group = "";

        foreach (string token in tokens)
        {
            if (token.StartsWith('#')) continue;

            if (token.StartsWith("NAME "))
            {
                brainName = token.Replace("NAME ", "") + ' ' + UniqueBrainID.GetNextBrainId();
                continue;
            }

            if (token.StartsWith("START "))
            {
                group = token.Split(' ')[1];

                if (!networkTemplateLines.ContainsKey(group)) networkTemplateLines.Add(group, new());
                continue;
            }

            if (token.StartsWith("END "))
            {
                string groupEnd = token.Split(' ')[1];

                if (group != groupEnd) throw new ApplicationException("START doesn't match END");

                continue;
            }

            networkTemplateLines[group].Add(token);
        }

        Brain brain = new(brainName, PersistentConfig.Settings.AllowedActivationFunctions)
        {
            AllowedCellTypes = PersistentConfig.Settings.CellTypeRatios
        };

        AddInputsToBrain(networkTemplateLines, brain);
        AddOutputsToBrain(networkTemplateLines, brain);

        // add the network(s)
        NeuralNetwork.Deserialise(networkTemplateLines["NETWORKS"].ToArray(), brain);

        return brain;
    }

    /// <summary>
    /// Adds the outputs to the brain (based on the template).
    /// </summary>
    /// <param name="networkTemplateLines"></param>
    /// <param name="brain"></param>
    private static void AddOutputsToBrain(Dictionary<string, List<string>> networkTemplateLines, Brain brain)
    {
        // START OUTPUTS TO BRAIN
        foreach (string output in networkTemplateLines["OUTPUTS"])
        {
            // ADD BRAIN-OUT ID="desired-position" ACTIVATIONFUNCTION="TanH" MIN="-2" MAX="2"
            BrainOutput brainOutput = BrainOutput.Deserialise(brain, output);
            brain.BrainOutputs.Add(brainOutput.Id, brainOutput);
        }
        // END BRAIN OUTPUTS
    }

    /// <summary>
    /// Adds the inputs to the brain (based on the template).
    /// </summary>
    /// <param name="networkTemplateLines"></param>
    /// <param name="brain"></param>
    private static void AddInputsToBrain(Dictionary<string, List<string>> networkTemplateLines, Brain brain)
    {
        // START INPUTS TO BRAIN 
        foreach (string input in networkTemplateLines["INPUTS"])
        {
            // ADD BRAIN-IN ID="player-position-x" MIN="-1" MAX="1"
            BrainInput brainInput = BrainInput.Deserialise(brain, input);
            brain.BrainInputs.Add(brainInput.Id, brainInput);
        }
        // END INPUTS TO BRAIN
    }

    /// <summary>
    /// Resets the brain to its initial state, by zeroing state and activation of all neurons.
    /// </summary>
    internal void Reset()
    {
        IsDead = false;

        foreach (NeuralNetwork n in Networks.Values)
        {
            foreach (BaseCell cell in n.Neurons.Values)
            {
                cell.State = 0;
                cell.Activation = 0;           
            }

            foreach(BrainInput b in BrainInputs.Values)
            {
                // don't have state, because the inputs are supplied by the caller.
                b.Activation = 0;
            }

            foreach (BrainOutput b in BrainOutputs.Values)           
            {
                b.State = 0;
                b.ResultOfLastActivation = 0;
            }
        }

        AIPlayer?.Dispose();
        AIPlayer = null;
    }
    #endregion

    /// <summary>
    /// Applies a single min/max range for ALL inputs and outputs.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    internal Brain SetInputOutputMinMaxRange(double min = -int.MaxValue, double max = int.MaxValue)
    {
        foreach (BrainInput bi in BrainInputs.Values)
        {
            bi.MaximumValue = max;
            bi.MinimumValue = min;
        }

        foreach (BrainOutput bo in BrainOutputs.Values)
        {
            bo.MaximumValue = max;
            bo.MinimumValue = min;
        }

        return this;
    }

    /// <summary>
    /// Handles mutation of the brain. There can be more than one network in a brain, so we need to mutate all of them.
    /// </summary>
    /// <param name="mutationMethod"></param>
    internal void Mutate(MutationMethod mutationMethod)
    {
        // mutate all networks in brain
        foreach (NeuralNetwork neuralNetwork in Networks.Values)
        {
            neuralNetwork.Mutate(mutationMethod);
        }
    }

    /// <summary>
    /// Gets a list of connections to the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    internal Connection[] GetConnectionsTo(string id)
    {
        List<Connection> connections = new();

        foreach (NeuralNetwork network in Networks.Values)
        {
            foreach (BaseCell cell in network.Neurons.Values)
            {
                foreach (Connection con in cell.InboundConnections)
                {
                    if (con.To == id) connections.Add(con);
                }
            }
        }

        return connections.ToArray();
    }

    /// <summary>
    /// Gets a list of all connections from the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    internal Connection[] GetConnectionsFrom(string id)
    {
        List<Connection> connections = new();

        foreach (NeuralNetwork network in Networks.Values)
        {
            foreach (BaseCell cell in network.Neurons.Values)
            {
                foreach (Connection con in cell.OutboundConnections)
                {
                    if (con.From == id) connections.Add(con);
                }
            }
        }

        return connections.ToArray();
    }

    /// <summary>
    /// Adds an empty network with the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="inputNames"></param>
    /// <param name="outputNames"></param>
    /// <returns></returns>
    internal NeuralNetwork AddEmptyNetwork(string id)
    {
        NeuralNetwork n = new(id, this);
        Networks.Add(id, n);

        return n;
    }

    /// <summary>
    /// Creates a neural network with the given inputs and outputs.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="inputNames"></param>
    /// <param name="outputNames"></param>
    /// <returns></returns>
    internal NeuralNetwork AddNetworkWithConnectedInputOutputs(string id, ActivationFunction[] allowedActivationFunctions)
    {
        NeuralNetwork n = new(id, this, BrainInputs.Keys.ToArray(), BrainOutputs.Keys.ToArray());
        Networks.Add(id, n);
        n.AllowedActivationFunctions = allowedActivationFunctions;

        return n;
    }

    #region NEURAL NETWORK FEEDBACK
    /// <summary>
    /// Assigns a value to an input. Equivalent to {brain}.Inputs["{parameter}"].Value = {double value}.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal Brain SetInputValue(string name, double value)
    {
        if (!BrainInputs.ContainsKey(name)) throw new ArgumentException("input not known", nameof(name));

        BrainInputs[name].Activation = value;

        return this;
    }

    /// <summary>
    /// Assigns all input values in one go from an array.
    /// Assumption is given you provide values, and not the input "name" that they are in order.
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal Brain SetInputValues(double[] values)
    {
        if (BrainInputs.Count != values.Length) throw new ArgumentOutOfRangeException(nameof(values), "setting values directly without names requires the same number of items");

        BrainInput[] inputs = BrainInputs.Values.ToArray();

        for (int inputIndex = 0; inputIndex < values.Length; inputIndex++)
        {
            inputs[inputIndex].Activation = values[inputIndex];
        }

        return this;
    }

    /// <summary>
    /// Applies a feed forward to all networks in the brain.
    /// </summary>
    /// <param name="externalInputs">This doesn't need to be the complete list of inputs. Some outputs can be linked to an input.</param>
    /// <param name="externalOutputNames"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal Dictionary<string, double> FeedForward(Dictionary<string, double>? externalInputs = null, List<string>? externalOutputNames = null)
    {
        if (!optimised)
        {
            OptimiseNetwork();
            optimised = true;
        }

        if (externalInputs is not null)
        {
            // assign all the "external" input values
            foreach (string id in externalInputs.Keys)
            {
                BrainInputs[id].Activation = externalInputs[id];
            }
        }

        // perform the feed forward on all networks belonging to brain
        foreach (string id in Networks.Keys)
        {
            Networks[id].FeedForward();
        }

        // return the requested "outputs" 

        Dictionary<string, double> outputs = new();

        // default them, if not specified
        if (externalOutputNames is null)
        {
            externalOutputNames = new();
            foreach (string s in BrainOutputs.Keys) externalOutputNames.Add(s);
        }

        foreach (string id in externalOutputNames)
        {            
            outputs.Add(id, BrainOutputs[id].Activation);
        }

        return outputs;
    }

    /// <summary>
    /// We can optimise the network by avoiding using Dictionary lookups. An array is faster.
    /// </summary>
    private void OptimiseNetwork()
    {
        // perform the feed forward on all networks belonging to brain
        foreach (string id in Networks.Keys)
        {
            Networks[id].Optimise();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal void WriteOutputs()
    {
        Debug.WriteLine("AFTER FEEDBACK");
        Debug.WriteLine("");
        Debug.WriteLine("INPUT(S):");
        foreach (string s in BrainInputs.Keys) Debug.WriteLine($"{s} <= {BrainInputs[s].Activation}");

        Debug.WriteLine("");
        Debug.WriteLine("NEURON(S):");
        foreach (NeuralNetwork n in Networks.Values)
        {
            foreach (var x in n.Neurons.Values)
            {
                Debug.WriteLine($"ID: {x.Id} ACTIVATION: {x.Activation} {x.ParameterAnnotation}");
            }
        }

        Debug.WriteLine("\nOUTPUT(S):");
        foreach (string s in BrainOutputs.Keys) Debug.WriteLine($"{s} => {BrainOutputs[s].Activation}");
        Debug.WriteLine("==================================================");
    }
    #endregion

    /// <summary>
    /// Standard dispose method.
    /// </summary>
    public void Dispose()
    {
        Clear();
    }

    /// <summary>
    /// Gets a cell from the brain using the id provided.
    /// </summary>
    /// <param name="cellId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal BaseCell GetCell(string cellId)
    {
        foreach (NeuralNetwork n in Networks.Values)
        {
            if (n.Neurons.TryGetValue(cellId, out BaseCell? value)) return value;
        }

        throw new ArgumentOutOfRangeException(nameof(cellId), "Id not found as input, output or brain cell.");
    }

    /// <summary>
    /// Outputs the brain in human readable form.
    /// </summary>
    /// <returns></returns>
    internal string GetBrainAsText()
    {
        Brain brain = this;
        StringBuilder sb = new(1000);
        sb.AppendLine($"BRAIN DUMP:");

        sb.AppendLine($"Brain: \"{brain.Name}\" lineage: \"{brain.lineage}\" genomeSize: {brain.GenomeSize}");
        sb.AppendLine($"  Inputs: {brain.BrainInputs.Count} Outputs: {brain.BrainOutputs.Count} Networks: {brain.Networks.Count}");
        
        // Remember the "brain" as my implementation can have multiple networks attached to common inputs and outputs.
        // imagine the human brain, where the eyes, ears, nose, touch are inputs. There are different circuits in the brain that use these inputs.
        // Not all networks have to use all inputs and outputs.

        // dump the inputs and outputs to the BRAIN
        sb.Append("  Inputs:");
        foreach(BrainInput bi in brain.BrainInputs.Values)
        {
            sb.Append($" \"{bi.Id}\" ({bi.MinimumValue}..{bi.MaximumValue}) ");
        }
        sb.AppendLine("");
        
        sb.Append("  Outputs:");
        foreach (BrainOutput bo in brain.BrainOutputs.Values)
        {
            sb.Append($" \"{bo.Id}\" ({bo.MinimumValue}..{bo.MaximumValue}) ");
        }
        sb.AppendLine("");

        if (brain.Networks.Count == 0)
        {
            sb.AppendLine("  No Networks.");
        }
        else
        {
            // for each network (typically one, but could be more), output the neurons and connections
            foreach (NeuralNetwork n in brain.Networks.Values)
            {
                sb.AppendLine($"\n  Network: \"{n.Id}\" inputs: {n.Inputs.Count} outputs: {n.Outputs.Count} tot-connections: {n.AllNetworkConnections.Count}");

                // sort neurons by id, so comparing two brains via DIFF is easier
                List<BaseCell> neurons = n.Neurons.Values.ToList();
                neurons.Sort((x, y) => x.Id.CompareTo(y.Id));

                foreach (BaseCell nu in neurons)
                {
                    sb.AppendLine($"    {nu.ToString()}");

                    // sort inbound connections by from-to, so comparing two brains via DIFF is easier
                    List<Connection> inbound = nu.InboundConnections.ToList();
                    inbound.Sort((x, y) => x.KeyFromTo.CompareTo(y.KeyFromTo));

                    foreach (Connection c in inbound)
                    {
                        sb.AppendLine($"       Inbound:  \"{c.From}\" to \"{c.To}\" Weight: {c.Weight}");
                    }

                    // sort outbound connections by from-to, so comparing two brains via DIFF is easier
                    List<Connection> outbound = nu.OutboundConnections.ToList();
                    outbound.Sort((x, y) => x.KeyFromTo.CompareTo(y.KeyFromTo));

                    foreach (Connection c in outbound)
                    {
                        sb.AppendLine($"       Outbound: \"{c.From}\" to \"{c.To}\" Weight: {c.Weight}");
                    }

                    sb.AppendLine("");
                }
          
                sb.AppendLine("");
            }
        }

        return sb.ToString();
    }
}