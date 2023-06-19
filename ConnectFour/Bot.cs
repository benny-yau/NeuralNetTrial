using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NeuralNet;

namespace ConnectFour
{

    public enum LambdaType { ProbabilityDistribution, Threshold }

    /// <summary>
    /// Plays the game of ConnectFour using an unspecified algorithm.
    /// </summary>
    [Serializable]
    public abstract class Bot
    {
        public Checker MyColor;
        public double Lambda;
        public LambdaType LambdaType;
        static Random RANDOM = new Random();

        protected Bot(Checker myColor, double? lambda = null, LambdaType? lambdaType = null)
        {
            Debug.Assert(myColor != Checker.Empty);
            MyColor = myColor;
            Lambda = lambda ?? 0.05;
            LambdaType = lambdaType ?? LambdaType.ProbabilityDistribution;
        }

        protected abstract double EvaluateBoard(Board board);
        public abstract Example MakeExample(Board board, Checker color);
        public abstract void LearnOneExample(Example example);

        public (Tuple<int, int>, double, List<Go.LinkedPoint<Tuple<int, int>>>) recSelectMove(Board board)
        {
            List<Go.LinkedPoint<Tuple<int, int>>> evaluations = new List<Go.LinkedPoint<Tuple<int, int>>>();
            Tuple<int, int> bestX = null;
            double bestV = Double.NegativeInfinity;
            IEnumerable<Board> boards = board.GetPossibleMoves(MyColor);
            foreach (Board b in boards)
            {
                double v = EvaluateBoard(b);
                evaluations.Add(new Go.LinkedPoint<Tuple<int, int>>(b.Move, v));
                if (v > bestV)
                {
                    bestV = v;
                    bestX = b.Move;
                }
            }
            return (bestX, bestV, evaluations);
        }

        /// <summary>
        ///  Select move.
        ///  we pick a move randomly, using a probability distribution such that the moves with the "best" board positions have a
        ///  higher probability of being selected higher values of Lambda mean choices will have more equal probability, even if they had different 
        ///  low values of Lambda will have the opposite effect Lambda should be positive number.  Otherwise, no exploration will take place. 
        ///  If non-positive, just return the "best" move now, to avoid divide-by-zero type issues.
        /// </summary>
        public (Tuple<int, int>, double) SelectMove(Board board)
        {
            (Tuple<int, int> move, double score, List<Go.LinkedPoint<Tuple<int, int>>> evaluations) = recSelectMove(board);
            if (LambdaType == LambdaType.ProbabilityDistribution && Lambda > 0)
            {
                double sum = 0.0;
                double[] weights = new double[evaluations.Count];
                for (int i = 0; i < evaluations.Count; i++)
                {
                    // the closer this column's evaluation to the "best", the greater weight it will have
                    double w = 1 / (Lambda + (score - (double)evaluations[i].CheckMove));
                    weights[i] = w;
                    sum += w;
                }

                double r = RANDOM.NextDouble() * sum;
                int c;
                for (c = 0; c + 1 < weights.Length; c++)
                {
                    r -= weights[c];
                    if (r <= 0)
                        break;
                }
                if (evaluations.Count() == 0)
                    move = null;
                else
                {
                    move = evaluations[c].Move;
                    score = (double)evaluations[c].CheckMove;
                }
            }
            return (move, score);
        }



    }
}
