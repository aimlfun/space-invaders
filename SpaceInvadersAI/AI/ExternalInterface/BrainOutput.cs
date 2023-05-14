// I hate using #define, but we need PERFORMANCE, and I don't know that JIT will avoid an empty method call.
//#define DebugCell
using SpaceInvadersAI.AI;
using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.AI.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace SpaceInvadersAI.AI.ExternalInterface;

/// <summary>
/// Represents an output from the brain. Don't confuse with an OUTPUT cell. The latter is part of a specific neural network, and this is part of the brain that is shared across networks.
/// </summary>
internal class BrainOutput
{
    //  ████    ████      █      ███    █   █            ███    █   █   █████   ████    █   █   █████
    //  █   █   █   █    █ █      █     █   █           █   █   █   █     █     █   █   █   █     █
    //  █   █   █   █   █   █     █     ██  █           █   █   █   █     █     █   █   █   █     █
    //  ████    ████    █   █     █     █ █ █           █   █   █   █     █     ████    █   █     █
    //  █   █   █ █     █████     █     █  ██           █   █   █   █     █     █       █   █     █
    //  █   █   █  █    █   █     █     █   █           █   █   █   █     █     █       █   █     █
    //  ████    █   █   █   █    ███    █   █            ███     ███      █     █        ███      █
    
    /// <summary>
    /// This is the brain that the cell belongs to.
    /// </summary>
    readonly internal Brain BrainItBelongsTo;

    /// <summary>
    /// Each cell has a unique id, for removal / display later
    /// </summary>
    internal string Id;

    /// <summary>
    /// This is the minimum value the cell interface can accept
    /// </summary>
    internal double MinimumValue;

    /// <summary>
    /// This is the maximum value the cell interface can accept.
    /// </summary>
    internal double MaximumValue;

    /// <summary>
    /// The result of last activation. We have to store this to compute self-connecting neurons.
    /// </summary>
    internal double ResultOfLastActivation = 0;

    /// <summary>
    /// Previous activation state.
    /// </summary>
    internal double State = 0;

    /// <summary>
    /// This contains the activation function applied to the cell.
    /// </summary>
    internal ActivationFunction ActivationFunction;

    /// <summary>
    /// Constructor.
    /// Creates a brain "cell" for the output.
    /// </summary>
    /// <param name="brainInputBelongsTo"></param>
    /// <param name="id"></param>
    /// <param name="constrainFunction"></param>
    /// <param name="minAllowedValue"></param>
    /// <param name="maxAllowedValue"></param>
    internal BrainOutput(
        Brain brainInputBelongsTo,
        string id,
        ActivationFunction constrainFunction = ActivationFunction.TanH,
        double minAllowedValue = -2,
        double maxAllowedValue = 2)
    {
        BrainItBelongsTo = brainInputBelongsTo;
        Id = id;
        ActivationFunction = constrainFunction;
        MinimumValue = minAllowedValue;
        MaximumValue = maxAllowedValue;        
    }

    /// <summary>
    /// The Activation function an output is a sum of all neural network OUTPUT cells with the same ID, 
    /// </summary>
    internal double Activation
    {
        get
        {
            double State = 0;

            // Find outputs of the same name in the networks, and SUM() like with a perceptron. 
            // We don't add any Bias, or have a threshold, because it's shared across networks.
            foreach (NeuralNetwork neuralNetwork in BrainItBelongsTo.Networks.Values)
            {
                if (neuralNetwork.Outputs.TryGetValue(Id, out OUTPUTCell? value))
                {
                    State += value.Activation;
                }
            }

            ResultOfLastActivation = ActivationUtils.Activate(ActivationFunction, State);

            // protects against hard to detect errors where the input is scaled too large.
            if (ResultOfLastActivation < MinimumValue || ResultOfLastActivation > MaximumValue)
            {
                // The best approach I could think of was to set the cell to zero and mark the brain as dead.
                // Otherwise you end up with a brain that could be mutated/reused, when clearly it is not working.
#if DebugCell
                Debug.WriteLine($"BrainOutput activation: \"{Id}\" {ResultOfLastActivation} exceeds min/maximum value.");
#endif
                ResultOfLastActivation = 0;
                BrainItBelongsTo.IsDead = true;
            }

#if DebugCell
            Debug.WriteLine($"BrainOutput ACTIVATION Set \"{Id}\" <= {ResultOfLastActivation}");
#endif
            return ResultOfLastActivation;
        }
    }

    /// <summary>
    /// Serialises the object.
    /// </summary>
    /// <returns></returns>
    internal string Serialise()
    {
        return $"ADD BRAIN-OUT ID=\"{Id}\" ACTIVATIONFUNCTION=\"{ActivationFunction}\" MIN=\"{MinimumValue}\" MAX=\"{MaximumValue}\"";
    }

    /// <summary>
    /// Parses the "line" and returns a new BrainOutput.
    /// </summary>
    /// <param name="brainThisCellBelongsTo"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    internal static BrainOutput Deserialise(Brain brainThisCellBelongsTo, string line)
    {
        line = line.Trim().Replace("BRAIN-OUT ", "");
        Dictionary<string, string> tokens = Utils.RegExpParseTokens(line);

        string id = Utils.SafeGetAttribute(tokens, "ID");
        string activationFunction = Utils.SafeGetAttribute(tokens, "ACTIVATIONFUNCTION");
        double min = double.Parse(Utils.SafeGetAttribute(tokens, "MIN"));
        double max = double.Parse(Utils.SafeGetAttribute(tokens, "MAX"));

        BrainOutput cell = new(brainThisCellBelongsTo, id, (ActivationFunction)Enum.Parse(typeof(ActivationFunction), activationFunction), min, max);

        return cell;
    }
}