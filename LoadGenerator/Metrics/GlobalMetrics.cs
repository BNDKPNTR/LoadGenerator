using System;
using System.Linq;

namespace LoadGenerator.Metrics
{
    class GlobalMetrics
    {
        private readonly SysPerfLogger _sysPerfLogger;
        private readonly ThreadLocalValue<TimeSpan> _connectionTimes;
        private readonly MetricsCollection<TimeSpan> _requestDurations;
        private readonly MetricsCollection<DateTime> _responseDates;
        private readonly MetricsCollection<TimeSpan> _responseDurations;
        private readonly MetricsCollection<TimeSpan> _totalDurations;
        private readonly ThreadLocalValue<int> _successStatusCodeCounter;
        private readonly ThreadLocalValue<int> _requestCounter;

        public GlobalMetrics(int maxThreadId, int maxMeasuredThreadId, SysPerfLogger sysPerfLogger, int metricsCollectionCapacity)
        {
            _sysPerfLogger = sysPerfLogger;

            _connectionTimes = new ThreadLocalValue<TimeSpan>(maxThreadId);
            _requestDurations = new MetricsCollection<TimeSpan>(maxMeasuredThreadId, metricsCollectionCapacity);
            _responseDates = new MetricsCollection<DateTime>(maxMeasuredThreadId, metricsCollectionCapacity);
            _responseDurations = new MetricsCollection<TimeSpan>(maxMeasuredThreadId, metricsCollectionCapacity);
            _totalDurations = new MetricsCollection<TimeSpan>(maxMeasuredThreadId, metricsCollectionCapacity);
            _successStatusCodeCounter = new ThreadLocalValue<int>(maxThreadId);
            _requestCounter = new ThreadLocalValue<int>(maxThreadId);
        }

        public void AddConnectionTimeMetrics(TimeSpan duration)
        {
            _connectionTimes.Value = duration;
        }

        public void AddRequestMetrics(TimeSpan duration)
        {
            _requestDurations.Add(duration);
        }

        public void AddResponseMetrics(DateTime date, TimeSpan duration)
        {
            _responseDates.Add(date);
            _responseDurations.Add(duration);
        }

        public void AddStatusCode(bool isSuccessStatusCode)
        {
            if (isSuccessStatusCode)
            {
                _successStatusCodeCounter.Value++;
            }
        }

        public void IncreaseRequestCounter()
        {
            _requestCounter.Value++;
        }

        public void AddTotalMetrics(TimeSpan duration)
        {
            _totalDurations.Add(duration);
        }

        public string GetFormattedMetrics(DateTime startDate)
        {
            _sysPerfLogger.StopLogging();
            var GCCollectionCount = GetGCCollectionCount();
            return $"{"Conn. Average", -20}{_connectionTimes.Average(t => t.TotalMilliseconds):0.0000} ms" + Environment.NewLine
                + $"{"Req. Average: ", -20}{_requestDurations.Average(t => t.TotalMilliseconds):0.0000} ms" + Environment.NewLine
                + $"{"Resp. Average: ", -20}{_responseDurations.Average(t => t.TotalMilliseconds):0.0000} ms" + Environment.NewLine
                + $"{"Total Average: ", -20}{_totalDurations.Average(t => t.TotalMilliseconds):0.0000} ms" + Environment.NewLine
                + $"{"Req/sec: ", -20}{Math.Round(GetRequestsPerSec(startDate), 0)}" + Environment.NewLine
                + $"{"Errors: ", -20}{GetErrorsCount()}" + Environment.NewLine
                + $"{"Processor usage:", -20}{GetProcessorUsage():00.00} %" + Environment.NewLine
                + $"Gen 0: {GCCollectionCount.Gen0}, Gen 1: {GCCollectionCount.Gen1}, Gen 2: {GCCollectionCount.Gen2}";
        }

        private double GetRequestsPerSec(DateTime startDate)
        {
            TimeSpan totalDuration = _responseDates.Max() - startDate;
            var totalRequests = _requestCounter.Aggregate((x, y) => x + y);
            return totalRequests / totalDuration.TotalSeconds;
        }

        private int GetErrorsCount()
        {
            var totalRequests = _requestCounter.Aggregate((x, y) => x + y);
            var totalSuccesRequests = _successStatusCodeCounter.Aggregate((x, y) => x + y);

            return totalRequests - totalSuccesRequests;
        }

        private double GetProcessorUsage()
        {
            if (_responseDates.Count == 0) return 0;

            var firstMetricDate = _responseDates.Min();
            var lastMetricDate = _responseDates.Max();
            var testDuration = lastMetricDate - firstMetricDate;
            var processorUsages = _sysPerfLogger.ProcessorUsages
                .Where(x => x.Key >= firstMetricDate && x.Key <= lastMetricDate)
                .OrderBy(x => x.Key);

            var processorUsage = processorUsages.Last().Value - processorUsages.First().Value;

            return processorUsage.TotalMilliseconds / Environment.ProcessorCount / testDuration.TotalMilliseconds * 100;
        }

        private GCCollectionCount GetGCCollectionCount()
        {
            if (_responseDates.Count == 0) return new GCCollectionCount(0, 0, 0);

            var firstMetricDate = _responseDates.Min();
            var lastMetricDate = _responseDates.Max();
            var GCCollectionCounts = _sysPerfLogger.GCCollectionCounts
                .Where(x => x.Key >= firstMetricDate && x.Key <= lastMetricDate)
                .OrderBy(x => x.Key);

            var firstGCCollection = GCCollectionCounts.First().Value;
            var lastGCCollection = GCCollectionCounts.Last().Value;

            return new GCCollectionCount(
                lastGCCollection.Gen0 - firstGCCollection.Gen0,
                lastGCCollection.Gen1 - firstGCCollection.Gen1,
                lastGCCollection.Gen2 - firstGCCollection.Gen2);
        }
    }
}
