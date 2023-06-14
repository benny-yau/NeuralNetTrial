using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuralNet;

namespace ConnectFour
{
    public class Trainer
    {
        Simulator Simulator = new Simulator();
        NetworkGenerator gui;
        Network Network;

        private Trainer(){}
        public Trainer(NetworkGenerator gui)
        {
            this.gui = gui;
            Network = gui.Network;
        }
        public Trainer(Network network)
        {
            Network = network;
        }

        public void Train(Func<Board> regimen)
        {
            Network.TrainTime.Start();
            while (!Network.IsTrained && (gui == null || gui.Status != TrainStatus.Paused))
            {
                List<Example> trace = Simulator.Play(regimen(), Network);
                Network.TrainNetwork(trace);
                if (gui != null)
                    gui.Dispatcher.BeginInvoke(new Action<Network>(n => gui.UpdateProgress(n)), Network);
            }
            Network.TrainTime.Stop();
        }

    }
}
