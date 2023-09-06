using SpaceInvadersAI.AI.Cells;
using SpaceInvadersAI.AI.ExternalInterface;
using SpaceInvadersAI.AI.Utilities;
using System.Security.Cryptography;

namespace SpaceInvadersAI.AI
{
    /// <summary>
    /// A class that creates the networks using different architectures.
    /// </summary>
    internal static class Architecture
    {
        //    █     ████     ███    █   █    ███    █████   █████    ███    █████   █   █   ████    █████
        //   █ █    █   █   █   █   █   █     █       █     █       █   █     █     █   █   █   █   █
        //  █   █   █   █   █       █   █     █       █     █       █         █     █   █   █   █   █
        //  █   █   ████    █       █████     █       █     ████    █         █     █   █   ████    ████
        //  █████   █ █     █       █   █     █       █     █       █         █     █   █   █ █     █
        //  █   █   █  █    █   █   █   █     █       █     █       █   █     █     █   █   █  █    █
        //  █   █   █   █    ███    █   █    ███      █     █████    ███      █      ███    █   █   █████                                                              

        /// <summary>
        /// Adds a layer of neurons connected to the every input, every output.
        /// </summary>
        /// <param name="neuralNetwork"></param>
        /// <param name="inputsToConnectTo">If no elements, connects to ALL inputs.</param>
        /// <param name="layers">Definition of hidden layer.</param>
        /// <param name="outputsToConnectTo">If no elements, connects to ALL outputs.</param>
        /// <param name="activationFunction">The type of activation function for the perceptrons (null=TANH).</param>
        /// <returns></returns>
        internal static NeuralNetwork CreatePerceptronNetwork(
            NeuralNetwork neuralNetwork,
            string[] inputsToConnectTo,
            int[] layers,
            string[] outputsToConnectTo,
            ActivationFunction? activationFunction = null)
        {
            Dictionary<int, List<BaseCell>> perceptrons = new();

            for (int layer = 0; layer < layers.Length; layer++)
            {
                perceptrons.Add(layer, new());

                List<BaseCell> cells = new();

                // layers comprise of X neurons, this loops to create all in the layer
                for (int neuronIndex = 0; neuronIndex < layers[layer]; neuronIndex++)
                {
                    ActivationFunction cellActivationFunction;

                    if (activationFunction is null)
                    {
                        if (neuralNetwork.AllowedActivationFunctions == null) // pick a safe bet, as we weren't given a list to choose from
                        {
                            cellActivationFunction = ActivationFunction.TanH;
                        }
                        else
                        {
                            // pick activation function at random
                            cellActivationFunction = neuralNetwork.AllowedActivationFunctions[RandomNumberGenerator.GetInt32(0, neuralNetwork.AllowedActivationFunctions.Length)];
                        }
                    }
                    else
                    {
                        cellActivationFunction = (ActivationFunction)activationFunction;
                    }

                    BaseCell cell = neuralNetwork.AddCell($"{neuralNetwork.Id}-hidden-{layer}.{neuronIndex}", CellType.PERCEPTRON, cellActivationFunction);

                    cells.Add(cell);

                    // attach this first layer to the inputs
                    if (layer == 0)
                    {
                        foreach (InputCell bi in neuralNetwork.Inputs.Values)
                        {
                            if (inputsToConnectTo.Length == 0 || inputsToConnectTo.Contains(bi.Id))
                            {
                                bi.ConnectTo(cell, Utils.RandomFloatBetweenMinusHalfToPlusHalf());
                            }
                        }
                    }
                    else
                    {
                        // not first layer, connect to all cells in previous layer
                        foreach (BaseCell cellPreviousLayer in perceptrons[layer - 1])
                        {
                            cellPreviousLayer.ConnectTo(cell, Utils.RandomFloatBetweenMinusHalfToPlusHalf());
                        }
                    }
                }

                // SonarQube moans about the line below. But at the start of the loop we add it to the dictionary, and cells is also initialised.
                perceptrons[layer] = cells;
            }

            // add all the connections from the last hidden layer to the outputs
            foreach (OutputCell bo in neuralNetwork.Outputs.Values)
            {
                if (outputsToConnectTo.Length > 0 && !outputsToConnectTo.Contains(bo.Id))
                {
                    continue;
                }

                foreach (BaseCell cellInPreviousLayer in perceptrons[layers.Length - 1])
                {
                    cellInPreviousLayer.ConnectTo(bo, Utils.RandomFloatBetweenMinusHalfToPlusHalf());
                }
            }

            return neuralNetwork;
        }

