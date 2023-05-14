namespace SpaceInvadersAI.AI.Cells;

internal class TRANSISTORCell : BaseCell
{
    //  █████   ████      █     █   █    ███     ███     ███    █████    ███    ████
    //    █     █   █    █ █    █   █   █   █     █     █   █     █     █   █   █   █
    //    █     █   █   █   █   ██  █   █         █     █         █     █   █   █   █
    //    █     ████    █   █   █ █ █    ███      █      ███      █     █   █   ████
    //    █     █ █     █████   █  ██       █     █         █     █     █   █   █ █
    //    █     █  █    █   █   █   █   █   █     █     █   █     █     █   █   █  █
    //    █     █   █   █   █   █   █    ███     ███     ███      █      ███    █   █

    /// <summary>
    /// This is the base connection of the transistor.
    /// </summary>
    internal const int baseConn = 0;

    /// <summary>
    /// This is the collector connection of the transistor.
    /// </summary>
    internal const int collectorConn = 1;

    // base 
    // collector
    // emitter

    // collector set to 1, and voltage off the emitter, acts as amplifier. But a connection can do that.
    // behaviour of base < 0?

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="bias"></param>
    /// <param name="activationFunction"></param>
    /// <param name="cellActivationThreshold"></param>
    internal TRANSISTORCell(Brain brain,
                   NeuralNetwork neuralNetwork,
                   string id,
                   double bias,
                   ActivationFunction activationFunction,
                   double cellActivationThreshold) : base(brain, neuralNetwork, id, CellType.TRANSISTOR, bias, activationFunction, cellActivationThreshold)
    {
        MinimumInputs = 2;
    }

    /// <summary>
    /// Output can be input, as long as it isn't fed into the base.
    /// </summary>
    internal override bool SupportsSelfConnection
    {
        get { return InboundConnections.Count > 0; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="Exception"></exception>
    protected override void CellFunction()
    {
        Connection[] baseAndCollector = InboundConnections.ToArray();
        
        double baseValue = Network.NeuronsAsArray[baseAndCollector[baseConn].FromNeuronIndex].Activation * baseAndCollector[baseConn].Weight;

        // insufficient input on the base returns 0
        if (baseValue < Bias)
        {
            State = 0;
        }
        else
        {
            // preserve state by default, so self-connections work
            double newState = Bias;

            // AND the inputs, working out average.
            for (int i = 0; i < InboundConnections.Count; i++)
            {
                Connection conn = InboundConnections[i];

                if (conn == baseAndCollector[baseConn]) continue; // don't include the base

                // if we used this.State the AND would stick at 0, regardless of other inputs, so we min it at "Bias"
                newState += (conn.From == conn.To ? State : Network.NeuronsAsArray[conn.FromNeuronIndex].Activation) * conn.Weight;
            }

            newState /= InboundConnections.Count - 1; // -1 is because 1 is the base, we are dividing by non bases

            State = newState;

            // how much the base exceeds the bias, we scale the output
            double gain = baseValue / Bias;

            State *= gain;
        }
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal override TRANSISTORCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Bias, ActivationFunction, CellActivationThreshold);
    }
}