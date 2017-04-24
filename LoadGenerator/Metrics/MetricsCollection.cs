using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LoadGenerator.Metrics
{
    class MetricsCollection<T> : IEnumerable<T>
    {
        private readonly List<T>[] _collection;

        public int Count => _collection.Sum(x => x.Count);

        public MetricsCollection(int maxThreadId, int initialCapacity)
        {
            _collection = new List<T>[maxThreadId + 1];

            for (int i = 0; i < _collection.Length; i++)
            {
                _collection[i] = new List<T>(initialCapacity);
            }
        }

        public void Add(T item)
        {
            _collection[Thread.CurrentThread.ManagedThreadId].Add(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _collection
                .SelectMany(x => x)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
