#nullable enable
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace Elffy.Core.Timer
{
    internal class DebugGameTimer : IGameTimer
    {
        private readonly TimeSpan THRESHOLD = TimeSpan.FromMilliseconds(50);
        private TimeSpan _baseTime = TimeSpan.Zero;
        private TimeSpan _time;
        private readonly Stopwatch _watch = new Stopwatch();
        private Task? _task;
        private CancellationTokenSource? _tokenSource;

        public TimeSpan Elapsed
        {
            get
            {
                Refresh();
                return _watch.Elapsed + _baseTime;
            }
        }

        public long ElapsedMilliseconds => (long)Elapsed.TotalMilliseconds;

        public bool IsRunning { get; private set; }

        internal DebugGameTimer() { }

        public void Start()
        {
            if(IsRunning) { return; }
            IsRunning = true;
            _watch.Start();
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            _task = Task.Factory.StartNew(() => {
                while(true) {
                    if(token.IsCancellationRequested) { return; }
                    Refresh();
                    _time = _watch.Elapsed;
                    Thread.Sleep(1);
                }
            }, token);
        }

        public void Stop()
        {
            if(!IsRunning) { return; }
            IsRunning = false;
            _watch.Stop();
            _tokenSource!.Cancel();
            try {
                _task!.Wait();
            }
            finally {
                _task = null;
                _tokenSource = null;
            }
        }

        public void Reset()
        {
            _watch.Reset();
            _baseTime = TimeSpan.Zero;
            _time = TimeSpan.Zero;
        }

        private void Refresh()
        {
            var diff = _watch.Elapsed - _time;
            if(diff > THRESHOLD) {
                _baseTime += _time;
                _watch.Stop();
                _watch.Reset();
                _watch.Start();
            }
        }
    }
}
