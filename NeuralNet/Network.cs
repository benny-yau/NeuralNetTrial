using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;

namespace NeuralNet
{

	public enum NetworkStatus { Initiated, Running, Paused, Trained }

	[Serializable]
	public class Network
	{
		/// <summary>
		/// Name of the network. Used to compute file paths to save networks as they are generated.  DefaultPath/Name/Name_1, Name_2, ....
		/// </summary>
		public string Name;
		public double? TrueError = null;
		public List<Point> ErrorHistory = new List<Point>();

		public List<Neuron> Hiddens { get { return Neurons.Where(n => n.Type == NeuronType.Hidden).ToList(); } }
		public List<Neuron> Inputs { get { return Neurons.Where(n => n.Type == NeuronType.Input).ToList(); } }
		public List<Neuron> Outputs { get { return Neurons.Where(n => n.Type == NeuronType.Output).ToList(); } }
		public List<Neuron> Constants { get { return Neurons.Where(n => n.Type == NeuronType.Constant).ToList(); } }
		public List<Neuron> Neurons = new List<Neuron>();
		public List<int> HiddensLayout = new List<int>();
		public NetworkParameters Parameters = new NetworkParameters();
		Termination _termination;
		public Termination Termination { get { return _termination; } set { _termination = value; if (_termination != null) _termination.Network = this; } }
		public bool IsTrained { get { return Termination.IsNetworkTrained; } }
		public NetworkTimer TrainTime = new NetworkTimer();

		private Network() { }
		public Network(string name, int inputs, int hiddens, int outputs, Termination termination, NetworkParameters parameters = null) : this(name, inputs, new List<int>() { hiddens }, outputs, termination, parameters) { }
		public Network(string name, int inputs, List<int> hiddens, int outputs, Termination termination, NetworkParameters parameters = null, int constants = 1)
		{
			Name = name;
			Termination = termination;
			if (parameters != null)
			{
				parameters.Assert();
				Parameters = parameters;
			}
			HiddensLayout = hiddens;

			List<List<Neuron>> levels = new List<List<Neuron>>();
			levels.Add(Enumerable.Range(0, inputs).Select(i => new Neuron(NeuronType.Input)).ToList());
			foreach (int hidden in hiddens)
				levels.Add(Enumerable.Range(0, hidden).Select(i => new Neuron(NeuronType.Hidden)).ToList());
			levels.Add(Enumerable.Range(0, outputs).Select(i => new Neuron(NeuronType.Output)).ToList());

			// Connects to all other neurons.. except input ones (this is used as the additive "constant" value attached to each sigmoid unit.)
			List<Neuron> connectToAll = Enumerable.Range(0, constants).Select(i => new Neuron(NeuronType.Constant) { Value = 1 }).ToList();

			for (int i = 0; i + 1 < levels.Count; ++i)
			{
				foreach (Neuron n in levels[i])
					foreach (Neuron m in levels[i + 1])
						n.Attach(m, new Weight(Parameters.InitialWeightInterval));

				foreach (Neuron c in connectToAll)
					foreach (Neuron m in levels[i + 1])
						c.Attach(m, new Weight(Parameters.InitialWeightInterval));
			}

			Neurons = levels.Aggregate<List<Neuron>>((aggregated, level) => { aggregated.AddRange(level); return aggregated; }).ToList();
			Neurons.AddRange(connectToAll);
		}

		/// <summary>
		/// Trains a network using a given set of examples.
		/// </summary>
		/// <param name="examples">Set of examples.  Make sure features and labels lists are populated before training.</param>
		/// <param name="iterations">Number of iterations to train on the given set of examples</param>
		/// <returns>True if network is fully trained.  False otherwise.  (Based on Termination object given in constructor)</returns>
		public bool TrainNetwork(List<Example> examples)
		{
			AssertValidForTraining(examples);
			Termination.CompleteIteration();
			for (int i = 0; i < examples.Count; i++)
				LearnOneExample(examples[i]);
			return false;
		}

		public void LearnOneExample(Example example)
		{
			PropogateInput(example);
			PropogateErrors(example);
			UpdateWeights();
		}

		/// <summary>
		/// Propogates input through network (FeedForward).  Updates example.predictions.
		/// </summary>
		public void PropogateInput(Example example)
		{
			AssertValidFeatures(example);

			Neuron.UnfeedAll(Neurons);

			int feature = 0;
			foreach (Neuron neuron in Inputs)
			{
				neuron.Value = example.Features[feature++];
				neuron.Fed = true;
			}
			foreach (Neuron neuron in Constants)
			{
				neuron.Value = 1;
				neuron.Fed = true;
			}
			foreach (Neuron neuron in Neurons)
				recur_FeedForward(neuron);

			example.Predictions = Outputs.Select(o => o.Value).ToList();
		}

		private void recur_FeedForward(Neuron neuron)
		{
			if (neuron.Fed)
				return;

			foreach (Neuron n in neuron.Upstream)
				recur_FeedForward(n);
			neuron.UpdateOutputValue(); // Safe to call since all upstream values have been updated.  *Note* Only Hidden and Output units' values are adjusted. Constants and Inputs units are left untouched.
			neuron.Fed = true;
		}

		private void PropogateErrors(Example example)
		{
			AssertValidLabels(example);

			Neuron.UnfeedAll(Neurons);

			int label = 0;
			foreach (Neuron neuron in Outputs)
			{
				neuron.UpdateErrorTerm(example.Labels[label++]);
				neuron.Fed = true;
			}

			foreach (Neuron neuron in Neurons)
				recur_BackPropogation(neuron);
		}

		private void recur_BackPropogation(Neuron neuron)
		{
			if (neuron.Fed)
				return;

			foreach (Neuron n in neuron.Downstream)
				recur_BackPropogation(n);
			neuron.UpdateErrorTerm(); // Safe to call since all downstream errorterms have been updated.
			neuron.Fed = true;
		}

		private void UpdateWeights()
		{
			foreach (Neuron neuron in Neurons)
				neuron.UpdateDownstreamWeights(Parameters.LearningRate, Parameters.Momentum);
		}



		private void AssertValidFeatures(Example example)
		{
			if (example.Features.Count != Inputs.Count)
				throw new Exception("The number of features must match the number of Input Neurons.\r\n" + "Features: " + example.Features.Count + " Inputs: " + Inputs.Count);
		}
		private void AssertValidLabels(Example example)
		{
			if (example.Labels.Count != Outputs.Count)
				throw new Exception("The number of labels must match the number of Output Neurons.\r\n" + "Labels: " + example.Features.Count + " Outputs: " + Inputs.Count);
		}
		private void AssertValidForTraining(Example example)
		{
			AssertValidFeatures(example);
			AssertValidLabels(example);
		}
		private void AssertValidForTraining(List<Example> examples)
		{
			examples.ForEach(e => AssertValidForTraining(e));
		}

	}

	[Serializable]
	public class NetworkParameters
	{
		public double LearningRate = .05;
		public double Momentum = 0;
		public double LearningRateDecay = 0;
		public double MomentumDecay = 0;
		public Tuple<double, double> InitialWeightInterval = Tuple.Create(-.05, .05);

		public void Assert()
		{
			if (LearningRate <= 0) throw new Exception("LearningRate > 0");
			if (Momentum < 0 || Momentum >= 1) throw new Exception("0 <= Momentum < 1");
		}
	}

}
