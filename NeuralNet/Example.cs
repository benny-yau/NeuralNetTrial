using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace NeuralNet
{
    [Serializable]
    public class Example
    {
        public List<double> Features = new List<double>();
        public List<double> Labels = new List<double>();
        public List<double> Predictions = new List<double>();

        private Example() { }

        /// <summary>
        /// Creates an example to be used when utilizing neural net.
        /// </summary>
        public Example(List<double> features = null, List<double> labels = null, List<double> predictions = null)
        {
            if (features != null) Features = features;
            if (labels != null) Labels = labels;
            if (predictions != null) Predictions = predictions;
        }

        public override string ToString()
        {
            string str = string.Empty;
			if (Features.Count == 42)
			{
				// treat this as a Connect-Four simple example
            	for (int i = 0; i < Features.Count; ++i)
            	{
                	str += Features[i]<0 ? "x" : Features[i] == 0 ? "-" : "o";
                	if (i % 7 == 6 && i < Features.Count - 1)
                    	str += "\r\n";
				}
            }
			else
			{
				for (int i = 0; i < Features.Count; i++)
				{
					str += String.Format("{0:#.00} ", Features[i]);
				}
			}
			if (Predictions.Count > 0)
				str += "  predicted " + Predictions[0];
			if (Labels.Count > 0)
				str += "  labeled " + Labels[0];
            return str;
        }
    }
}
