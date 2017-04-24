using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LoadGenerator.Metrics
{
    class ThreadLocalValue<T> : IEnumerable<T>
    {
        private readonly T[] _values;

        public T Value
        {
            get { return _values[Thread.CurrentThread.ManagedThreadId]; }
            set { _values[Thread.CurrentThread.ManagedThreadId] = value; }
        }

        public ThreadLocalValue(int maxThreadId)
        {
            _values = new T[maxThreadId + 1];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _values.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
