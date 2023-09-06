using SpaceInvadersAI.AI;
using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.AI.Utilities;
using SpaceInvadersAI.Learning.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.AI;

/// <summary>
/// Implements "Brain" inheritance mechanism.
/// </summary>
internal static class Inheritance
{
    //   ███    █   █   █   █   █████   ████     ███    █████     █     █   █    ███    █████
    //    █     █   █   █   █   █       █   █     █       █      █ █    █   █   █   █   █
    //    █     ██  █   █   █   █       █   █     █       █     █   █   ██  █   █       █
    //    █     █ █ █   █████   ████    ████      █       █     █   █   █ █ █   █       ████
    //    █     █  ██   █   █   █       █ █       █       █     █████   █  ██   █       █
    //    █     █   █   █   █   █       █  █      █       █     █   █   █   █   █   █   █
    //   ███    █   █   █   █   █████   █   █    ███      █     █   █   █   █    ███    █████

    /// <summary>
    /// It takes two brains and returns a new brain that is a cross over of the two brains.
    /// </summary>
    /// <param name="parentBrain1"></param>
    /// <param name="parentBrain2"></param>
    /// <param name="equal"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal static Brain GenesCrossOver(Brain parentBrain1, Brain parentBrain2, bool equal)
    {
        if (parentBrain1.Networks.Count != parentBrain2.Networks.Count)
        {
            throw new ApplicationException("Cross over isn't possible if the number of networks are different.");
        }

        // initialise a new offspring brain
        Brain offspringBrain = new( 
            brainName: UniqueBrainID.GetNextBrainId(),
            PersistentConfig.Settings.AllowedActivationFunctions,
            inputParameters: parentBrain1.BrainInputs.Keys.ToArray(),
            outputParameters: parentBrain1.BrainOutputs.Keys.ToArray())
        {
            AllowedCellTypes = parentBrain1.AllowedCellTypes,

            lineage = parentBrain1.Name + "|" + parentBrain2.Name
        };

        NeuralNetwork[] networks1 = parentBrain1.Networks.Values.ToArray();
        NeuralNetwork[] networks2 = parentBrain2.Networks.Values.ToArray();

        // cross over the network(s) in both brains
        for (int i = 0; i < parentBrain1.Networks.Count; i++)
        {
            NeuralNetwork network1 = networks1[i];
            NeuralNetwork network2 = networks2[i];

            BrainGenesCrossOver(offspringBrain, network1, network2, equal);
        }

        return offspringBrain;
    }

    /// <summary>
    /// Perform a cross over of the two networks and add the result to the offspring brain.
    /// </summary>
    /// <param name="offspringBrain"></param>
    /// <param name="network1"></param>
    /// <param name="network2"></param>
    /// <param name="equal"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static Brain BrainGenesCrossOver(Brain offspringBrain, NeuralNetwork network1, NeuralNetwork network2, bool equal)
    {
        // the brains can be different, but the networks cannot be in input/output.
        if (network1.Inputs.Count != network2.Inputs.Count ||
            network1.Outputs.Count != network2.Outputs.Count)
        {
            throw new ApplicationException("Cross over isn't possible if the 2 networks have differing number of input and outputs.");
        }

        // I have not implemented this yet, but seems a good idea
        // "https://en.wikipedia.org/wiki/Crossover_(genetic_algorithm)"

        // network has no input/outputs
        NeuralNetwork offspringNetwork = offspringBrain.AddEmptyNetwork("network");
        offspringNetwork.AllowedActivationFunctions = network1.AllowedActivationFunctions;

        // get fitness then we copy from random parents
        float fitness1 = network1.BrainItIsPartOf.Fitness;
        float fitness2 = network2.BrainItIsPartOf.Fitness;

        // 3 modes, equal (or both have equal fitness) we take ALL the neurons if same size, or random between smallest and largest if different size
        int neuronsToCopy;

        BaseCell[] network1CellsArray = network1.Neurons.Values.ToArray();
        BaseCell[] network2CellsArray = network2.Neurons.Values.ToArray();

        BaseCell[] oneToWalkDown = network1CellsArray;
        NeuralNetwork toProbe = network1;

        if (equal || fitness1 == fitness2)
        {
            int max = Math.Max(network1.Neurons.Count, network2.Neurons.Count);
            int min = Math.Min(network1.Neurons.Count, network2.Neurons.Count);

            neuronsToCopy = min == max ? min : RandomNumberGenerator.GetInt32(min, max);

            if (network2.Neurons.Count > network1.Neurons.Count)
            {
                toProbe = network2;
                oneToWalkDown = network2CellsArray;
            }
        }
        else
        {
            // neurons of the fittest network
            if (fitness1 > fitness2)
            {
                neuronsToCopy = network1.Neurons.Count;
            }
            else
            {
                neuronsToCopy = network2.Neurons.Count;
                toProbe = network2;
                oneToWalkDown = network2CellsArray;
            }
        }

        // copy the common neurons, and know how many "non" there are 
        neuronsToCopy -= CopyINPUTOUTPUTCellsPlusCommonDNAToOffspring(offspringNetwork, neuronsToCopy, oneToWalkDown, toProbe);

        CopyNonCommonDNA(offspringNetwork, neuronsToCopy, oneToWalkDown, toProbe);

        CopyConnectionsCommonToBoth(network1, network2, offspringNetwork);

        if (fitness1 > fitness2) CopyConnectionsFromNetworkToOffspring(network1, offspringNetwork);

        if (fitness2 > fitness1) CopyConnectionsFromNetworkToOffspring(network2, offspringNetwork);

        return offspringBrain;
    }

