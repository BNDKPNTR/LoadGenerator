using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LoadGenerator.Metrics
{
    class SysPerfLogger
    {
        private readonly Dictionary<DateTime, TimeSpan> _processorUsages;
        private readonly Dictionary<DateTime, GCCollectionCount> _GCCollectionCounts;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public TimeSpan LoggingInterval => TimeSpan.FromMilliseconds(200);
        public DateTime ProcessStartTime => Process.GetCurrentProcess().StartTime;
        public IReadOnlyDictionary<DateTime, TimeSpan> ProcessorUsages => _processorUsages;
        public IReadOnlyDictionary<DateTime, GCCollectionCount> GCCollectionCounts => _GCCollectionCounts;

        public SysPerfLogger(TimeSpan duration)
        {
            var capacity = (int)(duration.TotalMilliseconds / LoggingInterval.TotalMilliseconds * 1.1);
            _processorUsages = new Dictionary<DateTime, TimeSpan>(capacity);
            _GCCollectionCounts = new Dictionary<DateTime, GCCollectionCount>(capacity);
            StartLogging();
        }

        private void StartLogging()
        {
            Task.Run(async () =>
            {
                var process = Process.GetCurrentProcess();

                while (true)
                {
                    var now = DateTime.UtcNow;
                    _processorUsages.Add(now, process.TotalProcessorTime);
                    _GCCollectionCounts.Add(now, new GCCollectionCount(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)));
                    await Task.Delay(LoggingInterval);

                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }
                }
            });
        }

        public void StopLogging()
        {
            _cts.Cancel();
        }
    }
}
