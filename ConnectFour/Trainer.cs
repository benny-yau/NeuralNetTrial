﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private Trainer() { }
        public Trainer(NetworkGenerator gui)
        {
            this.gui = gui;
            Network = gui.Network;
        }
        public Trainer(Network network)
        {
            Network = network;
        }

        /// <summary>
        /// Train.
        /// </summary>
        public void Train(Func<Board> regimen)
        {
            Network.TrainTime.Start();
            while (!Network.IsTrained && (gui == null || gui.Status != TrainStatus.Paused))
            {
                List<Example> trace = Simulator.Play(regimen(), Network);
                Network.TrainNetwork(trace);
                if (gui != null)
                    gui.Dispatcher.BeginInvoke(new Action<Network>(n => gui.UpdateProgress(n)), Network);
                else
                    Debug.WriteLine(Network.Termination.CurrentIteration.ToString());
            }
            Network.TrainTime.Stop();
        }

        /// <summary>
        /// Train with validation set.
        /// </summary>
        public void TrainWithValidationSet(GoBoard board, List<Example> validationSet)
        {
            String scenarioName = board.GameInfo.ScenarioName;
            if (validationSet.Count == 0) return;

            for (int i = 0; i <= validationSet.Count - 1; i++)
            {
                Example example = validationSet[i];
                Board b = new GoBoard((Go.Board)example.RootBoard);
                List<Example> trace = Simulator.Play(b, Network, example);
                Network.TrainNetwork(trace);
                if (i % 50 == 0) Debug.WriteLine("iter : " + (i + 1).ToString() + " out of " + validationSet.Count);
            }
        }
    }
}
