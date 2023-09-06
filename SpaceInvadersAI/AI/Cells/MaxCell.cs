using SpaceInvadersAI.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// MAX => largest of input (equivalent to both sharing ground).
/// Behaviour: Evaluate each input, and determine the largest in "magnitude" aka ABS(). Not the largest value, but the largest in magnitude.
/// 
/// E.g. if 3 inputs of -1, -0.5, 0.9  then the largest in magnitude is -1.
/// 
/// It isn't a simple MAX(), which for the example would be 0.9.
/// Purpose: To pick the input that is shouting loudest (be it loud negative or loud positive).
/// </summary>
internal class MaxCell : BaseCell
{
    //  █   █     █     █   █
    //  ██ ██    █ █    █   █
    //  █ █ █   █   █    █ █
    //  █ █ █   █   █     █
    //  █   █   █████    █ █
    //  █   █   █   █   █   █
    //  █   █   █   █   █   █          

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="bias"></param>
    /// <param name="activationFunction"></param>
    /// <param name="cellActivationThreshold"></param>
    internal MaxCell(Brain brain,
                   NeuralNetwork neuralNetwork,
                   string id,
                   double bias,
                   ActivationFunction activationFunction,
                   double cellActivationThreshold) : base(brain, neuralNetwork, id, CellType.MAX, bias, activationFunction, cellActivationThreshold)
    {
        MinimumInputs = 2;
    }

    /// <summary>
    /// Returns "0" if no InboundConnections (not "Bias").
    /// </summary>
    protected override void CellFunction()
    {
        double maxValue = 0;

        Debug.Assert(Network.NeuronsAsArray is not null);

        // sum each input only if they are in range.
        for (int i = 0; i < InboundConnections.Count; i++)
        {
            Connection conn = InboundConnections[i];

            double val = (conn.From == conn.To ? State : Network.NeuronsAsArray[conn.FromNeuronIndex].Activation) * conn.Weight;

            if (Math.Abs(val) > Math.Abs(maxValue)) maxValue = val;
        }

        State = maxValue + Bias;
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal override MaxCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Bias, ActivationFunction, CellActivationThreshold);
    }
}