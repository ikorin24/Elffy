﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core.Timer
{
    internal class GameTimer : IGameTimer
    {
        private readonly Stopwatch _sw = new Stopwatch();

        internal GameTimer() { }

        public TimeSpan Elapsed => _sw.Elapsed;

        public long ElapsedMilliseconds => _sw.ElapsedMilliseconds;

        public bool IsRunning => _sw.IsRunning;

        public void Reset() => _sw.Reset();

        public void Start() => _sw.Start();

        public void Stop() => _sw.Stop();
    }
}