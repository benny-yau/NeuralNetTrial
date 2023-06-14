using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuralNet;
using ConnectFour.Properties;

namespace ConnectFour
{
    public class Simulator
    {
        int TotalGames, JasonWon, AllenWon, Ties, Turns, TotalTurns;
        GameViewer Viewer { get; set; }
        
        public Simulator(GameViewer viewer = null)
        {
            Viewer = viewer;
        }


        /// <summary>
        /// Simulate a game until completion.
        /// </summary>
        /// <param name="board">Starting board that the bots will play on.  This need not be empty!</param>
        /// <param name="network">Neural network that provides the AI for gameplay.</param>
        /// <returns>Trace of game sequence, each board state stored as a Neural Net Example</returns>
        public List<Example> Play(Board board, Network network)
        {
            Bot allen = new NeuralNetBot(Checker.Blue, network); // <-- you know he will win :)
            Bot jason = new NeuralNetBot(Checker.Green, network);

            List<Example> trace = new List<Example>();

            Turns = 0;
            Bot current = allen.MyColor == board.NextPlayer ? allen : jason;
            while (!board.IsGameOver)
            {
                int column;
                double score;
                current.SelectMove(board, out column, out score);
                Log(String.Format("{0} picks column {1}   (Score: {2:f2})", (current == allen ? "Allen" : "Jason"), column, score));
                board.AddChecker(current.MyColor, column);

                Example example = Transform.ToNormalizedExample(board, current.MyColor);
                example.Predictions.Add(score);
                trace.Add(example);

                current = (current == allen ? jason : allen);
                ++Turns;
            }

            if (Viewer != null)
                Viewer.BatchAddCheckers(Checker.Blue, board.MoveHistory,completedBoard:board);

            TotalTurns += Turns;

            Checker winner;
            if (board.TryGetWinner(out winner))
            {
		//The game is over, there was a winner.
		//This means the last element of "trace" represents a won
		//board state (i.e. there is a four-in-a-row with color
		//'winner').

                if (trace.Count > 0) trace[trace.Count - 1].Predictions[0] = Transform.ToValue(GameResult.Win); 
                if (trace.Count > 1) trace[trace.Count - 2].Predictions[0] = Transform.ToValue(GameResult.Loss);
                if (winner == allen.MyColor)
                {
                    Log("WINNER:  Allen");
                    ++AllenWon;
                }
                else
                {
                    Log("WINNER:  Jason");
                    ++JasonWon;
                }
            }
            else
            {
                if (trace.Count > 0) trace[trace.Count - 1].Predictions[0] = Transform.ToValue(GameResult.Draw);  
                if (trace.Count > 1) trace[trace.Count - 2].Predictions[0] = Transform.ToValue(GameResult.Draw);
                Log("TIE");
                ++Ties;
            }

            ++TotalGames;
            Log(string.Format("Turns: {0} ({1:f2})", Turns, (double)TotalTurns / TotalGames));
            Log(string.Format("Allen: {0}({1:f2}) Jason: {2}({3:f2}) Ties {4}({5:f2})   TOTAL: {6}", AllenWon, (double)AllenWon / TotalGames, JasonWon, (double)JasonWon / TotalGames, Ties, (double)Ties / TotalGames, TotalGames));
            Log("");

            List<Example> trace1 = new List<Example>(), trace2 = new List<Example>();
            for (int i = 0; i < trace.Count; ++i)
            {
                if (i % 2 == 0) trace1.Add(trace[i]);
                else trace2.Add(trace[i]);
            }
            double lambda = .7;
            double alpha = .1;
            double gamma = .5;
            UpdateTraceLabels(trace1, lambda, alpha, gamma);
            UpdateTraceLabels(trace2, lambda, alpha, gamma);

            return trace1.Union(trace2).ToList();
        }

        // Critic: Takes as input the history/trace and estimates label based on successor board state (Successor meaning next time current player goes -- every two moves!).  Assume all features and predictions values are populated already.
        private void UpdateTraceLabels(List<Example> trace, double lambda, double alpha, double gamma)
        {
            for (int i = 0; i+1 < trace.Count; ++i)
            {
                trace[i].Labels = trace[i + 1].Predictions;
            }

            if (trace.Count > 0) trace[trace.Count - 1].Labels = trace[trace.Count - 1].Predictions;
            
            /*
            for (int i=0; i<trace.Count; ++i)
            {
                double sum = 0;
                double product = 1;
                for (int j=i; j<trace.Count; ++j)
                {
                    double reward = j == trace.Count - 1 ? trace[j].Predictions[0] : 0; // only give a reward if its the end of the game.  
                    double delta = j < trace.Count - 1 ?  reward + gamma * trace[j+1].Predictions[0] - trace[j].Predictions[0] : reward;
                    sum += product * delta;
                    product *= gamma * lambda;
                }
                trace[i].Predictions[0] += alpha * sum;
            }
            for (int i = 0; i < trace.Count; ++i)
                trace[i].Labels = trace[i].Predictions;
            */
        }


        void Log(string msg)
        {
            if (Viewer != null)
                Viewer.Log.WriteLine(msg);
        }
    }
}
