// I hate using #define, but we need PERFORMANCE, and I don't know that JIT will avoid an empty method call.
// #define DebugCell

using SpaceInvadersAI.AI;
using SpaceInvadersAI.AI.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// Represents a versatile neuron cell, that can be subclassed to change basic behaviour.
/// </summary>
internal class BaseCell : IDisposable
{
    //  ████      █      ███    █████            ███    █████   █       █
    //  █   █    █ █    █   █   █               █   █   █       █       █
    //  █   █   █   █   █       █               █       █       █       █
    //  ████    █   █    ███    ████            █       ████    █       █
    //  █   █   █████       █   █               █       █       █       █
    //  █   █   █   █   █   █   █               █   █   █       █       █
    //  ████    █   █    ███    █████            ███    █████   █████   █████

    /// <summary>
    /// Each cell has a unique id, for removal / display later
    /// </summary>
    internal string Id;

    /// <summary>
    /// The type of cell. This is where we blur neuron vs. something else. I've created some cell types
    /// with special functionality that is not achievable by a plain old neuron.
    /// </summary>
    internal CellType Type;

    /// <summary>
    /// This is the bias of the cell. It is generally added to the sum of the inputs, before the activation function is applied.
    /// For the IF, it is the threshold for the IF to be true.
    /// </summary>
    internal double Bias;

    /// <summary>
    /// Most cells are part of a network, this tracks which network. Input/outputs are part of brain, and no specific network.
    /// </summary>
    internal NeuralNetwork Network;

    /// <summary>
    /// This is the brain that the cell belongs to.
    /// </summary>
    readonly internal Brain Brain;

    /// <summary>
    /// Our "DNA" is a combination of type and activation function turned into a hash code.
    /// </summary>
    internal int DNA
    {
        get
        {
            return (Type.ToString() + "~" + Bias.ToString() + "~" + CellActivationThreshold.ToString() + "~" + string.Join("~",InboundConnections)+"~"+ string.Join("~", OutboundConnections) + "~" + ActivationFunction.ToString()).GetHashCode();
        }
    }

    /// <summary>
    /// Cells need to know the inputs feeding into them.
    /// FROM A NODE TO *THIS* NODE.
    /// </summary>
    internal List<Connection> InboundConnections = new();

    /// <summary>
    /// How many inputs the cell requires.
    /// </summary>
    internal int MinimumInputs = 1;

    /// <summary>
    /// Unlimited maximum inputs.
    /// </summary>
    internal int MaximumInputs = int.MaxValue;

    /// <summary>
    /// Cells need to know the outbound for rendering, knowing if it has maximum number of outputs etc.
    /// FROM *THIS* NODE TO ANOTHER NODE.
    /// </summary>
    internal List<Connection> OutboundConnections = new();

    /// <summary>
    /// How many outputs the cell requires.
    /// </summary>
    internal int MinimumOutputs = 1;

    /// <summary>
    /// This limits the outputs. For some cells, there is a fixed number. For example IF has 2 outputs (true/false).
    /// </summary>
    internal int MaximumOutputs = int.MaxValue;

    /// <summary>
    /// Previous activation state.
    /// </summary>
    internal double State = 0;

    /// <summary>
    /// The result of last activation. We have to store this to compute self-connecting neurons.
    /// </summary>
    protected double ResultOfLastActivation = 0;

    /// <summary>
    /// If activation does not exceed this in magnitude (Math.Abs()), the output is 0.
    /// This enables on > 0.5, off< 0.5 like behaviour (if set to 0.5).
    /// </summary>
    internal double CellActivationThreshold = RarelyModifiedSettings.DefaultCellActivationThreshold;

    /// <summary>
    /// Setter/Getter for the last activation result.
    /// </summary>
    internal virtual double Activation
    {
        get { return ResultOfLastActivation; }
        set { ResultOfLastActivation = value; }
    }

    /// <summary>
    /// This contains the activation function applied to the cell.
    /// </summary>
    internal ActivationFunction ActivationFunction;
 
    /// <summary>
    /// Provides annotation for the cell, explaining what it is doing.
    /// </summary>
    internal string ParameterAnnotation
    {
        get
        {
            return $"Bias: {Bias}\nThreshold: {CellActivationThreshold}";
        }
    }

    /// <summary>
    /// By default all cells support self-connection. Override this to change.
    /// </summary>
    internal virtual bool SupportsSelfConnection
    {
        get { return true; }
    }

