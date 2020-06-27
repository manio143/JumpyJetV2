using Stride.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JumpyJetV2.AIv2
{
    [DataContract]
    public class NeuralNetworkEvolution
    {
        public static Random rng = new Random();

        public Options options = new Options();
        public Generations generations = new Generations();

        public void Restart() => generations = new Generations();

        public void AddWithScore(Network network, double score)
        {
            generations.AddGenome(new Genome(score, network.getSave()));
        }

        public List<Network> NextGeneration()
        {
            List<Network.NetworkState> networks;

            if (generations.generations.Count == 0)
            {
                // If no Generations, create first.
                networks = generations.FirstGeneration(options);
            }
            else
            {
                // Otherwise, create next one.
                networks = generations.NextGeneration(options);
            }

            // Create Networks from the current Generation.
            var nns = new List<Network>();
            foreach (var net in networks)
            {
                var nn = new Network();
                nn.setSave(options, net);
                nns.Add(nn);
            }

            if (options.LowHistoric)
            {
                // Remove old Networks.
                if (generations.generations.Count >= 2)
                {
                    var genomes =
                        generations
                        .generations[generations.generations.Count - 2]
                        .Genomes;
                    foreach (var genome in genomes)
                    {
                        genome.Network = null;
                    }
                }
            }

            if (options.Historic != -1)
            {
                // Remove older generations.
                if (generations.generations.Count > options.Historic + 1)
                {
                    generations.generations = generations.generations.Take(
                        generations.generations.Count - (options.Historic + 1)).ToList();
                }
            }

            return nns;
        }

        public class Options
        {
            public virtual double Activation(double a)
            {
                var ap = -a;
                return (1 / (1 + Math.Exp(ap)));
            }

            public virtual double RandomClamped()
            {
                return rng.NextDouble() * 2 - 1;
            }

            /// <summary>
            /// Population by generation.
            /// </summary>
            public virtual uint Population => 50;
            /// <summary>
            /// Best networks kepts unchanged for the generation (rate).
            /// </summary>
            public virtual double Elitism => 0.2;
            /// <summary>
            /// New random networks for the next generation (rate).
            /// </summary>
            public virtual double RandomBehaviour => 0.2;
            /// <summary>
            /// Mutation rate on the weights of synapses.
            /// </summary>
            public virtual double MutationRate => 0.1;
            /// <summary>
            /// Interval of the mutation changes on the synapse weight.
            /// </summary>
            public virtual double MutationRange => 0.5;
            /// <summary>
            /// Crossover factor between two genomes.
            /// </summary>
            public virtual double GeneticCrossover => 0.5;

            /// <summary>
            /// Latest generations saved.
            /// </summary>
            public int Historic { get; set; } = 0;
            /// <summary>
            /// Only save score (not the network).
            /// </summary>
            public bool LowHistoric { get; set; } = false;
            /// <summary>
            /// Number of children by breeding.
            /// </summary>
            public int NumberChild { get; set; } = 1;

            public (int, int[], int) Network { get; set; } = (2, new[] { 2 }, 1);
        }

        [DataContract]
        public class Neuron
        {
            public double Value = 0;
            public List<double> Weights = new List<double>();

            public void Populate(Options options, int count)
            {
                Weights.Clear();
                Weights.AddRange(Enumerable.Range(0, count).Select(_ => options.RandomClamped()));
            }
        }

        [DataContract]
        public class Layer
        {
            public int Id = 0;
            public List<Neuron> Neurons = new List<Neuron>();

            public Layer() { }
            public Layer(int? id)
            {
                if (id.HasValue)
                    Id = id.Value;
            }

            public void Populate(Options options, int neuronCount, int inputCount)
            {
                Neurons.Clear();
                for (var i = 0; i < neuronCount; i++)
                {
                    var n = new Neuron();
                    n.Populate(options, inputCount);
                    Neurons.Add(n);
                }
            }
        }

        [DataContract]
        public class Network
        {
            public List<Layer> Layers = new List<Layer>();

            public void PerceptronGeneration(Options options, int inputs, int[] hiddens, int outputs)
            {
                var index = 0;
                var previousNeurons = 0;
                var layer = new Layer(index);
                layer.Populate(options, inputs, previousNeurons); // Number of Inputs will be set to
                                                                  // 0 since it is an input layer.
                previousNeurons = inputs; // number of input is size of previous layer.
                Layers.Add(layer);
                index++;
                foreach (var hid in hiddens)
                {
                    // Repeat same process as first layer for each hidden layer.
                    layer = new Layer(index);
                    layer.Populate(options, hid, previousNeurons);
                    previousNeurons = hid;
                    Layers.Add(layer);
                    index++;
                }
                layer = new Layer(index);
                layer.Populate(options, outputs, previousNeurons); // Number of input is equal to
                                                                   // the size of the last hidden
                                                                   // layer.
                Layers.Add(layer);
            }

            public double[] Compute(Options options, double[] inputs)
            {
                var inputLayer = Layers[0];
                if (inputs.Length != inputLayer.Neurons.Count)
                    throw new ArgumentException($"Invalid number of inputs. Expected: {inputLayer.Neurons.Count}");

                for (int i = 0; i < inputs.Length; i++)
                {
                    inputLayer.Neurons[i].Value = inputs[i];
                }

                var prevLayer = inputLayer; // Previous layer is input layer.
                for (var i = 1; i < Layers.Count; i++)
                {
                    for (var j = 0; j < Layers[i].Neurons.Count; j++)
                    {
                        // For each Neuron in each layer.
                        var sum = 0.0;
                        for (var k = 0; k < prevLayer.Neurons.Count; k++)
                        {
                            // Every Neuron in the previous layer is an input to each Neuron in
                            // the next layer.
                            sum += prevLayer.Neurons[k].Value *
                                Layers[i].Neurons[j].Weights[k];
                        }

                        // Compute the activation of the Neuron.
                        Layers[i].Neurons[j].Value = options.Activation(sum);
                    }
                    prevLayer = Layers[i];
                }

                // All outputs of the Network.
                var lastLayer = Layers.Last();
                return lastLayer.Neurons.Select(n => n.Value).ToArray();
            }

            [DataContract]
            public class NetworkState
            {
                public List<int> Neurons = new List<int>();
                public List<double> Weights = new List<double>();
            }

            public NetworkState getSave()
            {
                var ns = new NetworkState();
                foreach (var layer in Layers)
                {
                    ns.Neurons.Add(layer.Neurons.Count);
                    foreach (var neuron in layer.Neurons)
                    {
                        ns.Weights.AddRange(neuron.Weights);
                    }
                }
                return ns;
            }

            public void setSave(Options options, NetworkState save)
            {
                var previousNeurons = 0;
                var index = 0;
                var indexWeights = 0;
                Layers.Clear();
                foreach (var neuron in save.Neurons)
                {
                    // Create and populate layers.
                    var layer = new Layer(index);
                    layer.Populate(options, neuron, previousNeurons);
                    for (var j = 0; j < layer.Neurons.Count; j++)
                    {
                        for (var k = 0; k < layer.Neurons[j].Weights.Count; k++)
                        {
                            // Apply neurons weights to each Neuron.
                            layer.Neurons[j].Weights[k] = save.Weights[indexWeights];

                            indexWeights++; // Increment index of flat array.
                        }
                    }
                    previousNeurons = neuron;
                    index++;
                    Layers.Add(layer);
                }

            }
        }

        [DataContract]
        public class Genome
        {
            public double Score = 0;
            public Network.NetworkState Network;

            public Genome() { }
            public Genome(double? score, Network.NetworkState network)
            {
                if (score.HasValue)
                    Score = score.Value;
                Network = network;
            }
        }

        [DataContract]
        public class Generation
        {
            public List<Genome> Genomes = new List<Genome>();

            public void AddGenome(Genome genome)
            {
                Genomes.Add(genome);
                // That is very inefficient by real fast to write a one line
                Genomes = Genomes.OrderByDescending(g => g.Score).ToList();
            }

            public List<Genome> Breed(Options options, Genome g1, Genome g2, int childCount)
            {
                var datas = new List<Genome>();
                for (var nb = 0; nb < childCount; nb++)
                {
                    // Deep clone of genome 1.
                    var child = CloneGenome(g1);
                    for (var i = 0; i < g2.Network.Weights.Count; i++)
                    {
                        // Genetic crossover
                        if (rng.NextDouble() <= options.GeneticCrossover)
                        {
                            child.Network.Weights[i] = g2.Network.Weights[i];
                        }
                    }

                    // Perform mutation on some weights.
                    for (var i = 0; i < child.Network.Weights.Count; i++)
                    {
                        if (rng.NextDouble() <= options.MutationRate)
                        {
                            child.Network.Weights[i] += rng.NextDouble() *
                                options.MutationRange *
                                2 -
                                options.MutationRange;
                        }
                    }

                    datas.Add(child);
                }

                return datas;
            }

            public static Genome CloneGenome(Genome g)
            {
                var stream = new MemoryStream();
                Stride.Core.Serialization.BinarySerialization.Write(stream, g);
                stream.Position = 0;
                return Stride.Core.Serialization.BinarySerialization.Read<Genome>(stream);
            }

            public List<Network.NetworkState> GenerateNext(Options options)
            {
                var nexts = new List<Network.NetworkState>();

                for (var i = 0; i < Math.Round(options.Elitism *
                        options.Population); i++)
                {
                    if (nexts.Count < options.Population)
                    {
                        // Push a deep copy of ith Genome's Nethwork.
                        nexts.Add(CloneGenome(Genomes[i]).Network);
                    }
                }

                for (var i = 0; i < Math.Round(options.RandomBehaviour *
                        options.Population); i++)
                {
                    var n = CloneGenome(Genomes[i]).Network;
                    for (var k = 0; k < n.Weights.Count; k++)
                    {
                        n.Weights[k] = options.RandomClamped();
                    }
                    if (nexts.Count < options.Population)
                    {
                        nexts.Add(n);
                    }
                }

                var max = 0;
                while (true)
                {
                    for (var i = 0; i < max; i++)
                    {
                        // Create the children and push them to the nexts array.
                        var childs = Breed(options, Genomes[i], Genomes[max],
                            (options.NumberChild > 0 ? options.NumberChild : 1));
                        foreach (var child in childs)
                        {
                            nexts.Add(child.Network);
                            if (nexts.Count >= options.Population)
                            {
                                // Return once number of children is equal to the
                                // population by generatino value.
                                return nexts;
                            }
                        }
                    }
                    max++;
                    if (max >= Genomes.Count - 1)
                    {
                        max = 0;
                    }
                }
            }
        }

        [DataContract]
        public class Generations
        {
            public List<Generation> generations = new List<Generation>();

            public List<Network.NetworkState> FirstGeneration(Options options)
            {
                var outs = new List<Network.NetworkState>();
                for (var i = 0; i < options.Population; i++)
                {
                    // Generate the Network and save it.
                    var nn = new Network();
                    nn.PerceptronGeneration(options, options.Network.Item1,
                        options.Network.Item2,
                        options.Network.Item3);
                    outs.Add(nn.getSave());
                }

                generations.Add(new Generation());
                return outs;
            }

            public List<Network.NetworkState> NextGeneration(Options options)
            {
                var lastGen = generations.Last();
                generations.Add(new Generation());
                return lastGen.GenerateNext(options);
            }

            public void AddGenome(Genome genome)
            {
                generations.Last().AddGenome(genome);
            }
        }
    }
}
