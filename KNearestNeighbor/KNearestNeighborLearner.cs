using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NeuralNet;

namespace KNearestNeighborLearner
{

    [Serializable]
    public class KNearestNeighbor
    {
        public int k; //number of nearest neighbors to use
        public int n; //number of instances to store as training set
        public List<Example> savedExamples;

        private KNearestNeighbor() { }

        public KNearestNeighbor(int k, int n)
        {
            this.k = k;
            this.n = n;
            this.savedExamples = new List<Example>();
        }

        public void LearnOneExample(Example example)
        {
            if (savedExamples.Count >= n)
            {
                //delete the oldest example
                savedExamples.RemoveAt(1);
            }
            savedExamples.Add(example);
        }

        private double distance_sq(Example e1, Example e2)
        {
            double sum = 0.0;
            Debug.Assert(e1.Features.Count == e2.Features.Count);
            for (int i = 0; i < e1.Features.Count; i++)
            {
                double x1 = e1.Features[i];
                double x2 = e2.Features[i];
                sum += Math.Pow(x1 - x2, 2.0);
            }
            return sum;
        }

        private class BestExample : IComparable
        {
            public Example example;
            public double distance;

            public BestExample(Example e, double d)
            {
                example = e;
                distance = d;
            }

            public int CompareTo(object o)
            {
                return distance.CompareTo(((BestExample)o).distance);
            }
        }

	class LearnerNotReady : ApplicationException
	{
		public LearnerNotReady() : base("Learner is not ready to make predictions.")
		{
		}
	}

        public void PropogateInput(Example example)
        {
            List<BestExample> best = new List<BestExample>();
            double maxBest = 0.0;

            foreach (Example e in savedExamples)
            {
                double d = distance_sq(e, example);
                if (best.Count < k)
                {
                    best.Add(new BestExample(e, d));
                    if (d > maxBest) { maxBest = d; }
                }
                else if (d < maxBest)
                {
                    // at this point, the Best list contains k elements
                    best.Sort();
                    maxBest = best[k - 1].distance;
                    best[k - 1] = new BestExample(e, d);
                }
            }

		if (best.Count < 1)
			throw new LearnerNotReady();

            // perform a weighted average
            List<Double> result = new List<Double>();
            for (int i = 0; i < best[0].example.Labels.Count; i++)
            {
                result.Add(0.0);
            }
            double sumWeight = 0.0;

            foreach (BestExample be in best)
            {
                double thisWeight = 1 / (1 + be.distance);
                for (int i = 0; i < result.Count; i++)
                {
                    result[i] += be.example.Labels[i] * thisWeight;
                }
                sumWeight += thisWeight;
            }

            for (int i = 0; i < result.Count; i++)
            {
                result[i] /= sumWeight;
            }
            example.Predictions = result;
        }


    }//end KNearestNeighbor class

}//end KNearestNeighbor namespace
