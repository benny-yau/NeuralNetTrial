using ConnectFour;
using NeuralNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleGo
{
    public class ConnectFour
    {
        public void HumanVsHumanGame()
        {
            Board CurrentBoard = new ConnectFourBoard();
            int column;
            Checker checker = Checker.Black;
            do
            {
                //get player move
                Console.WriteLine("\nEnter column (0 to 6):");
                String input = Console.ReadLine();
                if (!Int32.TryParse(input, out column))
                    continue;

                CurrentBoard.AddChecker(checker, column);
                Console.WriteLine(CurrentBoard.ToString());
                if (CurrentBoard.IsGameOver)
                {
                    Console.WriteLine("Player wins.");
                    break;
                }
                checker = CurrentBoard.Toggle(checker);
            } while (true);
            Console.ReadLine();
        }

        public void PlayGameVsComputer()
        {
            //load network
            String fileName = System.IO.Directory.GetCurrentDirectory() + @"\cf\cf.net";
            Network network = Simulator.LoadNetwork(fileName);
            Bot bot = new NeuralNetBot(Checker.White, network, 0);

            Checker checker = Checker.Black;
            Board CurrentBoard = new ConnectFourBoard();
            int column;
            do
            {
                //get player move
                Console.WriteLine("\nEnter column (0 to 6):");
                String input = Console.ReadLine();
                if (!Int32.TryParse(input, out column))
                    continue;

                CurrentBoard.AddChecker(checker, column);
                if (CurrentBoard.IsGameOver)
                {
                    Console.WriteLine("Player wins.");
                    break;
                }
                Console.WriteLine(CurrentBoard.ToString());

                //make computer move
                (Tuple<int, int> move, double score) = bot.SelectMove(CurrentBoard);
                CurrentBoard.AddChecker(CurrentBoard.Toggle(checker), move.Item1);
                Console.WriteLine(CurrentBoard.ToString());
                if (CurrentBoard.IsGameOver)
                {
                    Console.WriteLine("Computer wins.");
                    break;
                }
            } while (true);
            Console.ReadLine();
        }

        public void TrainNetwork()
        {
            Termination termination = Termination.ByValidationSet(DataParser.ValidationSet(), 500);
            NetworkParameters parameters = new NetworkParameters()
            {
                InitialWeightInterval = new Tuple<double, double>(-0.05, 0.05),
                LearningRate = 0.05,
                LearningRateDecay = 0,
                Momentum = 0,
                MomentumDecay = 0
            };

            Network network = new Network(
                        "cf",
                        82,
                        100,
                        1,
                        termination,
                        parameters);

            Trainer Trainer = new Trainer(network);
            Trainer.Train(TrainingRegimen.Blank);
        }
    }
}
