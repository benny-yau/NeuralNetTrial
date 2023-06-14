using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleGo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("1. Train network");
                Console.WriteLine("2. Show network results");
                int i = Go.SelectFromList<int>(new List<int> { 1, 2 });
                Go game = new Go();
                if (i == 1)
                    game.TrainNetwork();
                else if (i == 2)
                    game.ShowNetworkResults();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
            