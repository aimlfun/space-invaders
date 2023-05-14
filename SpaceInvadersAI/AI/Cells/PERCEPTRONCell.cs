namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// Represents a Perceptron (formally). In reality the cell default is a perceptron.
/// </summary>
internal class PERCEPTRONCell : BaseCell
{
    //  ████    █████   ████     ███    █████   ████    █████   ████     ███    █   █
    //  █   █   █       █   █   █   █   █       █   █     █     █   █   █   █   █   █
    //  █   █   █       █   █   █       █       █   █     █     █   █   █   █   ██  █
    //  ████    ████    ████    █       ████    ████      █     ████    █   █   █ █ █
    //  █       █       █ █     █       █       █         █     █ █     █   █   █  ██
    //  █       █       █  █    █   █   █       █         █     █  █    █   █   █   █
    //  █       █████   █   █    ███    █████   █         █     █   █    ███    █   █                                                                            

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="bias"></param>
    /// <param name="activationFunction"></param>
    /// <param name="cellActivationThreshold"></param>
    internal PERCEPTRONCell(Brain brain,
                   NeuralNetwork neuralNetwork,
                   string id,
                   double bias,
                   ActivationFunction activationFunction,
                   double cellActivationThreshold) : base(brain, neuralNetwork, id, CellType.PERCEPTRON, bias, activationFunction, cellActivationThreshold)
    {
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal override PERCEPTRONCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Bias, ActivationFunction, CellActivationThreshold);
    }
}