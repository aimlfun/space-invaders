using SpaceInvadersAI.AI;
using SpaceInvadersAI.AI.ExternalInterface;
using SpaceInvadersAI.AI.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// Represents the output gateway between neural network and the brain it is part of.
/// Behaves as a Perceptron.
/// 
/// Because the brain can be constructed out of multiple neural networks, the output cell is not the same as the brain output. 
/// The could be 4 brain outputs, with 2 coming from one neural network, and 2 coming from another.
/// </summary>
internal class OutputCell : BaseCell
{
    //   ███    █   █   █████   ████    █   █   █████
    //  █   █   █   █     █     █   █   █   █     █
    //  █   █   █   █     █     █   █   █   █     █
    //  █   █   █   █     █     ████    █   █     █
    //  █   █   █   █     █     █       █   █     █
    //  █   █   █   █     █     █       █   █     █
    //   ███     ███      █     █        ███      █

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="bias"></param>
    /// <param name="activationFunction"></param>
    /// <param name="cellActivationThreshold"></param>
    internal OutputCell(Brain brain,
                   NeuralNetwork neuralNetwork,
                   string id,
                   double bias,
                   ActivationFunction activationFunction,
                   double cellActivationThreshold) : base(brain, neuralNetwork, id, CellType.OUTPUT, bias, activationFunction, cellActivationThreshold)
    {
        MaximumOutputs = 0; // prevent connections from the output cell.
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal override OutputCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Bias, ActivationFunction, CellActivationThreshold);
    }
}