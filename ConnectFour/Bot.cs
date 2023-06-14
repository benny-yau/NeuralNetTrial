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

        public void recSelectMove(Board board, int depth, bool max, out int column, out double score, out List<double> columnEvaluations)
        {
            int bestX = 0;
            columnEvaluations = Enumerable.Repeat(double.NegativeInfinity, board.Columns).ToList();
            double bestV = max ? Double.NegativeInfinity : Double.PositiveInfinity; ;

            for (int x = 0; x < board.Columns; x++)
            {
                if (!board.IsColumnFull(x))
                {
                    board.AddChecker(MyColor, x);
                    int col;
                    double v;
                    if (depth <= 1 || board.IsGameOver)
                    {
                        col = x;
                        v = EvaluateBoard(board);
                    }
                    else
                    {
                        List<double> ignore = (new double[board.Columns]).ToList();
                        recSelectMove(board, depth - 1, !max, out col, out v, out ignore);
                    }
                    board.RemoveChecker(x);
                    columnEvaluations[x] = v;

                    if (v > bestV && max || v < bestV && !max)
                    {
                        bestV = v;
                        bestX = col;
                    }
                }
            }
            column = bestX;
            score = bestV;
        }

        public void SelectMove(Board board, out int column, out double score, int depth = 1)
        {
            // Lambda percent of the time, select a random move.
            if (LambdaType == LambdaType.Threshold && RANDOM.NextDouble() <= Lambda)
            {
                int[] cols = Enumerable.Range(0, board.Columns).Where(x => !board.IsColumnFull(x)).ToArray();
                column = cols[RANDOM.Next(cols.Count())];
                board.AddChecker(MyColor, column);
                score = EvaluateBoard(board);
                board.RemoveChecker(column);
                return;
            }

            List<double> columnEvaluations;
            recSelectMove(board, depth, true, out column, out score, out columnEvaluations);

            //we pick a move randomly, using a probability distribution such that the moves with the "best" board positions have a
            //  higher probability of being selected higher values of Lambda mean choices will have more equal probability, even if they had different 
            //  low values of Lambda will have the opposite effect Lambda should be positive number.  Otherwise, no exploration will take place. 
            //  If non-positive, just return the "best" move now, to avoid divide-by-zero type issues.
            if (LambdaType == LambdaType.ProbabilityDistribution && Lambda > 0)
            {
                double sum = 0.0;
                double[] weights = new double[columnEvaluations.Count];
                for (int i = 0; i < columnEvaluations.Count; i++)
                {
                    // the closer this column's evaluation to the "best", the
                    // greater weight it will have
                    double w = 1 / (Lambda + (score - columnEvaluations[i]));
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

                column = c;
                score = columnEvaluations[c];
            }
        }
    }
}
