using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using NeuralNet;
using MySerializer;

namespace ConnectFour
{
    /// <summary>
    /// Deserializes or Parses connect-4 8-ply database into a List of Boards then serializes as needed.
    /// </summary>
    public static class DataParser
    {
        public static List<Example> ValidationSet = new List<Example>();

        static DataParser()
        {
            Parse();
        }

        /// <summary>
        /// Parses the validation set from connect-4 8-ply database.
        /// </summary>
        /// <returns>Validation set</returns>
        public static List<Example> Parse()
        {
            ValidationSet.Clear();
            using (StringReader reader = new StringReader(Properties.Resources.connect_4))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');

                    Board board = new Board();
                    for (int i = 0; i < values.Length - 1; ++i)
                    {
                        string x = values[i].ToLower().Trim();
                        Checker checker = Checker.Empty;
                        switch (x)
                        {
                            case "x": checker = Checker.Blue; break;
                            case "o": checker = Checker.Green; break;
                            case "b": checker = Checker.Empty; break;
                        }
                        // Format of linear board data in connect-4.txt is bottom to top, left to right
                        board.AddChecker(checker, i/6);
                    }

                    // In connect-4.txt, it is X's turn to go next, which means
                    // player O has just went. Player O == Green, therefore
                    // we use Checker.Green in the following line.
                    Example example = Transform.ToNormalizedExample(board, Checker.Green);

                    string result = values[values.Length - 1].ToLower().Trim();

                    // Current values denote next player that goes will be guaranteed to win/lose/draw given he/she plays optimally...  
                    //  We need to normalize this for our network... Ie, the label should instead denote if last player that went for given board position win/loses/ties if he/she plays optimally.
                    GameResult gr =
                            result == "win" ? GameResult.Loss :
                            result == "loss" ? GameResult.Win :
                            GameResult.Draw;
                    example.Labels.Add(Transform.ToValue(gr));
                    ValidationSet.Add(example);
                }
            }
            return ValidationSet;
        }

        

    }
}
