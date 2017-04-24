using LoadGenerator.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LoadGenerator
{
    class LoadGenerator
    {
        private readonly TimeSpan _duration;
        private readonly TcpWorkerHelper _tcpWorkerHelper;
        private readonly List<Thread> _threads;
        private readonly List<ManualResetEvent> _signals;
        private readonly GlobalMetrics _metrics;

        private DateTime _startDate;

        public LoadGenerator(
            int threadCount, 
            TimeSpan duration, 
            Uri uri, 
            int numOfMeasuredThreads, 
            int metricsCollectionCapacity)
        {
            _duration = duration;
            _tcpWorkerHelper = new TcpWorkerHelper(uri);

            _signals = Enumerable.Range(0, threadCount).Select(i => new ManualResetEvent(false)).ToList();
            _threads = Enumerable.Range(0, threadCount).Select(i =>
            {
                var method = i < numOfMeasuredThreads ?
                      new ParameterizedThreadStart(Work_WithMetrics)
                    : new ParameterizedThreadStart(Work_WithoutMetrics);

                return new Thread(method) { IsBackground = true };
            }).ToList();

            var maxMeasuredThread = _threads.Count < numOfMeasuredThreads ? 
                  _threads.Last() 
                : _threads[numOfMeasuredThreads - 1];

            _metrics = new GlobalMetrics(
                _threads.Max(t => t.ManagedThreadId), 
                maxMeasuredThread.ManagedThreadId, 
                new SysPerfLogger(duration), 
                metricsCollectionCapacity);
        }

        public void Start()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _startDate = DateTime.UtcNow;

            for (int i = 0; i < _threads.Count; i++)
            {
                _threads[i].Start(_signals[i]);
            }

            var timeout = _duration.Add(TimeSpan.FromSeconds(5));
            for (int i = 0; i < _signals.Count; i += 64)
            {
                //max 64 WaitHandle can be passed to WaitAll
                WaitHandle.WaitAll(_signals.Skip(i).Take(64).ToArray(), timeout);
            }
        }

        public string GetFormattedResults()
        {
            return _metrics.GetFormattedMetrics(_startDate);
        }

        private void Work_WithMetrics(object arg)
        {
            var signal = (ManualResetEvent)arg;

            using (var tcpWorker = new TcpWorker(_tcpWorkerHelper))
            {
                var sw = Stopwatch.StartNew();
                tcpWorker.Connect();
                _metrics.AddConnectionTimeMetrics(sw.Elapsed);

                while (DateTime.UtcNow - _startDate < _duration)
                {
                    sw.Restart();
                    tcpWorker.Send();
                    var requestTime = sw.Elapsed;
                    _metrics.AddRequestMetrics(requestTime);
                    var statusIsOK = tcpWorker.ResponseArrivedWithOK();
                    _metrics.AddResponseMetrics(DateTime.UtcNow, sw.Elapsed - requestTime);
                    _metrics.AddTotalMetrics(sw.Elapsed);
                    _metrics.IncreaseRequestCounter();
                    _metrics.AddStatusCode(statusIsOK);
                }
            }

            signal.Set();
        }

        private void Work_WithoutMetrics(object arg)
        {
            var signal = (ManualResetEvent)arg;

            using (var tcpWorker = new TcpWorker(_tcpWorkerHelper))
            {
                var sw = Stopwatch.StartNew();
                tcpWorker.Connect();
                _metrics.AddConnectionTimeMetrics(sw.Elapsed);

                while (DateTime.UtcNow - _startDate < _duration)
                {
                    tcpWorker.Send();
                    var statusIsOK = tcpWorker.ResponseArrivedWithOK();
                    _metrics.IncreaseRequestCounter();
                    _metrics.AddStatusCode(statusIsOK);
                }
            }

            signal.Set();
        }
    }
}
