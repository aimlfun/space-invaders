using SpaceInvadersAI.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// Based on "input" picks between 2 other inputs to pass to output. i.e. Acts like IF. 
/// 3 Inputs: IF Base Input < Bias THEN Bias + (Input 1 * Weighting for Input 1) ELSE Bias + (Input 2 * Weighting for Input 2).
/// 2 Inputs: IF Base Input < Bias THEN Bias + (Input 1 * Weighting for Input 1) ELSE 0. <-- no Bias, just 0.
/// </summary>
internal class IfCell : BaseCell
{
    //   ███    █████
    //    █     █
    //    █     █
    //    █     ████
    //    █     █
    //    █     █
    //   ███    █

    const int controlConn = 0;  // condition decides whether we choose input 1 or 2
    const int branch1Conn = 1;  // input i1
    const int branch2Conn = 2;  // input i2

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="bias"></param>
    /// <param name="activationFunction"></param>
    /// <param name="cellActivationThreshold"></param>
    internal IfCell(Brain brain,
                   NeuralNetwork neuralNetwork,
                   string id,
                   double bias,
                   ActivationFunction activationFunction,
                   double cellActivationThreshold) : base(brain, neuralNetwork, id, CellType.IF, bias, activationFunction, cellActivationThreshold)
    {
        // If inputB < bias then input1 else input 2 (default 0)

        // that requires exactly 2 or 3 inputs.
        MinimumInputs = 2;
        MaximumInputs = 3;
    }


    /// <summary>
    /// If A then B else C; if B or C is used for A, then it latches as a 1 or 0 forever. So let's not allow self-connection.
    /// </summary>
    internal override bool SupportsSelfConnection
    {
        get { return false; }
    }

    /// <summary>
    /// Overrides the basic cell function to act as an IF(condition,input1,input2). 
    /// </summary>
    protected override void CellFunction()
    {
        Connection[] controlBranchConnections = InboundConnections.ToArray();

        Debug.Assert(Network.NeuronsAsArray is not null);

        double baseValue = Network.NeuronsAsArray[controlBranchConnections[controlConn].FromNeuronIndex].Activation * controlBranchConnections[controlConn].Weight;

        double weight;
        double value;

        // do the IF
        if (baseValue < Bias)
        {
            weight = controlBranchConnections[branch1Conn].Weight;
            value = Network.Neurons[controlBranchConnections[branch1Conn].From].Activation;
        }
        else
        {
            // 2 inputs, the first is the thing that decides output, it returned "false (<bias) so we provide no output whatsoever
            if (controlBranchConnections.Length == 2)
            {
                State = 0;
                return; // exit to avoid hitting the State = Bias + value * weight; line. If it hits that, it will set the state to Bias, which is wrong.
            }
            else // == 3 inputs
            {
                weight = controlBranchConnections[branch2Conn].Weight;
                value = Network.Neurons[controlBranchConnections[branch2Conn].From].Activation;
            }
        }

        State = Bias + value * weight;
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal override IfCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Bias, ActivationFunction, CellActivationThreshold);
    }
}