    /// <summary>
    /// Copy non common DNA between the two networks, picking from either at random.
    /// </summary>
    /// <param name="offspringNetwork"></param>
    /// <param name="neuronsToCopy"></param>
    /// <param name="oneToWalkDown"></param>
    /// <param name="toProbe"></param>
    private static void CopyNonCommonDNA(NeuralNetwork offspringNetwork, int neuronsToCopy, BaseCell[] oneToWalkDown, NeuralNetwork toProbe)
    {
        // Copy a brain cells at random from either the parent to the offspring.
        for (int i = 0; i < neuronsToCopy; i++)
        {
            // already added (common to both)?
            if (offspringNetwork.Neurons.ContainsKey(oneToWalkDown[i].Id)) continue;

            BaseCell cellToClone;

            // this neuron is in both and DNA appears the same? NO
            if (!toProbe.Neurons.ContainsKey(oneToWalkDown[i].Id))
            {
                // randomly choose between networks
                if (Utils.FiftyPercentChanceAtRandom())
                    cellToClone = oneToWalkDown[i];
                else
                    cellToClone = toProbe.Neurons[oneToWalkDown[i].Id]; //take the other.
            }
            else
            {
                cellToClone = oneToWalkDown[i];
            }

            // we don't copy input and output cells
            if (cellToClone.Type == CellType.INPUT || cellToClone.Type == CellType.OUTPUT) continue;

            if (offspringNetwork.Neurons.ContainsKey(cellToClone.Id)) continue;
            BaseCell newCell = cellToClone.Clone(offspringNetwork.BrainItIsPartOf, offspringNetwork);

            offspringNetwork.Neurons.Add(newCell.Id, newCell);
        }
    }

    /// <summary>
    /// Copy the common DNA between the two networks.
    /// </summary>
    /// <param name="offspringNetwork"></param>
    /// <param name="neuronsToCopy"></param>
    /// <param name="oneToWalkDown"></param>
    /// <param name="toProbe"></param>
    private static int CopyINPUTOUTPUTCellsPlusCommonDNAToOffspring(NeuralNetwork offspringNetwork, int neuronsToCopy, BaseCell[] oneToWalkDown, NeuralNetwork toProbe)
    {
        int copied = 0;

        // COPY INPUT AND OUTPUT CELLS

        // Copy a brain cells at random from either the parent to the offspring. ALWAYS copy INPUT and OUTPUT
        for (int i = 0; i < oneToWalkDown.Length; i++)
        {
            // not INPUT/OUTPUT, skip to next
            if (oneToWalkDown[i].Type != CellType.INPUT && oneToWalkDown[i].Type != CellType.OUTPUT) continue;

            BaseCell INPUTorOUTPUTtoClone;

            string idOfNeuron = oneToWalkDown[i].Id;

            // this neuron is in both and DNA appears the same
            if (toProbe.Neurons.ContainsKey(idOfNeuron))
            {
                // randomly choose between networks
                if (Utils.FiftyPercentChanceAtRandom())
                    INPUTorOUTPUTtoClone = oneToWalkDown[i];
                else
                    INPUTorOUTPUTtoClone = toProbe.Neurons[idOfNeuron]; //take the other.
            }
            else
            {
                INPUTorOUTPUTtoClone = oneToWalkDown[i];
            }

            BaseCell newCell;

            if (INPUTorOUTPUTtoClone.Type == CellType.INPUT)
            {
                newCell = ((InputCell)INPUTorOUTPUTtoClone).Clone(offspringNetwork.BrainItIsPartOf, offspringNetwork);
                offspringNetwork.Inputs.Add(newCell.Id, (InputCell)newCell);
            }
            else
            {
                Debug.Assert(INPUTorOUTPUTtoClone.Type == CellType.OUTPUT);
                newCell = ((OutputCell)INPUTorOUTPUTtoClone).Clone(offspringNetwork.BrainItIsPartOf, offspringNetwork);
                offspringNetwork.Outputs.Add(newCell.Id, (OutputCell)newCell);
            }

            // create an exact "clone"
            offspringNetwork.Neurons.Add(newCell.Id, newCell);

            ++copied;
        }

        // Copy brain cells at random from either the parent to the offspring.
        for (int i = 0; i < oneToWalkDown.Length; i++)
        {
            if (neuronsToCopy <= copied) break; // sometimes we copy less

            // exclude input output, already added
            if (oneToWalkDown[i].Type == CellType.INPUT || oneToWalkDown[i].Type == CellType.OUTPUT) continue;

            BaseCell cellToClone;
            string idOfNeuron = oneToWalkDown[i].Id;

            // this neuron is in both and DNA appears the same
            if (!toProbe.Neurons.ContainsKey(idOfNeuron) || toProbe.Neurons[idOfNeuron].DNA != oneToWalkDown[i].DNA)
            {
                continue; // not same DNA or not present in BOTH parents
            }

            // randomly choose between networks
            if (Utils.FiftyPercentChanceAtRandom())
                cellToClone = oneToWalkDown[i];
            else
                cellToClone = toProbe.Neurons[idOfNeuron]; //take the other.

            BaseCell newCell = cellToClone.Clone(offspringNetwork.BrainItIsPartOf, offspringNetwork);

            offspringNetwork.Neurons.Add(newCell.Id, newCell);

            ++copied;
        }

        return copied;
    }