        /// <summary>
        /// Returns a random network (random input, output, nodes, connections etc.)
        /// </summary>
        /// <param name="neuralNetwork">Which "neural network" random cells are attached.</param>
        /// <param name="inputsToConnectTo">Which "brain" inputs to connect to. Default=all.</param>
        /// <param name="outputsToConnectTo">Which "brain" outputs to connect to. Default=all.</param>
        /// <param name="hiddenNeuronCount">How many hidden neurons.</param>
        /// <param name="connectionCount">How many connections to add.</param>
        /// <param name="selfConnectionCount">How many self-connections to ass.</param>
        /// <param name="randomiseWhichInputsAreIncluded">true - a random number of inputs are connected | false - all inputs are connected.</param>
        /// <param name="randomiseWhichOutputsAreIncluded">true - a random number of outputs are connected | false - all outputs are connected.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal static NeuralNetwork CreateRandomNetwork(
            NeuralNetwork neuralNetwork,
            string[] inputsToConnectTo,
            string[] outputsToConnectTo,
            int hiddenNeuronCount,
            int connectionCount = -1,
            int selfConnectionCount = 0,
            bool randomiseWhichInputsAreIncluded = false,
            bool randomiseWhichOutputsAreIncluded = false)
        {
            // 0 signifies ALL inputs
            if (inputsToConnectTo.Length == 0) inputsToConnectTo = neuralNetwork.Inputs.Keys.ToArray();

            // 0 signifies ALL outputs
            if (outputsToConnectTo.Length == 0) outputsToConnectTo = neuralNetwork.Outputs.Keys.ToArray();

            // pick a random number of inputs 
            int inputCount;
            if (randomiseWhichInputsAreIncluded)
            {
                inputCount = inputsToConnectTo.Length == 1 ? 1 : RandomNumberGenerator.GetInt32(1, inputsToConnectTo.Length);
            }
            else
            {
                inputCount = inputsToConnectTo.Length == 1 ? 1 : inputsToConnectTo.Length;
            }

            // pick a random number of inputs
            int outputCount;
            if (randomiseWhichOutputsAreIncluded)
            {
                outputCount = outputsToConnectTo.Length == 1 ? 1 : RandomNumberGenerator.GetInt32(1, outputsToConnectTo.Length);
            }
            else
            {
                outputCount = outputsToConnectTo.Length == 1 ? 1 : outputsToConnectTo.Length;
            }

            // detect if too few hidden neurons
            if (hiddenNeuronCount < 1) throw new ArgumentOutOfRangeException(nameof(hiddenNeuronCount), " must be more than 0, otherwise there is nothing to create");

            // default with appropriate number
            if (connectionCount == -1) connectionCount = hiddenNeuronCount * 2 + inputCount + outputCount;

            // detect to few connections 
            if (connectionCount < hiddenNeuronCount + inputCount + outputCount) throw new ArgumentOutOfRangeException(nameof(connectionCount), " insufficient to connect the nodes + inputs");


            for (int i = 0; i < hiddenNeuronCount; i++)
            {
                neuralNetwork.Mutate(MutationMethod.AddCell);
            }

            for (int i = 0; i < connectionCount - hiddenNeuronCount; i++)
            {
                neuralNetwork.Mutate(MutationMethod.AddConnection);
            }

            for (int i = 0; i < selfConnectionCount; i++)
            {
                neuralNetwork.Mutate(MutationMethod.AddSelfConnection);
            }

            BaseCell[] cells = neuralNetwork.Neurons.Values.ToArray();

            AddMINIMUMConnectionsRequiredToRandomNetwork(ref connectionCount, ref selfConnectionCount, cells);

            UseUpAnyRemainingConnections(neuralNetwork, ref connectionCount, ref selfConnectionCount, cells);

            return neuralNetwork;
        }

