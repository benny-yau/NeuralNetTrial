using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuralNet
{
    /// <summary>
    /// Tries to emulate System.Diagnostics.StopWatch, but implemented to be serializable.
    /// </summary>
    [Serializable]
    public class NetworkTimer
    {
        private TimeSpan _elapsed = TimeSpan.Zero;
        public TimeSpan Elapsed
        {
            get { return _elapsed + (_start != DateTime.MinValue ? DateTime.Now.Subtract(_start) : TimeSpan.Zero); }
            set { _elapsed = value; }
        }
        private DateTime _start = DateTime.MinValue;
        public void Start()
        {
            if (_start == DateTime.MinValue)
                _start = DateTime.Now;
        }

        public void Stop()
        {
            if (_start == DateTime.MinValue)
                return;
            Elapsed += DateTime.Now - _start;
            _start = DateTime.MinValue;
        }

        public void Reset()
        {
            _elapsed = TimeSpan.Zero;
            _start = DateTime.MinValue;
        }
    }
}
