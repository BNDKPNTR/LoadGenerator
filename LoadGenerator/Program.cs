using System;
using System.Collections.Generic;

namespace LoadGenerator
{
    class Program
    {
        private const string THREAD_COUNT = "-t";
        private const string DURATION = "-d";
        private const string URI = "-u";
        private const string NUM_OF_MEASURED_THREADS = "-m";
        private const string METRICS_COLLECTION_CAPACITY = "-c";

        static void Main(string[] args)
        {
            var parsedArgs = ParseArgs(args);
            var config = GetOrSetDefaultConfiguration(parsedArgs);
            Console.WriteLine($"{"Thread count: ", -20}{config.ThreadCount}");
            Console.WriteLine($"{"Duration: ", -20}{config.Duration}");
            Console.WriteLine($"{"Uri: ", -20}{config.Uri.OriginalString}");
            Console.WriteLine();

            var loadGenerator = new LoadGenerator(
                config.ThreadCount, 
                config.Duration, 
                config.Uri, 
                config.NumOfMeasuredThreads, 
                config.MetricsCollectionCapacity);

            loadGenerator.Start();

            Console.WriteLine(loadGenerator.GetFormattedResults());
#if DEBUG
            Console.ReadKey();
#endif
        }

        private static Configuration GetOrSetDefaultConfiguration(IDictionary<string, string> configuration)
        {
            if (!int.TryParse(configuration[THREAD_COUNT], out var threadCount))
            {
                threadCount = 4;
            }

            if (!double.TryParse(configuration[DURATION], out var durationInSec))
            {
                durationInSec = 30;
            }
            var duration = TimeSpan.FromSeconds(durationInSec);

            var uriString = !string.IsNullOrEmpty(configuration[URI]) ? configuration[URI] : "http://localhost:5000/json";
            var uri = new Uri(uriString);

            if (!int.TryParse(configuration[NUM_OF_MEASURED_THREADS], out var numOfMeasuredThreads))
            {
                numOfMeasuredThreads = 32;
            }

            if (!int.TryParse(configuration[METRICS_COLLECTION_CAPACITY], out var metricsCollectionCapacity))
            {
                metricsCollectionCapacity = 300_000;
            }

            return new Configuration(threadCount, duration, uri, numOfMeasuredThreads, metricsCollectionCapacity);
        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            var result = new Dictionary<string, string>();

            if (args.Length % 2 == 0)
            {
                for (int i = 0; i < args.Length; i += 2)
                {
                    result.Add(args[i], args[i + 1]);
                }
            }
            
            if (!result.ContainsKey(THREAD_COUNT)) result.Add(THREAD_COUNT, string.Empty);
            if (!result.ContainsKey(DURATION)) result.Add(DURATION, string.Empty);
            if (!result.ContainsKey(URI)) result.Add(URI, string.Empty);
            if (!result.ContainsKey(NUM_OF_MEASURED_THREADS)) result.Add(NUM_OF_MEASURED_THREADS, string.Empty);
            if (!result.ContainsKey(METRICS_COLLECTION_CAPACITY)) result.Add(METRICS_COLLECTION_CAPACITY, string.Empty);

            return result;
        }
    }
}