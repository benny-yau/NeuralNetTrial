using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using NeuralNet;
using MySerializer;
using ScenarioCollection;
using Go;
using Newtonsoft.Json.Linq;

namespace ConnectFour
{
    /// <summary>
    /// Deserializes or Parses connect-4 8-ply database into a List of Boards then serializes as needed.
    /// </summary>
    public static class DataParser
    {

        static List<Example> validationSet;
        public static List<Example> ValidationSet(GameType gameType = GameType.ConnectFour, SurviveOrKill surviveOrKill = SurviveOrKill.Kill)
        {
            if (validationSet == null)
            {
                if (gameType == GameType.ConnectFour)
                    validationSet = Parse();
                else
                    validationSet = ParseMappedJson(surviveOrKill);
            }
            return validationSet;
        }

        #region connect four
        /// <summary>
        /// Parses the validation set from connect-4 8-ply database.
        /// </summary>
        /// <returns>Validation set</returns>
        public static List<Example> Parse()
        {
            List<Example> validationSet = new List<Example>();
            using (StringReader reader = new StringReader(Properties.Resources.connect_4))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');

                    Board board = new ConnectFourBoard();
                    for (int i = 0; i < values.Length - 1; ++i)
                    {
                        string x = values[i].ToLower().Trim();
                        Checker checker = Checker.Empty;
                        switch (x)
                        {
                            case "x": checker = Checker.Black; break;
                            case "o": checker = Checker.White; break;
                            case "b": checker = Checker.Empty; break;
                        }
                        // Format of linear board data in connect-4.txt is bottom to top, left to right
                        board.AddChecker(checker, i / 6);
                    }
                    // In connect-4.txt, it is X's turn to go next, which means
                    // player O has just went. Player O == Green, therefore
                    // we use Checker.Green in the following line.
                    Example example = Transform.ToNormalizedExample(board, Checker.White);

                    string result = values[values.Length - 1].ToLower().Trim();

                    // Current values denote next player that goes will be guaranteed to win/lose/draw given he/she plays optimally...  
                    //  We need to normalize this for our network... Ie, the label should instead denote if last player that went for given board position win/loses/ties if he/she plays optimally.
                    GameResult gr =
                            result == "win" ? GameResult.Loss :
                            result == "loss" ? GameResult.Win :
                            GameResult.Draw;
                    example.Labels.Add(Transform.ToValue(gr));
                    validationSet.Add(example);
                }
            }
            return validationSet;
        }

        public static void PrintBoardToText(Board board, GameResult gr, String fileName)
        {
            String content = board.ToString() + Environment.NewLine + "Win/Loss : " + gr.ToString() + Environment.NewLine;
            File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + fileName, content);
            Debug.WriteLine(content);
        }
        #endregion

        #region go

        public static List<Example> ParseMappedJson(SurviveOrKill surviveOrKill)
        {
            List<Example> validationSet = new List<Example>();
            for (int i = 0; i <= ScenarioHelper.GameSets.Count - 1; i++)
            {
                GameSet gameSet = ScenarioHelper.GameSets[i];
                if (gameSet.Name == "Problem-Set") continue;
                if (gameSet.Levels.Count == 0)
                    validationSet = validationSet.Union(ParseScenarioData(gameSet.Name, "", surviveOrKill)).ToList();
                else
                {
                    for (int j = 0; j <= gameSet.Levels.Count - 1; j++)
                    {
                        String level = gameSet.Levels[j];
                        validationSet = validationSet.Union(ParseScenarioData(gameSet.Name, level, surviveOrKill)).ToList();
                    }
                }
            }
            return validationSet;
        }

        /// <summary>
        /// Parse scenario data.
        /// </summary>
        public static List<Example> ParseScenarioData(String gameSet, String level, SurviveOrKill surviveOrKill)
        {
            List<Example> validationSet = new List<Example>();
            List<Func<Scenario, Game>> scenarioList = ScenarioHelper.GetScenarioDelegates(gameSet, level);
            Debug.WriteLine("Parse mapped json for: " + gameSet + ", " + level + Environment.NewLine);
            for (int i = 0; i <= scenarioList.Count - 1; i++)
            {
                Game game = ScenarioHelper.GetScenarioFromList(scenarioList, i);
                if (GameHelper.IsSurviveOrKill(game.GameInfo, surviveOrKill)) continue;
                JArray mappedJson = GameMapping.GetMappedJson(game);
                foreach (JObject move in mappedJson)
                {
                    GoBoard b = new GoBoard(game.Board);
                    Content c = GameHelper.GetContentForNextMove(b);
                    Point firstMove = new Point((int)move["FirstMove"]["x"], (int)move["FirstMove"]["y"]);
                    if (!b.PointWithinBoard(firstMove)) continue;
                    b.InternalMakeMove(firstMove, c);
                    Boolean solutionMove = SolutionHelper.GetSolutionMove(b) != null;
                    Point secondMove = new Point((int)move["SecondMove"]["x"], (int)move["SecondMove"]["y"]);
                    if (!b.PointWithinBoard(secondMove)) continue;
                    b.InternalMakeMove(secondMove, c.Opposite());
                    //add to validation set
                    validationSet.Add(GetExample(new GoBoard(game.Board), b, c, solutionMove));
                    if (move["SecondLevel"] == null) continue;

                    foreach (JObject move2 in move["SecondLevel"])
                    {
                        GoBoard b2 = new GoBoard(b);
                        Point thirdMove = new Point((int)move2["ThirdMove"]["x"], (int)move2["ThirdMove"]["y"]);
                        if (!b2.PointWithinBoard(thirdMove)) continue;
                        b2.InternalMakeMove(thirdMove, c);
                        Boolean solutionMove2 = SolutionHelper.GetSolutionMove(b2) != null;
                        Point fourthMove = new Point((int)move2["FourthMove"]["x"], (int)move2["FourthMove"]["y"]);
                        if (!b2.PointWithinBoard(fourthMove)) continue;
                        b2.InternalMakeMove(fourthMove, c.Opposite());
                        //add to validation set
                        validationSet.Add(GetExample(b, b2, c, solutionMove2));
                        if (move["ThirdLevel"] == null) continue;

                        foreach (JObject move3 in move["ThirdLevel"])
                        {
                            GoBoard b3 = new GoBoard(b2);
                            Point fifthMove = new Point((int)move3["FifthMove"]["x"], (int)move3["FifthMove"]["y"]);
                            if (!b3.PointWithinBoard(fifthMove)) continue;
                            b3.InternalMakeMove(fifthMove, c);
                            Boolean solutionMove3 = SolutionHelper.GetSolutionMove(b3) != null;
                            Point sixthMove = new Point((int)move3["SixthMove"]["x"], (int)move3["SixthMove"]["y"]);
                            if (!b3.PointWithinBoard(sixthMove)) continue;
                            b3.InternalMakeMove(sixthMove, c.Opposite());
                            //add to validation set
                            validationSet.Add(GetExample(b2, b3, c, solutionMove3));
                        }
                    }
                }
            }
            return validationSet;
        }

        /// <summary>
        /// Get example.
        /// </summary>
        public static Example GetExample(GoBoard rootBoard, GoBoard b, Content c, Boolean solutionMove = false)
        {
            Example example = Transform.ToNormalizedExample(b, (Checker)c);
            //result for next move
            GameResult result = solutionMove ? GameResult.Win : GameResult.Loss;
            example.Labels.Add(Transform.ToValue(result));
            example.ScenarioName = b.GameInfo.ScenarioName;
            example.NumberOfMoves = b.LastMoves.Count;
            example.Board = b;
            example.RootBoard = rootBoard;
            return example;
        }
        #endregion
    }
}
