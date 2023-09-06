using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.Learning.Configuration;
using SpaceInvadersAI.AI.Utilities;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SpaceInvadersAI.AI;

/// <summary>
/// PRINCIPLE: This enables it to run replacing existing code.
///     "standard interface => 
///        "inputs" double[] 
/// 	   "outputs" double[]
///        double[] FeedForward(double[] inputs)
///     
///  mutate => 
/// 	"options", what it can do, and how much
/// 	"top %" to preserve(elitism)
/// 	"rules" for offspring.
/// </summary>
internal class NeuralNetwork : IDisposable
{
    //  █   █   █████   █   █   ████      █     █               █   █   █████   █████   █   █    ███    ████    █   █
    //  █   █   █       █   █   █   █    █ █    █               █   █   █         █     █   █   █   █   █   █   █  █
    //  ██  █   █       █   █   █   █   █   █   █               ██  █   █         █     █   █   █   █   █   █   █ █
    //  █ █ █   ████    █   █   ████    █   █   █               █ █ █   ████      █     █ █ █   █   █   ████    ██
    //  █  ██   █       █   █   █ █     █████   █               █  ██   █         █     █ █ █   █   █   █ █     █ █
    //  █   █   █       █   █   █  █    █   █   █               █   █   █         █     ██ ██   █   █   █  █    █  █
    //  █   █   █████    ███    █   █   █   █   █████           █   █   █████     █     █   █    ███    █   █   █   █


    /// <summary>
    /// The unique identifier for this network, used to reference it.
    /// </summary>
    internal string Id;

    /// <summary>
    /// Brains can have multiple networks. This tracks which brain the network is part of.
    /// </summary>
    internal Brain BrainItIsPartOf;

    /// <summary>
    /// Neurons in this neural network.
    /// </summary>
    internal Dictionary<string, BaseCell> Neurons = new();

    // more optimal structures for FeedForward.

    /// <summary>
    /// During optimise phase, we put the neurons into an array for faster access.
    /// </summary>
    internal BaseCell[]? NeuronsAsArray;

    /// <summary>
    /// During optimise phase, we put the inputs into an array for faster access.
    /// </summary>
    internal BaseCell[] InputAsArray { get; private set; }

    /// <summary>
    /// During optimise phase, we put the outputs into an array for faster access.
    /// </summary>
    internal BaseCell[] OutputAsArray { get; private set; }

    /// <summary>
    /// During optimise phase, we put the hidden neurons into an array for faster access.
    /// </summary>
    internal BaseCell[] HiddenAsArray { get; private set; }
    
    /// <summary>
    /// Connections between neurons in this neural network.
    /// </summary>
    internal readonly Dictionary<string, Connection> AllNetworkConnections = new();

    /// <summary>
    /// The external INPUTs to this neural network.
    /// </summary>
    internal Dictionary<string, InputCell> Inputs = new();

    /// <summary>
    /// The OUTPUTs to this neural network.
    /// </summary>
    internal Dictionary<string, OutputCell> Outputs = new();

    /// <summary>
    /// This contains the allowed activation functions for this network.
    /// </summary>
    internal ActivationFunction[]? AllowedActivationFunctions = null;

    internal string DNA
    {
        get
        {
            StringBuilder sb = new();

            foreach (Connection c in AllNetworkConnections.Values)
            {
                sb.Append($"{c.From}~{c.To}~{c.Weight.ToString("F2")}~");
            }

            foreach (BaseCell c2 in Neurons.Values)
            {
                sb.Append(c2.DNA + "~");
            }

            return sb.ToString().GetHashCode()+"~";
        }
    }
    
    /// <summary>
    /// Returns true if the network has connections and brain cells.
    /// </summary>
    internal bool IsInitialised
    {
        get { return Neurons.Count > 0 && AllNetworkConnections.Count > 0; }
    }


    /// <summary>
    /// Constructor to create a neural network with the external inputs and outputs.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="brainNetworkBelongsTo"></param>
    /// <param name="inputs"></param>
    /// <param name="outputs"></param>
    /// <param name="addInputs"></param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. <<<-- before use, it calls "Optimise()" and that populates the arrays.
    internal NeuralNetwork(string id, Brain brainNetworkBelongsTo, string[]? inputs = null, string[]? outputs = null, bool MakeInterface = true)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        Id = id;

        BrainItIsPartOf = brainNetworkBelongsTo;

        // copy the allowed activation functions from the brain to the neural network
        AllowedActivationFunctions = brainNetworkBelongsTo.AllowedActivationFunctions;

        // if called with inputs, then we add them as neurons type=INPUT
        if (inputs is not null)
        {
            // copy the inputs (value by ref works, thankfully i.e. edit brain input changes network input)
            foreach (string inpId in inputs)
            {
                if (!brainNetworkBelongsTo.BrainInputs.ContainsKey(inpId))
                {
                    throw new ArgumentException($"input \"{inpId}\" does not reference a known brain input.", nameof(inputs));
                }

                if (MakeInterface)
                {
                    InputCell input = new(brainNetworkBelongsTo, this, inpId, Utils.RandomFloatBetweenMinusHalfToPlusHalf(), BaseCell.RandomActivationFunction(this), RarelyModifiedSettings.DefaultCellActivationThreshold);
                    Inputs.Add(inpId, input);
                    Neurons.Add(inpId, input);
                }
            }
        }

