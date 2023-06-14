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
        public List<Example> Play(Board rootBoard, Network network, Example example = null)
        {
            Bot allen = new NeuralNetBot(Checker.Black, network); // <-- you know he will win :)
            Bot jason = new NeuralNetBot(Checker.White, network);
            List<Example> trace = new List<Example>();

            Turns = 0;
            Board board = rootBoard;
            if (rootBoard is Go.Board) board = new GoBoard((Go.Board)example.RootBoard);
            if (example == null)
            {
                //network play
                GetNetworkPlayMoves(board, trace, allen, jason);
                return ProcessNetworkPlay(board, trace, allen, jason);
            }
            else
            {
                //validation play
                GetValidationPlayMoves(board, trace, allen, jason, example);
                return ProcessValidationPlay(trace, example);
            }
        }

        /// <summary>
        /// Process network play.
        /// </summary>
        public List<Example> ProcessNetworkPlay(Board board, List<Example> trace, Bot allen, Bot jason)
        {
            Checker winner;
            if (board.TryGetWinner(out winner))
            {
                //The game is over, there was a winner.
                //This means the last element of "trace" represents a won board state (i.e. there is a four-in-a-row with color 'winner').
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

        /// <summary>
        /// Process validation play.
        /// </summary>
        List<Example> ProcessValidationPlay(List<Example> trace, Example example)
        {
            double result = example.Labels.First();
            Boolean winOrLose = (result == 1);
            if (trace.Count > 0) trace[trace.Count - 1].Predictions[0] = Transform.ToValue((winOrLose) ? GameResult.Loss : GameResult.Win);
            if (trace.Count > 1) trace[trace.Count - 2].Predictions[0] = Transform.ToValue((winOrLose) ? GameResult.Win : GameResult.Loss);

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

        /// <summary>
        /// Get network play moves.
        /// </summary>
        public void GetNetworkPlayMoves(Board board, List<Example> trace, Bot allen, Bot jason)
        {
            Bot current = allen.MyColor == board.NextPlayer ? allen : jason;
            while (!board.IsGameOver)
            {
                Tuple<int, int> column;
                double score;
                //select move
                current.SelectMove(board, out column, out score);
                Log(String.Format("{0} picks column {1}   (Score: {2:f2})", (current == allen ? "Allen" : "Jason"), column, score));
                if (column.Item1 == -1 || column.Item2 == -1) break;
                //make move
                board.AddChecker(current.MyColor, column.Item1, column.Item2);
                //make prediction
                Example example = Transform.ToNormalizedExample(board, current.MyColor);
                example.Predictions.Add(score);
                trace.Add(example);

                current = (current == allen ? jason : allen);
                ++Turns;
            }
        }

        /// <summary>
        /// Get validation play moves.
        /// </summary>
        public void GetValidationPlayMoves(Board board, List<Example> trace, Bot allen, Bot jason, Example validationExample)
        {
            Bot current = allen.MyColor == board.NextPlayer ? allen : jason;
            Go.Board b = (Go.Board)validationExample.Board;

            Tuple<int, int> column;
            double score;
            foreach (Go.Point p in b.LastMoves)
            {
                List<Go.LinkedPoint<Tuple<int, int>>> columnEvaluations;
                //select move
                current.recSelectMove(board, out column, out score, out columnEvaluations);
                column = Tuple.Create(p.x, p.y);
                //make move
                board.AddChecker(current.MyColor, column.Item1, column.Item2);
                //make prediction
                Example example = Transform.ToNormalizedExample(board, current.MyColor);
                example.Predictions.Add(score);
                trace.Add(example);

                current = (current == allen ? jason : allen);
                ++Turns;
            }
        }

        // Critic: Takes as input the history/trace and estimates label based on successor board state (Successor meaning next time current player goes -- every two moves!).  Assume all features and predictions values are populated already.
        private void UpdateTraceLabels(List<Example> trace, double lambda, double alpha, double gamma)
        {
            for (int i = 0; i + 1 < trace.Count; ++i)
                trace[i].Labels = trace[trace.Count - 1].Predictions;
            if (trace.Count > 0) trace[trace.Count - 1].Labels = trace[trace.Count - 1].Predictions;
        }

        void Log(string msg)
        {
            if (Viewer != null) Viewer.Log.WriteLine(msg);
        }

        /// <summary>
        /// Load network.
        /// </summary>
        public static Network LoadNetwork(String path)
        {
            Settings.Default.Difficulty = 10;
            Settings.Default.CurrentNetworkPath = path;
            Settings.Default.CurrentNetwork = null;

            Network network = Settings.Default.CurrentNetwork;
            if (network == null)
                throw new Exception("Network not found");
            return network;
        }
    }
}
