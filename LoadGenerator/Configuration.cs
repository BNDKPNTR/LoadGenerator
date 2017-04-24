using System;

namespace LoadGenerator
{
    struct Configuration
    {
        public int ThreadCount { get; }
        public TimeSpan Duration { get; }
        public Uri Uri { get; }
        public int NumOfMeasuredThreads { get; }
        public int MetricsCollectionCapacity { get; }

        public Configuration(int threadCount, TimeSpan duration, Uri uri, int numOfMeasuredThreads, int metricsCollectionCapacity)
        {
            ThreadCount = threadCount;
            Duration = duration;
            Uri = uri;
            NumOfMeasuredThreads = numOfMeasuredThreads;
            MetricsCollectionCapacity = metricsCollectionCapacity;
        }
    }
}
