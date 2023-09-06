using SpaceInvadersAI.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI.Cells;

/// <summary>
/// AND of inputs: IF *all* inputs are greater than or equal to Bias THEN average of inputs ELSE 0.
/// </summary>
internal class AndCell : BaseCell
{
    //    █     █   █   ████
    //   █ █    █   █   █   █
    //  █   █   ██  █   █   █
    //  █   █   █ █ █   █   █
    //  █████   █  ██   █   █
    //  █   █   █   █   █   █
    //  █   █   █   █   ████

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="brain"></param>
    /// <param name="neuralNetwork"></param>
    /// <param name="id"></param>
    /// <param name="bias"></param>
    /// <param name="activationFunction"></param>
    /// <param name="cellActivationThreshold"></param>
    internal AndCell(Brain brain,
                   NeuralNetwork neuralNetwork,
                   string id,
                   double bias,
                   ActivationFunction activationFunction,
                   double cellActivationThreshold) : base(brain, neuralNetwork, id, CellType.AND, bias, activationFunction, cellActivationThreshold)
    {
        // 1 input is not an AND, so we ensure 2 or more are present
        MinimumInputs = 2;
    }

    /// <summary>
    /// Activation: If ALL inputs exceed "Bias" then State => AVG(inputs) else State => 0.
    /// </summary>
    protected override void CellFunction()
    {
        // preserve state by default, although for AND, if we take prior state when it becomes zero it will 
        // never become non zero, so "Bias" is used not state
        double newState = 0;

        Debug.Assert(Network.NeuronsAsArray is not null);

        // AND the inputs, working out average.
        for (int connectionIndex = 0; connectionIndex < InboundConnections.Count; connectionIndex++)
        {
            Connection conn = InboundConnections[connectionIndex];

            // if we used this.State the AND would stick at 0, regardless of other inputs, so we min it at "Bias"
            double val = (conn.From == conn.To ? Bias : Network.NeuronsAsArray[conn.FromNeuronIndex].Activation) * conn.Weight;

            if (val < Bias) // if any input is less than bias, we can stop, and return 0. 
            {
                newState = 0;
                break; // average will be 0 exiting loop
            }

            newState += val; // exceeds bias
        }

        // create the average of the inputs
        if (newState > 0)
        {
            newState /= InboundConnections.Count;
        }

        State = newState;
    }

    /// <summary>
    /// This clones a cell, and returns the clone.
    /// </summary>
    /// <param name="brainItIsPartOf">What brain the cell belongs to.</param>
    /// <param name="neuralNetwork">Which network the clone will be part of.</param>
    /// <returns></returns>
    internal override AndCell Clone(Brain brainItIsPartOf, NeuralNetwork neuralNetwork)
    {
        return new(brainItIsPartOf, neuralNetwork, Id, Bias, ActivationFunction, CellActivationThreshold);
    }
}