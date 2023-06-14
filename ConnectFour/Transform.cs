using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuralNet;
using System.Diagnostics;

namespace ConnectFour
{
    /// <summary>
    /// Contains the constant functions that transform Checkers/EndGameStates to double values to feed into neural network.
    /// </summary>
    public static class Transform
    {
        public static double ToValue(Checker checker) 
        {
            switch (checker)
            {
                case Checker.Blue: return -1.0; 
                case Checker.Green: return 1.0; 
                case Checker.Empty: return 0.0; 
                default: throw new Exception();
            }
        }

        public static double ToValue(GameResult result)
        {
            switch (result)
            {
                case GameResult.Loss: return 0;
                case GameResult.Draw: return 0.5;
                case GameResult.Win: return 1;
                default: throw new Exception();
            }
        }

        public static double ToNormalizedValue(Checker checker, Checker lastPlayerToGo)
        {
            if (checker == Checker.Empty)
                return ToValue(checker);
            else if (checker == lastPlayerToGo)
                return ToValue(Checker.Green);
            else
                return ToValue(Checker.Blue);  // The player that is about to go will be blue.  
        }


        /// <summary>
        /// Converts board to neurel net training example.  Returns example corresopnding to normalized board (current player's checker color is blue, last player is green)
        /// </summary>
        /// <param name="lastPlayerToGo">Current Player which corresponds to last checker placed on board.</param>
        public static Example ToNormalizedExample(Board board, Checker lastPlayerToGo)
        {
            Debug.Assert(lastPlayerToGo != Checker.Empty);
            List<double> boardState = board.Cells.Cast<Checker>().Select(c=>Transform.ToNormalizedValue(c, lastPlayerToGo)).ToList();
            List<int> features = new List<int>();

            // 42 Input Units - Board State Only
            // return new Example(boardState);
            
            foreach (Checker checker in new List<Checker> { lastPlayerToGo, Board.Toggle(lastPlayerToGo) })
            {
                features.AddRange(board.LineOfX(checker));
                features.AddRange(board.LineOfX(checker, potential: true));
                features.AddRange(board.NumbersInColumns(checker));
                features.AddRange(board.NumbersInRows(checker));
                features.Add(board.NumberOnBoard(checker));
            }
            boardState.AddRange(features.Select(e => (double)e));

            // 40 Input Units - Features Only
            //return new Example(features.Select(e => (double)e).ToList());

            // 82 Input Units - Board State and Features
            return new Example(boardState);

        }

  
    }
}