    /// <summary>
    /// Constructor.
    /// Arguments: all the properties of every cell type.
    /// </summary>
    /// <param name="brainCellBelongsTo"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="typeLabel"></param>
    /// <param name="bias"></param>
    /// <param name="activationFunction"></param>
    /// <param name="cellActivationThreshold"></param>
    /// <exception cref="ArgumentNullException"></exception>
    internal BaseCell(
        Brain brainCellBelongsTo,
        NeuralNetwork neuralNetwork,
        string id,
        CellType typeLabel,
        double bias,
        ActivationFunction activationFunction,
        double cellActivationThreshold)
    {
        if (id is null) throw new ArgumentNullException(nameof(id));
        if (neuralNetwork is null) throw new ArgumentNullException(nameof(neuralNetwork));

        Brain = brainCellBelongsTo;
        Network = neuralNetwork;
        Id = id;
        Type = typeLabel;
        Bias = bias;
        ActivationFunction = activationFunction;
        CellActivationThreshold = cellActivationThreshold;
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal virtual BaseCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Type, Bias, ActivationFunction, CellActivationThreshold);
    }

    #region MUTATION
    /// <summary>
    /// Mutate the cell.
    /// </summary>
    /// <param name="mutationMethod"></param>
    internal virtual void Mutate(MutationMethod mutationMethod)
    {
        switch (mutationMethod)
        {
            case MutationMethod.ModifyBias:
                {
                    MutateBias();
                    break;
                }

            case MutationMethod.ModifyActivationFunction:
                {
                    MutateActivationFunction();
                    break;
                }

            case MutationMethod.ModifyThreshold:
                {
                    MutateThreshold();
                    break;
                }

            default:
                break; // do not add exception here, as this method can be overridden
        }
    }

    /// <summary>
    /// Mutate the bias adding +/- of the range. 
    /// IMPORTANT: example, if you put min 0.1, max 0.25 then the valid change is -.25 to -0.1 and 0.1 to 0.25.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    internal void MutateBias(float min = 0.1f, float max = 0.25f)
    {
        Bias += Utils.RandomPlusMinusRange(min, max);
    }

    /// <summary>
    /// Mutate the activation function.
    /// </summary>
    internal void MutateActivationFunction()
    {
        ActivationFunction = RandomActivationFunction(Network);
    }

    /// <summary>
    /// Mutates the threshold.
    /// If it is minimum (int.MinValue), a random value is assigned.
    /// If threshold is int.MinValue, a random value is assigned to the threshold.
    /// </summary>
    /// <param name="threshold"></param>
    internal void MutateThreshold(float threshold = int.MinValue)
    {
        // either we are assigning a random threshold, or we are modifying the existing one

        // if it is minimum, assign a random value (0..+/-1) to initialise the CellActivationThreshold
        if (CellActivationThreshold == int.MinValue)
        {
            CellActivationThreshold = RandomNumberGenerator.GetInt32(-1000000, 1000000) / 1000000;
        }

        // if threshold is int.MinValue, assign a random value (0..+/-1/10th) to the threshold input
        if (threshold == int.MinValue) threshold = RandomNumberGenerator.GetInt32(-1000000, 1000000) / 10000000; // 1/10th

        // apply the delta to the CellActivationThreshold
        CellActivationThreshold += threshold;
    }

    /// <summary>
    /// Picks a random activation function from the list of allowed activation functions.
    /// </summary>
    /// <returns></returns>
    internal static ActivationFunction RandomActivationFunction(NeuralNetwork network)
    {
        if (network.AllowedActivationFunctions is null) throw new ArgumentNullException(nameof(Network), "network cannot be null");

        return network.AllowedActivationFunctions[RandomNumberGenerator.GetInt32(0, network.AllowedActivationFunctions.Length)];
    }
    #endregion

    #region CONNECTIONS
    /// <summary>
    /// Connects a node to another. 
    /// We track using the "InboundConnections" (FROM A NODE TO THIS NODE), 
    /// and to aid certain subsequent tasks also the opposite way (FROM THIS NODE TO ANOTHER NODE).
    /// Equally important, we track all connections at the NeuralNetwork level.
    /// </summary>
    /// <param name="toCell"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    internal NeuralNetwork ConnectTo(BaseCell toCell, double weight = 1)
    {
        BaseCell fromCell = this;

        // inbound have the "to" set as the "toCell" and "from" as the "fromCell".

        // Example  A -> B

        //   fromCell = A, toCell = B
        //   toCell   Inbound  is A->B  = receiving from A
        //   fromCell Outbound is A->B  = transmitting to B

        // i.e. point inbound, from a cell to this cell
        Connection conn = new(fromCell.Id, toCell.Id, weight);

        // cells need to know their incoming connections
        toCell.InboundConnections.Add(conn);

        // also track outbound, this is the same, but note the lack of polarity switch.
        // i.e. for the fromCell, it's "from" it, and "to" another
        fromCell.OutboundConnections.Add(conn);

        // the network must track ALL connections
        Network.RegisterConnection(conn);

        return Network;
    }

    /// <summary>
    /// Connects a specific node to another by node "id".
    /// </summary>
    /// <param name="toCellId"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal NeuralNetwork ConnectTo(string toCellId, double weight = 1)
    {
        if (Network is null) throw new Exception("network undefined for cell");

        BaseCell? cellTo = Network.Neurons[toCellId];

        return cellTo is not null ? ConnectTo(cellTo, weight) : throw new ArgumentOutOfRangeException(nameof(toCellId), "cell not found");
    }

    /// <summary>
    /// Determines whether a specific neuron is connected to this cell.
    /// This is DIRECTIONAL: this = "from".
    /// </summary>
    /// <param name="toCell"></param>
    /// <returns></returns>
    internal bool IsConnectedTo(BaseCell toCell)
    {
        return Network.NeuronsAreConnected(Id, toCell.Id);
    }

    /// <summary>
    /// Determines if this cell type allow additional connections. Some like "IF" are fixed at 3.
    /// </summary>
    internal bool SupportsAdditionalConnection
    {
        get
        {
            return InboundConnections.Count < MaximumInputs;
        }
    }

    /// <summary>
    /// Determines if this cell has maximum outputs. 
    /// </summary>
    internal bool HasMaximumOutputs
    {
        get
        {
            return OutboundConnections.Count >= MaximumOutputs;
        }
    }
    #endregion

    #region ACTIVATIONS
    /// <summary>
    /// Activates the cell.
    /// </summary>
    internal virtual void Activate()
    {
        // only applies the cell function if it has the minimum number of inputs
        if (InboundConnections.Count >= MinimumInputs)
        {
            CellFunction(); // each subclass implements this to provide the "function" of the cell (behaviour).
        }

        // apply the activation function to the "result"
        ResultOfLastActivation = ActivationUtils.Activate(ActivationFunction, State);

        // unless exceeds a minimum threshold, it will be 0.
        if (ResultOfLastActivation < CellActivationThreshold) ResultOfLastActivation = 0;

#if DebugCell
        Debug.WriteLine($"{Type} ACTIVATION {Id} ResultOfLastActivation: {ResultOfLastActivation}: State: {State}");
#endif
    }

    /// <summary>
    /// Override this to provide a specific cell function.
    /// By default it behaves as a Perceptron.
    /// </summary>
    protected virtual void CellFunction()
    {
        double value = Bias;

        // sum each input only if they are in range.
        for (int i = 0; i < InboundConnections.Count; i++)
        {
            Connection conn = InboundConnections[i];

            value += (conn.From == conn.To ? State : Network.NeuronsAsArray[conn.FromNeuronIndex].Activation) * conn.Weight;
        }

        State = value;
    }