        // if called with outputs, then we add them as neurons type=OUTPUT
        if (outputs is not null)
        {
            // copy the inputs (value by ref works, thankfully i.e. edit output changes network output)
            foreach (string idOfOutput in outputs as string[])
            {
                if (!brainNetworkBelongsTo.BrainOutputs.ContainsKey(idOfOutput))
                {
                    throw new ArgumentException($"output \"{idOfOutput}\" does not reference a known brain output.", nameof(inputs));
                }

                if (MakeInterface)
                {
                    OutputCell output = new(brainNetworkBelongsTo, this, idOfOutput, Utils.RandomFloatBetweenMinusHalfToPlusHalf(), BaseCell.RandomActivationFunction(this), RarelyModifiedSettings.DefaultCellActivationThreshold);
                    Outputs.Add(idOfOutput, output);
                    Neurons.Add(idOfOutput, output);
                }
            }
        }

        // joins all inputs to outputs, if BOTH are specified.
        if (inputs is not null && outputs is not null && MakeInterface) ApplyRectifierImprovement();
    }

    /// <summary>
    /// Connect input nodes with output nodes directly applying the approach as per this discussion. ** not tested**
    /// 
    /// It applies to ReLU style networks.
    /// https://stats.stackexchange.com/questions/47590/what-are-good-initial-weights-in-a-neural-network/248040#248040
    /// </summary>
    /// <returns></returns>
    internal NeuralNetwork ApplyRectifierImprovement()
    {
        foreach (InputCell bi in Inputs.Values)
        {
            foreach (OutputCell bo in Outputs.Values)
            {
                // https://arxiv.org/abs/1502.01852
                // Rectified activation units (rectifiers) are essential for state-of-the-art neural networks. In this work, we 
                // rectifier neural networks for image classification from two aspects. First, we propose a Parametric Rectified Linear Unit (3) 
                // that generalizes the traditional rectified unit. PReLU improves model fitting with nearly zero extra computational cost and little 
                // overfitting risk. Second, we derive a robust initialization method that particularly considers the rectifier nonlinearities. 
                // This method enables us to train extremely deep rectified models directly from scratch and to investigate deeper or wider network 
                // architectures. Based on our PReLU networks (PReLU-nets), we achieve 4.94% top-5 test error on the ImageNet 2012 classification dataset. 
                // This is a 26% relative improvement over the ILSVRC 2014 winner (GoogLeNet, 6.66%). To our knowledge, our result is the first to surpass 
                // human-level performance (5.1%, Russakovsky et al.) on this visual recognition challenge.

                double weight = Utils.RandomNumberPlusOrMinus1() * Inputs.Count * Math.Sqrt((double)2 / (double)Inputs.Count);

                // no point in having 0 weight connections, they just slow down processing
                if (Math.Abs(weight) > 0.000001f) Connect(bi, bo, weight);
            }
        }

        return this;
    }

    #region CELL ADD
    /// <summary>
    /// Creates a new cell based on the type specified.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cellType"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal BaseCell CreateNewCell(Brain b, string? id, CellType cellType, double bias, ActivationFunction activationFunction, double cellActivationThreshold)
    {
        id ??= GetUniqueCellId();

        BaseCell cell = cellType switch
        {
            CellType.AND => new AndCell(b, this, id, bias, activationFunction, cellActivationThreshold),
            CellType.IF => new IfCell(b, this, id, bias, activationFunction, cellActivationThreshold),
            CellType.MAX => new MaxCell(b, this, id, bias, activationFunction, cellActivationThreshold),
            CellType.MIN => new MinCell(b, this, id, bias, activationFunction, cellActivationThreshold),
            CellType.PERCEPTRON => new PerceptronCell(b, this, id, bias, activationFunction, cellActivationThreshold),
            CellType.TRANSISTOR => new TransistorCell(b, this, id, bias, activationFunction, cellActivationThreshold),
            CellType.INPUT => new InputCell(b, this, id, bias, activationFunction, cellActivationThreshold),
            CellType.OUTPUT => new OutputCell(b, this, id, bias, activationFunction, cellActivationThreshold),
            _ => throw new ArgumentException( $"cell type not supported {cellType}", nameof(cellType)),
        };

        return cell;
    }

    /// <summary>
    /// Adds a specific brain cell type, with a random activation function.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cellType"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal BaseCell AddCell(string? id, CellType cellType, double bias, ActivationFunction activationFunction, double cellActivationThreshold)
    {
        BaseCell cell = CreateNewCell(BrainItIsPartOf, id, cellType, bias, activationFunction, cellActivationThreshold);
        Neurons.Add(cell.Id, cell);

        return cell;
    }

    /// <summary>
    /// Adds a "brain" cell with the chosen activation function (as opposed to random).
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cellType"></param>
    /// <param name="activationFunction">A specific activation function.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal BaseCell AddCell(string id, CellType cellType, ActivationFunction activationFunction)
    {
        BaseCell cell = AddCell(id, cellType, Utils.RandomFloatBetweenMinusHalfToPlusHalf(),activationFunction, RarelyModifiedSettings.DefaultCellActivationThreshold);

        cell.Network = this;

        return cell;
    }

    /// <summary>
    /// Adds a "brain" cell with the chosen activation function (as opposed to random).
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cellType"></param>
    /// <param name="activationFunction">A specific activation function.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal BaseCell AddCell(string id, CellType cellType, ActivationFunction activationFunction, double bias)
    {
        BaseCell cell = AddCell(id, cellType, activationFunction);

        cell.Bias = bias;

        return cell;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cellId"></param>
    /// <returns></returns>
    internal bool IsUniqueCellId(string cellId)
    {
        return !Neurons.ContainsKey(cellId) &&
               !Inputs.ContainsKey(cellId) &&
               !Outputs.ContainsKey(cellId);
    }

    int nextIntegerId = 0;
    private bool disposedValue;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    internal string GetUniqueCellId(string format = "{network}-hidden-{integer}")
    {
        while (true) // TO DO: allow other formats
        {
            string uniqueId = format.Replace("{network}", this.Id)
                                    .Replace("{integer}", (nextIntegerId++).ToString());

            if (IsUniqueCellId(uniqueId)) return uniqueId;

            ++nextIntegerId;
        }
    }
    #endregion

    #region CONNECT TO

    // TWO ways to connect, chainable because it returns the network i.e myNetwork.Connect(..).Connect(...)...

    // RULE: It must ALWAYS add to cell.InboundConnections, cell.OutboundConnections, and AllNetworkConnections.

    /// <summary>
    /// Connect two cells together using by "cells" not id.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    internal NeuralNetwork Connect(BaseCell a, BaseCell b, double weight = 1)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        a.ConnectTo(b, weight);

        return this;
    }

    /// <summary>
    /// Connect two cells together using by "id".
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    internal NeuralNetwork Connect(string a, string b, double weight = 1)
    {
        if (string.IsNullOrEmpty(a)) throw new ArgumentNullException(nameof(a));
        if (string.IsNullOrEmpty(b)) throw new ArgumentNullException(nameof(b));

        // ensure the cells being connected exist
        BaseCell? cellA = Neurons[a] ?? throw new ArgumentOutOfRangeException(nameof(a), "network does not contain cell");
        BaseCell? cellB = Neurons[b] ?? throw new ArgumentOutOfRangeException(nameof(b), "network does not contain cell");

        cellA.ConnectTo(cellB, weight);
        return this;
    }

    /// <summary>
    /// Registers a connection in the network.
    /// </summary>
    /// <param name="conn"></param>
    /// <returns></returns>
    internal NeuralNetwork RegisterConnection(Connection conn)
    {
        if (AllNetworkConnections.ContainsKey(conn.KeyFromTo))
        {
            Debugger.Break();
            return this;
        }

        AllNetworkConnections.Add(conn.KeyFromTo, conn);
        return this;
    }

    /// <summary>
    /// Determines if two neurons are connected.
    /// </summary>
    /// <param name="idFrom"></param>
    /// <param name="idTo"></param>
    /// <returns></returns>
    internal bool NeuronsAreConnected(string idFrom, string idTo)
    {
        return AllNetworkConnections.ContainsKey(Connection.ComputeKey(idFrom, idTo));
    }

    #endregion

    #region ACTIVATION / FEED FORWAARD
    /// <summary>
    /// Activate cells in order.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    internal void FeedForward()
    {
        // we do this in order. We apply impact of the "Inputs" first, before hidden, and lastly determine the output.

        // A possible optimisation is to do this in parallel, but we need to be careful about the order of activation, but this is too
        // impossible as you have self-connections, and loops.

        // A more practical improvement is to maintain an array for input, hidden, and output. That saves filtering, and reduces the loop iterations.

        for (int i = 0; i < InputAsArray.Length; i++)
        {
            InputAsArray[i].Activate();
        }

        for (int i = 0; i < HiddenAsArray.Length; i++)
        {
            HiddenAsArray[i].Activate();
        }

        for (int i = 0; i < OutputAsArray.Length; i++)
        {
            OutputAsArray[i].Activate();
        }
    }
    #endregion 

    #region SERIALISATION
    /// <summary>
    /// Provides the neural network serialised into a simple human readable format.
    /// </summary>
    /// <returns></returns>
    internal string Serialise()
    {
        StringBuilder sb = new(100);

        sb.AppendLine($"ADD NETWORK ID=\"{Id}\"");

        // write all non input/output cells (they belong to the brain)
        foreach (string id in Neurons.Keys)
        {
            sb.AppendLine(Neurons[id].Serialise());
        }
    
        // write all connections for this network (includes output)
        foreach (string id in AllNetworkConnections.Keys)
        {
            sb.AppendLine(AllNetworkConnections[id].Serialise());
        }

        return sb.ToString();
    }

    /// <summary>
    /// De-serialises a network (text to object).
    /// </summary>
    /// <param name="linesPfText"></param>
    /// <param name="brainNetworkBelongsTo"></param>
    /// <returns></returns>
    internal static void Deserialise(string[] linesPfText, Brain brainNetworkBelongsTo)
    {
        int line = 0;

        Dictionary<string, List<string>> networksAsText = new();

        string ID = "";

        // first pass, we just want to split the text into networks
        while (line < linesPfText.Length)
        {
            string thisLine = linesPfText[line++];

            if (thisLine.StartsWith('#'))
            {
                continue; // # indicates its a comment
            }

            // the brain definition wants to add a network
            if (thisLine.StartsWith("ADD NETWORK "))
            {
                Dictionary<string, string> tokens2 = Utils.RegExpParseTokens(thisLine.Replace("ADD NETWORK ", ""));
                ID = Utils.SafeGetAttribute(tokens2, "ID");

                if (!networksAsText.ContainsKey(ID))
                {
                    networksAsText.Add(ID, new());
                }

                continue;
            }

            if (thisLine.StartsWith("END "))
            {
                ID = "";

                continue;
            }

            // associate the line with the network, which we'll construct afterwards.
            networksAsText[ID].Add(thisLine);
        }

        // second pass, we parse and construct the networks.
        // we don't need to handle comments, as they are ignored in the first pass.

        foreach (string id in networksAsText.Keys)
        {
            NeuralNetwork n = new(
                id: id,
                brainNetworkBelongsTo: brainNetworkBelongsTo,
                inputs: brainNetworkBelongsTo.BrainInputs.Keys.ToArray(),
                outputs: brainNetworkBelongsTo.BrainOutputs.Keys.ToArray(),
                MakeInterface: false);
            brainNetworkBelongsTo.Networks.Add(n.Id, n);
            n.AllowedActivationFunctions = PersistentConfig.Settings.AllowedActivationFunctions;

            // this is dependent on the connections being serialised after the cells.

            foreach (string lineOfTextExpressingNetwork in networksAsText[id])
            {
                if (lineOfTextExpressingNetwork.StartsWith("ADD CELL "))
                {

                    BaseCell cell = BaseCell.Deserialise(brainNetworkBelongsTo, n, lineOfTextExpressingNetwork);
                    if (cell is InputCell cell1) n.Inputs.Add(cell.Id,cell1);
                    if (cell is OutputCell cell2) n.Outputs.Add(cell.Id, cell2);
                }

                if (lineOfTextExpressingNetwork.StartsWith("ADD CONNECTION "))
                {
                    Connection c2 = Connection.Deserialise(n, lineOfTextExpressingNetwork);
                    n.Connect(c2.From, c2.To, c2.Weight);
                }
            }
        }
    }
    #endregion

    #region MUTATION
    /// <summary>
    /// Handles mutations.
    /// </summary>
    /// <param name="mutationMethod">Contains the type of mutation to apply.</param>
    internal void Mutate(MutationMethod mutationMethod)
    {
        switch (mutationMethod)
        {
            case MutationMethod.AddCell:
                {
                    InsertNewCellAtRandomPosition();
                    break;
                }

            case MutationMethod.RemoveCell:
                {
                    RemoveCellAtRandomPosition();
                    break;
                }

            case MutationMethod.AddConnection:
                {
                    AddRandomConnection();
                    break;
                }

            case MutationMethod.AddSelfConnection:
                {
                    AddSelfConnection();
                    break;
                }

            case MutationMethod.RemoveConnection:
                {
                    RemoveRandomConnection(removeSelfConnection: false);
                    break;
                }

            case MutationMethod.RemoveSelfConnection:
                {
                    RemoveRandomConnection(removeSelfConnection: true);
                    break;
                }

            case MutationMethod.ModifyWeight:
                {
                    MutateWeightOfConnection(RarelyModifiedSettings.MutationCellWeightMinDelta, RarelyModifiedSettings.MutationCellWeightMaxDelta);
                    break;
                }

            case MutationMethod.ModifyBias:
                {
                    MutateBiasOnARandomCellIncludingOutput(RarelyModifiedSettings.MutationCellBiasMinDelta, RarelyModifiedSettings.MutationCellBiasMaxDelta);
                    break;
                }

            case MutationMethod.ModifyActivationFunction:
                {
                    MutateActivationFunctionOnARandomCell();
                    break;
                }

            case MutationMethod.ModifyThreshold:
                {
                    MutateThresholdOnARandomCell();
                    break;
                }

            case MutationMethod.SwapBiasAndActivationBetweenNodes:
                {
                    SwapBiasAndActivationBetweenToRandomCells();
                    break;
                }

            default:
                break;
        }
    }

    /// <summary>
    /// Inserts a cell at a random position in the network. Doing so means something connected to another cell suddenly 
    /// has this new cell in between. We have to disconnect inbound connections, connect to the new cell, then connect 
    /// from the new cell to the previous outbound cells.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    internal BaseCell InsertCellAtRandomPosition(BaseCell cell)
    {
        int cnt = AllNetworkConnections.Count;

        if (cnt == 0)
        {
            Neurons.Add(cell.Id, cell);
        }
        else
        {
            // pick a random connection
            Connection connection = AllNetworkConnections.Values.ToArray()[RandomNumberGenerator.GetInt32(0, cnt)];

            // store what it was previously connected
            BaseCell previousConnectedFrom = Neurons[connection.From];
            BaseCell previousConnectedTo = Neurons[connection.To];

            double weight = connection.Weight;

            DisconnectInboundConnections(connection);
            DisconnectOutboundConnections(connection);

            // connect to the new cell
            previousConnectedFrom.ConnectTo(cell, weight);

            // connect from new cell to previous endpoint
            cell.ConnectTo(previousConnectedTo);

            // order is important as we evaluate in that order.

            List<BaseCell> cells = Neurons.Values.ToList();
            List<string> keys = Neurons.Keys.ToList();

            int pos = keys.IndexOf(previousConnectedFrom.Id);

            if (keys.Count == 0)
            {
                keys.Add(cell.Id);
                cells.Add(cell);
            }
            else
            {
                keys.Insert(pos, cell.Id);
                cells.Insert(pos, cell);
            }

            Neurons = keys.Zip(cells, (key, val) => new { key, val }).ToDictionary(x => x.key, x => x.val);
        }

        return cell;
    }

    /// <summary>
    /// Removes a cell at a random position in the network. What happens to the connections? We have to reassign them.
    /// It will not remove input or output cells, because that will crash the app using the network.
    /// </summary>
    internal void RemoveCellAtRandomPosition()
    {
        if (Neurons.Count == 0) return; // only input/output left

        BaseCell[] cells = Neurons.Values.ToArray();

        int idOfCellToRemove = RandomNumberGenerator.GetInt32(0, Neurons.Values.Count);

        // pick a random brain cell
        BaseCell braincell = cells[idOfCellToRemove];

        // cannot remove input or outputs
        if (braincell.Type == CellType.INPUT || braincell.Type == CellType.OUTPUT) return;

        // reassign connections in and out.
        /*
         *    [i1] [i2] [i3]          [i1] [i2] [i3]
         *       \   |   /               \  |  /          i.e. remove c, and redirect inputs to a random output
         *          [c}          =>        [o2]
         *         /   \
         *       [o1]  [o2]
         */

        // small chance of bypassing node (inputs go to one of the random outputs)
        if (Neurons.Count > 1 && braincell.OutboundConnections.Count > 0 && braincell.InboundConnections.Count > 0)
        {
            // walk down every output, and attach it to an input if INPUT/OUTPUT (or random chance)
            Connection[] outboundConnectionArray = braincell.OutboundConnections.ToArray();
            Connection[] inboundConnectionArray = braincell.InboundConnections.ToArray();

            foreach (Connection connOut in outboundConnectionArray)
            {
                foreach (Connection connection in inboundConnectionArray)
                {
                    BaseCell previousConnectedFrom = Neurons[connection.From];
                    BaseCell linkConnectionTo = Neurons[connOut.To];

                    if (Utils.FiftyPercentChanceAtRandom() || previousConnectedFrom.Type == CellType.INPUT || linkConnectionTo.Type == CellType.OUTPUT) // 33% chance of re-routing
                    {
                        // store what it was previously connected
                        if (AllNetworkConnections.ContainsKey(Connection.ComputeKey(connection.From, connOut.To)))
                        {
                            continue;
                        }

                        // connect to the new cell
                        previousConnectedFrom.ConnectTo(linkConnectionTo);
                    }
                }
            }
        }

        // remove connections for the cell we are removing

        foreach (Connection inboundConnection in new List<Connection>(braincell.InboundConnections))
        {
            DisconnectInboundConnections(inboundConnection);
        }

        foreach (Connection outboundConnection in new List<Connection>(braincell.OutboundConnections))
        {
            DisconnectOutboundConnections(outboundConnection);
        }

        // lastly remove the brain cell
        if (!Neurons.Remove(braincell.Id)) Debugger.Break();
    }

    /// <summary>
    /// Adds a specific brain cell type, with a random activation function.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cellType"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal BaseCell InsertNewCellAtRandomPosition(string? id = null, CellType? cellType = null)
    {
        cellType ??= GetCellType();

        BaseCell cell = CreateNewCell(BrainItIsPartOf, id, (CellType)cellType, Utils.RandomFloatBetweenMinusHalfToPlusHalf(),BaseCell.RandomActivationFunction(this), RarelyModifiedSettings.DefaultCellActivationThreshold);

        InsertCellAtRandomPosition(cell);

        if (cell.InboundConnections.Count < cell.MinimumInputs) AddRandomConnection();

        return cell;
    }

    /// <summary>
    /// Pick a random cell type based on ratio user provides.
    /// </summary>
    /// <returns></returns>
    private CellType GetCellType()
    {
        int total = 0;

        foreach (var x in BrainItIsPartOf.AllowedCellTypes.Values) total += x;

        int randomCell = total == 1 ? 0 : RandomNumberGenerator.GetInt32(0, total);

        int val = 0;

        foreach (CellType cellType in BrainItIsPartOf.AllowedCellTypes.Keys)
        {
            val += BrainItIsPartOf.AllowedCellTypes[cellType];

            if (val >= randomCell)
            {
                return cellType;
            }
        }

        return BrainItIsPartOf.AllowedCellTypes.Keys.ToArray()[^1];
    }

    /// <summary>
    /// Disconnects an inbound connection from the network.
    /// </summary>
    /// <param name="inboundConnection"></param>
    private void DisconnectInboundConnections(Connection inboundConnection)
    {
        foreach (Connection c in Neurons[inboundConnection.From].OutboundConnections.ToArray())
        {
            if (c.To != inboundConnection.To) continue;

            if (!Neurons[inboundConnection.From].OutboundConnections.Remove(c)) Debugger.Break();

            if (!AllNetworkConnections.Remove(c.KeyFromTo)) Debugger.Break();
        }
    }

    /// <summary>
    /// Disconnects an outbound connection from the network.
    /// </summary>
    /// <param name="outboundConnection"></param>
    private void DisconnectOutboundConnections(Connection outboundConnection)
    {
        foreach (Connection c in Neurons[outboundConnection.To].InboundConnections.ToArray())
        {
            if (c.From != outboundConnection.From)
            {
                continue;
            }

            if (!Neurons[outboundConnection.To].InboundConnections.Remove(c)) Debugger.Break();

            AllNetworkConnections.Remove(c.KeyFromTo);
        }
    }

    /// <summary>
    /// Attaches a cell to a random other cell.
    /// </summary>
    internal void AddRandomConnection()
    {
        // indexable as array
        BaseCell[] cells = Neurons.Values.ToArray();
        BaseCell[] inputs = Inputs.Values.ToArray();
        BaseCell[] outputs = Outputs.Values.ToArray();

        // make a list of ones we can connect, that aren't already connected
        List<TwoBrainCellsInOneObject> availableToConnect = new();

        int attempts = 300;

        while (true)
        {
            int firstCell = cells.Length == 0 ? 1 : RandomNumberGenerator.GetInt32(0, cells.Length);
            BaseCell first = cells[firstCell];
            int type = RandomNumberGenerator.GetInt32(0, 3);

            switch (type)
            {
                case 0: // connect to input cell
                    // provide available input to a cell connections.
                    int inputCell = inputs.Length == 1 ? 0 : RandomNumberGenerator.GetInt32(0, inputs.Length);

                    BaseCell second = inputs[inputCell];
                    if (first == second) continue;

                    if (first.SupportsAdditionalConnection && !second.IsConnectedTo(first)) // some cell types only support a fixed number of inputs
                    {
                        // from input to cell
                        availableToConnect.Add(new TwoBrainCellsInOneObject(second, first));
                    }

                    break;

                case 1:  // connect to output cell

                    // provide available output to a cell connections.
                    int outputCell = outputs.Length == 1 ? 0 : RandomNumberGenerator.GetInt32(0, outputs.Length);

                    BaseCell second3 = outputs[outputCell];
                    if (first == second3) continue;

                    if (second3.SupportsAdditionalConnection && !first.IsConnectedTo(second3))
                    {
                        // from input to cell
                        availableToConnect.Add(new TwoBrainCellsInOneObject(first, second3));
                    }

                    break;

                case 2: // connect two cells together (non input/output)

                    int secondCell = cells.Length == 1 ? 0 : RandomNumberGenerator.GetInt32(0, cells.Length);
                    if (firstCell == secondCell && !first.SupportsSelfConnection) continue; // prevent self connection if not supported

                    BaseCell second2 = cells[secondCell];

                    if (!first.IsConnectedTo(second2) && second2.SupportsAdditionalConnection)
                    {
                        availableToConnect.Add(new TwoBrainCellsInOneObject(first, second2));
                    }

                    break;
            }

            if (availableToConnect.Count == 0)
            {
                // stop it hanging when it cannot find one.
                if (--attempts <= 0)
                {
                    return;
                }

                continue;
            }
            else
            {
                break;
            }
        }

        // randomly choose from our list of available connections
        int connectionPair = availableToConnect.Count == 1 ? 0 : RandomNumberGenerator.GetInt32(0, availableToConnect.Count);

        // connect the chosen pair. For feedforward, cell1 -> cell2 (otherwise back propagation cannot work)
        availableToConnect[connectionPair].Cell1.ConnectTo(availableToConnect[connectionPair].Cell2, Utils.RandomFloatBetweenMinusHalfToPlusHalf());
    }

    /// <summary>
    /// Attaches a cell to itself (self-connection).
    /// </summary>
    internal void AddSelfConnection()
    {
        // indexable as array
        BaseCell[] cells = Neurons.Values.ToArray();

        // make a list of ones we can connect, that aren't already connected
        List<TwoBrainCellsInOneObject> availableToConnect = new();

        for (int firstCell = 0; firstCell < cells.Length - 1; firstCell++)// -1, is because inner is +1, and it will break boundary without
        {
            BaseCell first = cells[firstCell];

            if (!first.IsConnectedTo(first) && first.SupportsAdditionalConnection && first.SupportsSelfConnection)
            {
                // from input to cell
                availableToConnect.Add(new TwoBrainCellsInOneObject(first, first));
            }
        }

        if (availableToConnect.Count == 0)
        {
            // unable to connect self- cells, as no neurons have spare connection slots...
            return;
        }

        // randomly choose from our list of available connections
        int connectionPair = RandomNumberGenerator.GetInt32(0, availableToConnect.Count);

        // connect the chosen pair. For feedforward, cell1 -> cell2 (otherwise back propagation cannot work)
        availableToConnect[connectionPair].Cell1.ConnectTo(availableToConnect[connectionPair].Cell2, Utils.RandomFloatBetweenMinusHalfToPlusHalf());
    }

    /// <summary>
    /// Removes a random connection from the network.
    /// </summary>
    internal void RemoveRandomConnection(bool removeSelfConnection = false)
    {
        if (AllNetworkConnections.Values.Count == 1) return;

        // make a list of ones we can connect that aren't already connected
        List<Connection> availableToDisconnect = new();
        List<string> NeuronsArray = Neurons.Keys.ToList();

        foreach (Connection conn in AllNetworkConnections.Values)
        {
            if (!removeSelfConnection && (conn.From == conn.To)) continue; // doesn't remove self-connections
            if (removeSelfConnection && (conn.From != conn.To)) continue; // remove self-connections

            BaseCell cellFrom = Neurons[conn.From];
            if (cellFrom.Type == CellType.INPUT) continue;

            if (cellFrom.OutboundConnections.Count < 2) continue; // it will no longer be connected unless we stop it

            BaseCell cellTo = Neurons[conn.To];

            if (cellTo.Type == CellType.OUTPUT) continue;
            if (cellTo.InboundConnections.Count < 2) continue; // it will no longer be connected unless we stop it
            if (cellTo.InboundConnections.Count <= cellTo.MinimumInputs) continue; // some cell types have a minimum number of connections

            if (NeuronsArray.IndexOf(conn.From) > NeuronsArray.IndexOf(conn.To)) continue;

            availableToDisconnect.Add(conn);
        }

        if (availableToDisconnect.Count == 0)
        {
            // unable to disconnect cells, as no available connections to remove
            return;
        }

        // randomly choose from our list of available connections
        int connectionPair = RandomNumberGenerator.GetInt32(0, availableToDisconnect.Count);

        // disconnect the chosen connection
        DisconnectInboundConnection(availableToDisconnect[connectionPair]);
        DisconnectOutboundConnection(availableToDisconnect[connectionPair]);
    }

    /// <summary>
    /// Disconnects a connection outbound from another cell as long as it is not an input. 
    /// </summary>
    /// <param name="outboundConnection"></param>
    private void DisconnectInboundConnection(Connection outboundConnection)
    {
        if (Neurons[outboundConnection.To].Type == CellType.OUTPUT)
        {
            return;
        }

        foreach (Connection c in Neurons[outboundConnection.To].InboundConnections.ToArray())
        {
            if (c.From != outboundConnection.From)
            {
                continue;
            }

            if (Neurons[outboundConnection.From].Type == CellType.INPUT)
            {
                continue;
            }

            Neurons[outboundConnection.To].InboundConnections.Remove(c);
        }

        AllNetworkConnections.Remove(outboundConnection.KeyFromTo);
    }

    /// <summary>
    /// Disconnects a connection inbound to another cell as long as it is not an output.
    /// </summary>
    /// <param name="inboundConnection"></param>
    private void DisconnectOutboundConnection(Connection inboundConnection)
    {
        if (Neurons[inboundConnection.From].Type == CellType.INPUT)
        {
            return;
        }

        foreach (Connection c in Neurons[inboundConnection.From].OutboundConnections.ToArray())
        {
            if (c.To != inboundConnection.To)
            {
                continue;
            }

            if (Neurons[inboundConnection.To].Type == CellType.OUTPUT)
            {
                continue;
            }

            Neurons[inboundConnection.From].OutboundConnections.Remove(c);
        }
    }

    /// <summary>
    /// Mutates the weight of a random connection, by a random amount.
    /// </summary>
    internal void MutateWeightOfConnection(float min, float max)
    {
        if (AllNetworkConnections.Values.Count == 0)
        {
            return;
        }

        // randomly choose from our list of available connections
        int connectionPair = AllNetworkConnections.Values.Count == 1 ? 0 : RandomNumberGenerator.GetInt32(0, AllNetworkConnections.Values.Count);

        Connection connection = AllNetworkConnections.Values.ToArray()[connectionPair];
        connection.Weight += Utils.RandomPlusMinusRange(min, max);
    }

    /// <summary>
    /// Picks a random cell, and asks it to mutate its bias.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    internal void MutateBiasOnARandomCellIncludingOutput(float min, float max)
    {
        List<BaseCell> cells = Neurons.Values.ToList();

        int chosenCell = RandomNumberGenerator.GetInt32(0, cells.Count);
        cells[chosenCell].MutateBias(min, max);
    }

    /// <summary>
    /// Picks a random cell, and asks it to mutate its threshold.
    /// </summary>
    /// <param name="amount"></param>
    internal void MutateThresholdOnARandomCell(float amount = int.MinValue)
    {
        List<BaseCell> cells = Neurons.Values.ToList();

        int chosenCell = RandomNumberGenerator.GetInt32(0, cells.Count);
        cells[chosenCell].MutateThreshold(amount);
    }

    /// <summary>
    /// Picks a random cell, and asks it to mutate its activation function.
    /// </summary>
    internal void MutateActivationFunctionOnARandomCell()
    {
        List<BaseCell> cells = Neurons.Values.ToList();

        cells[RandomNumberGenerator.GetInt32(0, cells.Count)].MutateActivationFunction();
    }

    /// <summary>
    /// Swaps two cells over (bias/activation function).
    /// </summary>
    internal void SwapBiasAndActivationBetweenToRandomCells()
    {
        BaseCell[] cells = Neurons.Values.ToArray();

        // pick two random cells
        int chosenCell1 = RandomNumberGenerator.GetInt32(0, cells.Length);
        int chosenCell2 = RandomNumberGenerator.GetInt32(0, cells.Length);

        if (chosenCell1 == chosenCell2) return; // no point swapping with itself.

        // these are not swappable
        if (cells[chosenCell1].Type == CellType.IF || cells[chosenCell1].Type == CellType.TRANSISTOR ||
            cells[chosenCell2].Type == CellType.IF || cells[chosenCell2].Type == CellType.TRANSISTOR) return;

        // check min connections satisfied
        if (cells[chosenCell1].InboundConnections.Count < cells[chosenCell2].MinimumInputs ||
            cells[chosenCell2].InboundConnections.Count < cells[chosenCell1].MinimumInputs) return;

        // do the swap of bias and activation function
        (cells[chosenCell2].Bias, cells[chosenCell1].Bias) = (cells[chosenCell1].Bias, cells[chosenCell2].Bias);
        (cells[chosenCell2].ActivationFunction, cells[chosenCell1].ActivationFunction) = (cells[chosenCell1].ActivationFunction, cells[chosenCell2].ActivationFunction);
    }

    /// <summary>
    /// Dispose the numerous parts of the network.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // to be unambiguous to the garbage collector, explicitly remove objects it created
                foreach (BaseCell cell in Neurons.Values) { cell.Dispose(); }

                Neurons.Clear();

                AllNetworkConnections.Clear(); // brain cells dispose() destroy the connections.

                Inputs.Clear();
                Outputs.Clear();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Copy the lists into a more performant arrays.
    /// </summary>
    internal void Optimise()
    {
        NeuronsAsArray = Neurons.Values.ToArray();

        List<BaseCell> hiddenCells = new();
        List<BaseCell> inputCells = new();
        List<BaseCell> outputCells = new();

        for (int i = 0; i < NeuronsAsArray.Length; i++)
        {
            BaseCell cell = NeuronsAsArray[i];

            switch (cell.Type)
            {
                case CellType.INPUT:
                    inputCells.Add(cell);
                    break;
                
                case CellType.OUTPUT:
                    outputCells.Add(cell);
                    break;
                
                default:
                    hiddenCells.Add(cell);
                    break;
            }
        }

        InputAsArray = inputCells.ToArray();
        OutputAsArray = outputCells.ToArray();
        HiddenAsArray = hiddenCells.ToArray();

        foreach(Connection c in AllNetworkConnections.Values)
        {
            c.FromNeuronIndex = GetNeuronIndex(c.From);
            c.ToNeuronIndex = GetNeuronIndex(c.To);
        }
    }

    /// <summary>
    /// Returns the index of a neuron in the array, to enable access by element rather than by key.
    /// </summary>
    /// <param name="neuronId"></param>
    /// <returns></returns>
    private int GetNeuronIndex(string neuronId)
    {
        Debug.Assert(NeuronsAsArray is not null);

        for (int i = 0; i < NeuronsAsArray.Length; i++)
        {
            if (NeuronsAsArray[i].Id == neuronId)
            {
                return i;
            }
        }

        return -1;
    }

    #endregion
}