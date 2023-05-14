// I hate using #define, but we need PERFORMANCE, and I don't know that JIT will avoid an empty method call.
//#define DebugCell
using SpaceInvadersAI.AI.Utilities;
using System.Diagnostics;

namespace SpaceInvadersAI.AI.ExternalInterface;

/// <summary>
/// Represents an input from the brain. Don't confuse with an INPUT cell. The latter is part of a specific neural network, and this is part of the brain that is shared across networks.
/// </summary>
internal class BrainInput
{
    //  ████    ████      █      ███    █   █            ███    █   █   ████    █   █   █████
    //  █   █   █   █    █ █      █     █   █             █     █   █   █   █   █   █     █
    //  █   █   █   █   █   █     █     ██  █             █     ██  █   █   █   █   █     █
    //  ████    ████    █   █     █     █ █ █             █     █ █ █   ████    █   █     █
    //  █   █   █ █     █████     █     █  ██             █     █  ██   █       █   █     █
    //  █   █   █  █    █   █     █     █   █             █     █   █   █       █   █     █
    //  ████    █   █   █   █    ███    █   █            ███    █   █   █        ███      █

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
    protected double ResultOfLastActivation = 0;

    /// <summary>
    /// Constructor.
    /// Creates a brain "cell" for the input.
    /// </summary>
    /// <param name="brainInputBelongsTo"></param>
    /// <param name="id"></param>
    /// <param name="minAllowedValue">(optional, default: -1) defines the minimum value this input will receive.</param>
    /// <param name="maxAllowedValue">(optional, default:  1) defines the maximum value this input will receive.</param>
    internal BrainInput( Brain brainInputBelongsTo, string id, double minAllowedValue = -1, double maxAllowedValue = 1)
    {
        BrainItBelongsTo = brainInputBelongsTo;
        Id = id;
        MinimumValue = minAllowedValue;
        MaximumValue = maxAllowedValue;
    }

    /// <summary>
    /// Activation function for the brain input is always passthrough - by default you probably don't want to alter the input.
    /// However, to protect the AI from crashing (reaching infinity), it protects the input to be within a min/max range, throwing an exception if it is not. 
    /// I could simply constrain it, but doing so masks a problem with the input. Better to detect input issues and fix.
    /// </summary>
    internal double Activation
    {
        get
        {
            return ResultOfLastActivation;
        }

        set
        {
            double newValue = value; // there is intentionally no Activation function applied to the input.

            // protects against hard to detect errors where the input is scaled too large.
            if (newValue < MinimumValue || newValue > MaximumValue) throw new ArgumentOutOfRangeException(nameof(value), $"{value} exceeds min/maximum value: {newValue}.");

            ResultOfLastActivation = newValue;

#if DebugCell
            Debug.WriteLine($"BrainInput ACTIVATION Set \"{Id}\" <= {ResultOfLastActivation} from value: {value}");
#endif
        }
    }

    /// <summary>
    /// Serialises the object.
    /// </summary>
    /// <returns></returns>
    internal string Serialise()
    {
        return $"ADD BRAIN-IN ID=\"{Id}\" MIN=\"{MinimumValue}\" MAX=\"{MaximumValue}\"";
    }

    /// <summary>
    /// De-serialises a line of text into a BrainInput.
    /// </summary>
    /// <param name="brainThisCellBelongsTo"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    internal static BrainInput Deserialise(Brain brainThisCellBelongsTo, string line)
    {
        line = line.Trim().Replace("ADD BRAIN-IN ", "");
        Dictionary<string, string> tokens = Utils.RegExpParseTokens(line);

        string id = Utils.SafeGetAttribute(tokens, "ID");
        double min = double.Parse(Utils.SafeGetAttribute(tokens, "MIN"));
        double max = double.Parse(Utils.SafeGetAttribute(tokens, "MAX"));

        BrainInput cell = new(brainThisCellBelongsTo, id, min, max);

        return cell;
    }
}