#endregion

    #region SERIALISATION
    /// <summary>
    /// Serialises the cell to text.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal virtual string Serialise()
    {
        return $"ADD CELL ID=\"{Id}\" TYPELABEL=\"{Type}\" BIAS=\"{Bias}\" ACTIVATIONTHRESHOLD=\"{CellActivationThreshold}\" ACTIVATIONFUNCTION=\"{ActivationFunction}\"";
    }

    /// <summary>
    /// Convert text to cell object.
    /// </summary>
    /// <param name="brainTheCellBelongsTo"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    internal static BaseCell Deserialise(Brain brainTheCellBelongsTo, NeuralNetwork neuralNetwork, string line)
    {
        line = line.Trim().Replace("ADD CELL ", "");
        Dictionary<string, string> tokens = Utils.RegExpParseTokens(line);

        string id = Utils.SafeGetAttribute(tokens, "ID");
        CellType typeLabel = Enum.Parse<CellType>(Utils.SafeGetAttribute(tokens, "TYPELABEL"));
        double bias = double.Parse(Utils.SafeGetAttribute(tokens, "BIAS"));
        string activationFunction = Utils.SafeGetAttribute(tokens, "ACTIVATIONFUNCTION");
        double cellActivationThreshold = double.Parse(Utils.SafeGetAttribute(tokens, "ACTIVATIONTHRESHOLD"));

        // CREATE THE CELL
        BaseCell cell = neuralNetwork.CreateNewCell(brainTheCellBelongsTo, id, typeLabel, bias, (ActivationFunction)Enum.Parse(typeof(ActivationFunction), activationFunction), cellActivationThreshold);

        // add to the network
        neuralNetwork.Neurons.Add(cell.Id, cell);

        return cell;
    }
    #endregion

    /// <summary>
    /// Provide something meaningful for debugging. 
    /// Extend this if there are other useful attributes.
    /// </summary>
    /// <returns></returns>
    public override string? ToString()
    {
        return $"{Type}: \"{Id}\" ActivationFunction: \"{ActivationFunction}\" Value: {Activation} " +
            $"Bias: {Bias} CellActivationThreshold: {CellActivationThreshold} " +
            $"MinimumInputs: {MinimumInputs} MaximumInputs: {MaximumInputs} " +
            $"MinimumOutputs: {MinimumOutputs} MaximumOutputs: {MaximumOutputs} " +
            $"InboundConnections: {InboundConnections.Count} OutboundConnections: {OutboundConnections.Count} " +
            $"=> ResultOfLastActivation: {ResultOfLastActivation} => State: {State}";
    }

    /// <summary>
    /// If we destroy the cell, the connections to it need to be disposed.
    /// </summary>
    public void Dispose()
    {
        // we are careful to remove items to ensure garbage collection occurs. You don't want a memory leak if you're running a long simulation.
        foreach (Connection conn in InboundConnections)
        {
            Network?.AllNetworkConnections.Remove(conn.KeyFromTo);
        }

        foreach (Connection conn in OutboundConnections)
        {
            Network?.AllNetworkConnections.Remove(conn.KeyFromTo);
        }

        // having dispose off them cleanly, we can stop tracking them
        InboundConnections.Clear();
        OutboundConnections.Clear();

        GC.SuppressFinalize(this);
    }
}