using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NeuralNet;
using MySerializer;
namespace ConnectFour.Properties
{
    internal sealed partial class Settings
    {
        // Your code goes here, safe from being overwritten by the IDE
        public System.Collections.Specialized.StringCollection NetworkPaths
        {
            get
            {
                if (NetworkPathsRaw == null)
                {
                    NetworkPathsRaw = new System.Collections.Specialized.StringCollection();
                    for (int i = 0; i <= 10; ++i)
                        NetworkPathsRaw.Add(null);
                }
                else
                {
                    for (int i = 0; i < NetworkPathsRaw.Count; ++i)
                        if (NetworkPathsRaw[i] != null && !File.Exists(NetworkPathsRaw[i]))
                            NetworkPathsRaw[i] = null;
                }
                return NetworkPathsRaw;
            }

            set
            {
                NetworkPathsRaw = value;
            }
        }

        public string CurrentNetworkPath 
        { 
            get { return NetworkPaths[Difficulty]; }
            set { NetworkPaths[Difficulty] = value; }
        }

        public Network[] Networks = new Network[11];

        public Network CurrentNetwork 
        {
            get 
            {
                if (NetworkPaths[Difficulty] == null)
                    return null;
                if (Networks[Difficulty] != null)
                    return Networks[Difficulty];
                if (NetworkPaths[Difficulty] != null)
                    return Networks[Difficulty] = (Network)Serializer.Deserialize(CurrentNetworkPath);
                return null;
            }
            set
            {
                Networks[Difficulty] = value;
            }
        }
    }
}
