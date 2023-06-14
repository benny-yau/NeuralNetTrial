using ConnectFour;
using Go;
using NeuralNet;
using ScenarioCollection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleGo
{
    public class Go
    {

        public void ShowNetworkResults()
        {
            //load network
            String fileName = System.IO.Directory.GetCurrentDirectory() + @"\nn1\X_nn1_25200_0.314306536896135.net";
            Network network = Simulator.LoadNetwork(fileName);
            do
            {
                Game game = GetScenarioGame();
                GoBoard CurrentBoard = new GoBoard(game.Board);
                Content c = game.Board.GameInfo.StartContent;
                Checker checker = (Checker)c;

                double score;
                Tuple<int, int> move;
                List<LinkedPoint<Tuple<int, int>>> columnEvaluations;
                Bot bot = new NeuralNetBot(checker, network, 0);
                bot.recSelectMove(CurrentBoard, out move, out score, out columnEvaluations);
                Console.WriteLine(CurrentBoard.ToString() + Environment.NewLine);
                GetAnswer(game);
                Console.WriteLine(Environment.NewLine);
                List<double> results = columnEvaluations.Select(n => (double)n.CheckMove).ToList();
                double lowerSD = results.Mean() - (results.StandardDeviation() * 2);
                foreach (LinkedPoint<Tuple<int, int>> p in columnEvaluations.OrderByDescending(n => (double)n.CheckMove))
                    Console.WriteLine("Move: " + p.Move + " - " + p.CheckMove.ToString() + ((((double)p.CheckMove) < lowerSD) ? " (out)" : ""));
                Console.WriteLine(Environment.NewLine + Environment.NewLine);
            } while (true);
        }

        public static Game GetScenarioGame()
        {
            //select game set and level
            List<String> gameSetList = new List<String>();
            ScenarioHelper.VerifyForAllScenarios((gameSet, level) => gameSetList.Add(gameSet + " - " + level));
            Console.WriteLine("Select scenario:");
            for (int i = 0; i <= gameSetList.Count - 1; i++)
                Console.WriteLine((i + 1).ToString().PadLeft(3, ' ') + ": " + gameSetList[i]);

            Console.WriteLine("\nSelect game set (1 to " + gameSetList.Count.ToString() + "):");

            int gameSetSelected = SelectFromList<String>(gameSetList);
            String selectedGameSet = gameSetList[gameSetSelected - 1];
            String[] gameSetLevel = selectedGameSet.Split(new string[] { " - " }, StringSplitOptions.None);

            //get all scenarios for gameset and level
            List<Func<Scenario, Game>> scenarioList = ScenarioHelper.AddScenarios(gameSetLevel[0].Trim(), (gameSetLevel.Length == 2) ? gameSetLevel[1].Trim() : "");

            //select scenario number
            Console.WriteLine("Select scenario number (1 to " + scenarioList.Count.ToString() + ") : ");
            int scenarioSelected = SelectFromList<Func<Scenario, Game>>(scenarioList);

            //return selected scenario
            return ScenarioHelper.GetScenarioFromList(scenarioList, scenarioSelected - 1);
        }

        public static int SelectFromList<T>(List<T> list)
        {
            int selected = 0;
            do
            {
                String input = Console.ReadLine();
                if (Int32.TryParse(input, out selected))
                {
                    if (selected > 0 && selected <= list.Count)
                        return selected;
                }
            } while (true);
        }
        public static void GetAnswer(Game g)
        {
            if (g.GameInfo.solutionPoints.Count == 0)
                Console.WriteLine("No answers for this scenario.");

            List<Point> solution = g.GameInfo.solutionPoints.First();

            String msg = "";
            for (int i = 0; i <= solution.Count - 1; i++)
            {
                Point p = solution[i];
                msg += p;
                if (i < solution.Count - 1)
                    msg += ",";
            }
            Console.WriteLine("\nSolution: " + msg + "\n");
        }

        public void HumanVsHumanGame()
        {
            GoBoard CurrentBoard = new GoBoard();
            double score;
            int column;
            Checker checker = Checker.Black;
            do
            {
                GetNextMoveFromUser(CurrentBoard, (Content)checker);
                Console.WriteLine(CurrentBoard.ToString());
                checker = CurrentBoard.Toggle(checker);
            } while (true);
            Console.ReadLine();
        }

        public void GetNextMoveFromUser(GoBoard CurrentBoard, Content c)
        {
            int x, y;
            bool parseX, parseY;
            MakeMoveResult result = MakeMoveResult.Unknown;
            do
            {
                Console.WriteLine("Enter x position: ");
                String input = Console.ReadLine();
                if (input == "x") return;
                parseX = Int32.TryParse(input, out x);
                Console.WriteLine("Enter y position: ");
                parseY = Int32.TryParse(Console.ReadLine(), out y);
                if (parseX && parseY)
                {
                    result = CurrentBoard.InternalMakeMove(x, y, c);
                    if (result != MakeMoveResult.Legal)
                        Console.WriteLine("Illegal move.");
                    else
                        break;
                }
            } while (true);
        }

        static Network network;
        public void TrainNetwork(Boolean startNew = true)
        {
            Termination termination = Termination.ByValidationSet(DataParser.ValidationSet(GameType.Go, SurviveOrKill.Kill), 1800);
            NetworkParameters parameters = new NetworkParameters()
            {
                InitialWeightInterval = new Tuple<double, double>(-0.05, 0.05),
                LearningRate = 0.05,
                LearningRateDecay = 0,
                Momentum = 0,
                MomentumDecay = 0
            };

            if (startNew)
            {
                network = new Network(
                            "nn1",
                            143,
                            429,
                            1,
                            termination,
                            parameters);
            }
            else
            {
                String fileName = System.IO.Directory.GetCurrentDirectory() + @"\nn1\nn1.net";
                Network network = Simulator.LoadNetwork(fileName);
                network = Simulator.LoadNetwork(fileName);
                network.Termination = termination;
            }
            do
            {
                ScenarioHelper.VerifyForAllScenarios(TrainOnScenario);
                network.Termination.TotalIterations = 0;
            } while (true);
        }

        public static void TrainOnScenario(String gameSet, String level)
        {
            int totalCount = DataParser.ValidationSet(GameType.Go).Count;
            List<Func<Scenario, Game>> scenarioList = ScenarioHelper.GetScenarioDelegates(gameSet, level);
            for (int i = 0; i <= scenarioList.Count - 1; i++)
            {
                //get scenario
                Game game = ScenarioHelper.GetScenarioFromList(scenarioList, i);
                GoBoard board = new GoBoard(game.Board);

                Debug.WriteLine("Scenario: " + game.GameInfo.ScenarioName + " - " + (i + 1).ToString() + " out of " + scenarioList.Count + " in " + gameSet + ((!String.IsNullOrEmpty(level)) ? ", " + level : ""));
                Debug.WriteLine("Total iterations: " + (network.Termination.TotalIterations + 1).ToString() + " out of " + totalCount);
                //train scenario
                Trainer Trainer = new Trainer(network);
                Trainer.TrainWithValidationSet(() => board);
                network.Termination.CurrentIteration = 0;
            }
        }
    }
}