    /// <summary>
    /// Copy from source network to offspring.
    /// </summary>
    /// <param name="network"></param>
    /// <param name="offspringNetwork"></param>
    private static void CopyConnectionsFromNetworkToOffspring(NeuralNetwork sourceNetwork, NeuralNetwork offspringNetwork)
    {
        // copy common connections (in both), take the weight from ONE of them at random (as they may differ)
        // in theory this includes input and output
        foreach (string conn in sourceNetwork.AllNetworkConnections.Keys)
        {
            // don't add a connection that is already present
            if (offspringNetwork.AllNetworkConnections.ContainsKey(conn)) continue;

            // convert connection into "from" and "to"
            Connection.GetIDsFromKey(conn, out string fromId, out string toId);

            // if offspring doesn't have either, we cannot connect it.
            if (!offspringNetwork.Neurons.ContainsKey(fromId) || !offspringNetwork.Neurons.ContainsKey(toId)) continue;

            BaseCell? cellFrom = offspringNetwork.Neurons[fromId];
            BaseCell? cellTo = offspringNetwork.Neurons[toId];

            if (cellFrom is null || cellTo is null) Debugger.Break(); // one of them doesn't exist, yet we checked above??!!

            // in network 1, not network 2 and network 1 is fitter, add it
            offspringNetwork.Connect(fromId, toId, sourceNetwork.AllNetworkConnections[conn].Weight);
        }
    }

    /// <summary>
    /// Copy connections common to both networks.
    /// </summary>
    /// <param name="network1"></param>
    /// <param name="network2"></param>
    /// <param name="offspringNetwork"></param>
    private static void CopyConnectionsCommonToBoth(NeuralNetwork network1, NeuralNetwork network2, NeuralNetwork offspringNetwork)
    {
        // copy common connections (in both), take the weight from ONE of them at random (as they may differ)
        // in theory this includes input and output
        foreach (string conn in network1.AllNetworkConnections.Keys)
        {
            Connection.GetIDsFromKey(conn, out string fromId, out string toId);

            // don't add a connection that is already present
            if (offspringNetwork.NeuronsAreConnected(fromId, toId)) continue;

            // ensure both endpoints are present
            if (!offspringNetwork.Neurons.ContainsKey(fromId) || !offspringNetwork.Neurons.ContainsKey(toId)) continue;

            BaseCell? cellFrom = offspringNetwork.Neurons[fromId];
            BaseCell? cellTo = offspringNetwork.Neurons[toId];

            if (cellFrom is null || cellTo is null) Debugger.Break(); // one of them doesn't exist.

            // we know the 2 are connected in BOTH, so pick a weight at random from the two
            if (network2.NeuronsAreConnected(fromId, toId))
            {
                offspringNetwork.Connect(fromId,
                                         toId,
                                         Utils.FiftyPercentChanceAtRandom() ?
                                            network1.AllNetworkConnections[conn].Weight : // weight comes from either at random.
                                            network2.AllNetworkConnections[conn].Weight);
            }
        }
    }
}