        /// <summary>
        /// Adds a number of hidden random neurons, and returns an array of them.
        /// </summary>
        /// <param name="neuralNetwork"></param>
        /// <param name="hiddenNeuronCount"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        internal static BaseCell[] AddCELLsToRandomNetwork(NeuralNetwork neuralNetwork, int hiddenNeuronCount, ref CellType[]? types)
        {
            // default with all types
            types ??= new CellType[] {
                    CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON, CellType.PERCEPTRON,
                    CellType.TRANSISTOR, CellType.TRANSISTOR, CellType.TRANSISTOR,
                    CellType.AND, CellType.AND,
                    CellType.MAX, CellType.MIN,
                    CellType.IF, CellType.IF };

            // create the desired neurons, of random type
            for (int i = 0; i < hiddenNeuronCount; i++)
            {
                _ = neuralNetwork.AddCell(
                    id: null,
                    cellType: types[RandomNumberGenerator.GetInt32(0, types.Length)],
                    bias: Utils.RandomFloatBetweenMinusHalfToPlusHalf(),
                    activationFunction: BaseCell.RandomActivationFunction(neuralNetwork),
                    cellActivationThreshold: RarelyModifiedSettings.DefaultCellActivationThreshold);
            }

            return neuralNetwork.Neurons.Values.ToArray();
        }

        /// <summary>
        /// Adds connections to a random network.
        /// </summary>
        /// <param name="neuralNetwork"></param>
        /// <param name="connectionCount"></param>
        /// <param name="cells"></param>
        /// 
        internal static void AddRandomConnectionsToNetwork(NeuralNetwork neuralNetwork, ref int connectionCount, BaseCell[] cells)
        {
            // attach each cell to random other ones
            for (int thisCell = 0; thisCell < cells.Length; thisCell++)
            {
                neuralNetwork.AddRandomConnection();

                --connectionCount;
            }
        }

        /// <summary>
        /// Use up remaining any connections, connecting at random.
        /// </summary>
        /// <param name="neuralNetwork"></param>
        /// <param name="connectionCount"></param>
        /// <param name="selfConnectionCount"></param>
        /// <param name="cells"></param>
        private static void UseUpAnyRemainingConnections(NeuralNetwork neuralNetwork, ref int connectionCount, ref int selfConnectionCount, BaseCell[] cells)
        {
            // use up any remaining connections
            while (connectionCount > 0)
            {
                if (selfConnectionCount == 0)
                {
                    neuralNetwork.AddRandomConnection();
                    --connectionCount;
                    continue;
                }

                // pick 2 random cells.
                int thisCell = RandomNumberGenerator.GetInt32(0, cells.Length);
                int targetCell = thisCell;

                // run out of self connections, or destination cell does not support it?
                if (selfConnectionCount == 0 || !cells[targetCell].SupportsSelfConnection) continue;

                --selfConnectionCount; // track, so we limit self connections

                // does this connection already exist
                if (cells[targetCell].IsConnectedTo(cells[thisCell])) continue;

                // add the connection
                cells[targetCell].ConnectTo(cells[thisCell], Utils.RandomFloatBetweenMinusHalfToPlusHalf());

                --connectionCount;
            }
        }

        /// <summary>
        /// Ensures cells have the minimum number of connections.
        /// </summary>
        /// <param name="connectionCount"></param>
        /// <param name="selfConnectionCount"></param>
        /// <param name="cells"></param>
        private static void AddMINIMUMConnectionsRequiredToRandomNetwork(ref int connectionCount, ref int selfConnectionCount, BaseCell[] cells)
        {
            // fix up minimum required (IF, TRANSISTOR etc)
            for (int thisCell = 0; thisCell < cells.Length; thisCell++)
            {
                while (cells[thisCell].InboundConnections.Count < cells[thisCell].MinimumInputs)
                {
                    int targetCell = RandomNumberGenerator.GetInt32(0, cells.Length);

                    // limit self connections 
                    if (thisCell == targetCell)
                    {
                        if (selfConnectionCount == 0 || !cells[targetCell].SupportsSelfConnection) continue;

                        --selfConnectionCount;
                    }

                    // does this connection already exist?
                    if (!cells[targetCell].IsConnectedTo(cells[thisCell]))
                    {
                        cells[targetCell].ConnectTo(cells[thisCell], Utils.RandomFloatBetweenMinusHalfToPlusHalf());

                        --connectionCount;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

    }
}