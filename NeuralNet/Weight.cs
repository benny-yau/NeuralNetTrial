using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuralNet
{
	// Need to encapsulate within a class because we need to get a pointer, hence updates to upstream weights reflect in downstream weights
	[Serializable]
	public class Weight
	{
		// create a Random object once and use the same Random object
		// each time a new random value is needed; otherwise you are
		// not really random
		static Random RANDOM = new Random();

		public double Value;
		public Weight(double value) { Value = value; }
		public Weight(double min, double max)
		{
			min = Math.Min(min, max);
			max = Math.Max(min, max);
			Value = RANDOM.NextDouble() * (max - min) + min;
		}
		public Weight(Tuple<double, double> range) : this(range.Item1, range.Item2) { }
	}
}
