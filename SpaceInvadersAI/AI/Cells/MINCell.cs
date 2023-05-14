using SpaceInvadersAI.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// MIN => smallest of input(equivalent to both sharing ground).
/// Behaviour: Evaluate each input, and determine the smallest in "magnitude" aka ABS(). Not the smallest value, but the smallest in magnitude.
/// 
/// e.g. if 3 inputs of -1, -0.5, 0.9  then the smallest in magnitude is -0.5.
/// 
/// It isn't a simple MIN(), which for the example would be -1.
/// Purpose: To pick the input that is shouting the least (be it quiet negative or quiet positive).
/// </summary>
internal class MINCell : BaseCell
{
    //  █   █    ███    █   █
    //  ██ ██     █     █   █
    //  █ █ █     █     ██  █
    //  █ █ █     █     █ █ █
    //  █   █     █     █  ██
    //  █   █     █     █   █
    //  █   █    ███    █   █

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="bias"></param>
    /// <param name="activationFunction"></param>
    /// <param name="cellActivationThreshold"></param>
    internal MINCell(Brain brain,
                   NeuralNetwork neuralNetwork,
                   string id,
                   double bias,
                   ActivationFunction activationFunction,
                   double cellActivationThreshold) : base(brain, neuralNetwork, id, CellType.MIN, bias, activationFunction, cellActivationThreshold)
    {
        MinimumInputs = 2;
    }

    /// <summary>
    /// Returns the minimum of the inputs.
    /// </summary>
    protected override void CellFunction()
    {
        double minValue = int.MaxValue;

        // sum each input only if they are in range.
        for (int i = 0; i < InboundConnections.Count; i++)
        {
            Connection conn = InboundConnections[i];

            double val = (conn.From == conn.To ? State : Network.NeuronsAsArray[conn.FromNeuronIndex].Activation) * conn.Weight;

            if (Math.Abs(val) < Math.Abs(minValue)) minValue = val;
        }

        State = minValue + Bias;
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal override MINCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Bias, ActivationFunction, CellActivationThreshold);
    }
}