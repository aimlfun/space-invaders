using SpaceInvadersAI.AI;
using SpaceInvadersAI.AI.ExternalInterface;
using SpaceInvadersAI.AI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// Represents an input to the neural network, not brain.
/// That is an important distinction, as a brain can have multiple neural networks sharing the same input.
/// </summary>
internal class INPUTCell : BaseCell
{
    //   ███    █   █   ████    █   █   █████
    //    █     █   █   █   █   █   █     █
    //    █     ██  █   █   █   █   █     █
    //    █     █ █ █   ████    █   █     █
    //    █     █  ██   █       █   █     █
    //    █     █   █   █       █   █     █
    //   ███    █   █   █        ███      █
    
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="bias"></param>
    /// <param name="cellActivationThreshold"></param>
    /// <param name="activationFunction"></param>
    internal INPUTCell(Brain brain,
                   NeuralNetwork neuralNetwork,
                   string id,
                   double bias,
                   ActivationFunction activationFunction,
                   double cellActivationThreshold)
        : base(brain, neuralNetwork, id, CellType.INPUT, bias, activationFunction, cellActivationThreshold)
    {
        MaximumInputs = 0; // no inputs connection to this cell
        MinimumInputs = 0; // no inputs connection to this cell
    }

    /// <summary>
    /// This is the function the cell performs. In the case of the input cell, it takes the "input" value and applies the cell function to it.
    /// </summary>
    protected override void CellFunction()
    {
        // passthrough.
        State = Brain.BrainInputs[Id].Activation;

        ResultOfLastActivation = ActivationUtils.Activate(ActivationFunction, State);
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal override INPUTCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Bias, ActivationFunction, CellActivationThreshold);
    }
}