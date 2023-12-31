﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows;

using MySerializer;

namespace NeuralNet
{
    public enum TerminationType { ByIteration, ByValidationSet }

    /// <summary>
    /// Encapsulates a set of termination criteria to complete networking propogation.
    /// </summary>
    [Serializable]
    public class Termination
    {
        public TerminationType Type;

        public int CurrentIteration;
        public int TotalIterations;
        public int TargetIterations;

        [NonSerialized]
        public List<Example> ValidationSet;
        public int ValidateCycle;
        string Snapshot; // Snapshot of network that had best validation error thus far.
        double SnapshotError = double.MaxValue;
        public Network Network;

        private Termination() { }
        public static Termination ByIteration(int iterations)
        {
            if (!(iterations > 0)) throw new Exception("Iterations > 0");
            return new Termination() { Type = TerminationType.ByIteration, TargetIterations = iterations };
        }

        public static Termination ByValidationSet(List<Example> validationSet, int validateCycle)
        {
            if (!(validateCycle > 0 && validationSet != null && validationSet.Count > 0)) throw new Exception("Invalid validation set provided.");
            return new Termination() { Type = TerminationType.ByValidationSet, ValidationSet = validationSet, ValidateCycle = validateCycle };
        }

        public void CompleteIteration()
        {
            // Check if we need to run through the validation set.
            if (Type == TerminationType.ByValidationSet && TotalIterations % ValidateCycle == 0)
            {
                Debug.WriteLine("Validating...");
                double error = Validate();
                Network.ErrorHistory.Add(new Point(CurrentIteration, error));
                //if (error < SnapshotError)
                {
                    Debug.WriteLine("Serializing network...");
                    Network.TrueError = SnapshotError = error;
                    if (!Directory.Exists(Network.Name))
                        Directory.CreateDirectory(Network.Name);
                    Snapshot = Path.Combine(Network.Name, Network.Name + "_" + TotalIterations.ToString() + "_" + error.ToString()) + ".net";
                    Network.TrainTime.Stop();
                    Serializer.Serialize(Network, Snapshot);
                    Network.TrainTime.Start();
                }
            }
            ++CurrentIteration;
            ++TotalIterations;
        }

        /// <summary>
        /// Tests a network by first computing a prediction (foward feeding), then comparing these predictions to the correct labels of examples in the validation set.
        /// </summary>
        /// <returns>Root mean squared error of all predictions from labels</returns>
        public double Validate()
        {
            double meanSquaredError = 0;
            double n = 0;
            foreach (Example example in ValidationSet.Take(8000))
            {
                Network.PropogateInput(example);
                for (int i = 0; i < example.Predictions.Count; ++i)
                {
                    meanSquaredError += Math.Pow((example.Predictions[i] - example.Labels[i]), 2);
                    ++n;
                }
            }
            meanSquaredError /= n;
            return Math.Sqrt(meanSquaredError);
        }

        public bool IsNetworkTrained
        {
            get
            {
                if (Type == TerminationType.ByIteration)
                {
                    if (CurrentIteration == TotalIterations)
                        return true;
                    return false;
                }
                else if (Type == TerminationType.ByValidationSet)
                {
                    //throw new NotImplementedException(); Go forever.... until user manually stops.  
                }
                return false;
            }

        }
    }
}
