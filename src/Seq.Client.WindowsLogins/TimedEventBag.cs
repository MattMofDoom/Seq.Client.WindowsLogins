using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

// ReSharper disable UnusedMember.Global

namespace Seq.Client.WindowsLogins
{
    public class TimedEventBag : IList<int>
    {
        private readonly Timer _timer;
        private volatile List<Tuple<DateTime, int>> _collection = new List<Tuple<DateTime, int>>();

        /// <summary>
        ///     Define a list that automatically remove expired objects.
        /// </summary>
        /// <param name="interval"></param>
        /// The interval at which the list test for old objects.
        /// <param name="expiration"></param>
        /// The TimeSpan an object stay valid inside the list.
        public TimedEventBag(int interval, TimeSpan expiration)
        {
            _timer = new Timer {Interval = interval};
            _timer.Elapsed += Tick;
            _timer.Start();

            Expiration = expiration;
        }

        public int Interval
        {
            get => (int) _timer.Interval;
            set => _timer.Interval = value;
        }

        private TimeSpan Expiration { get; }

        private void Tick(object sender, EventArgs e)
        {
            for (var i = _collection.Count - 1; i >= 0; i--)
                if (DateTime.Now - _collection[i].Item1 >= Expiration)
                    _collection.RemoveAt(i);
        }

        #region IList Implementation

        public int this[int index]
        {
            get => _collection[index].Item2;
            set => _collection[index] = new Tuple<DateTime, int>(DateTime.Now, value);
        }

        public IEnumerator<int> GetEnumerator()
        {
            return _collection.Select(x => x.Item2).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.Select(x => x.Item2).GetEnumerator();
        }

        public void Add(int item)
        {
            _collection.Add(new Tuple<DateTime, int>(DateTime.Now, item));
        }

        public int Count => _collection.Count;

        public bool IsSynchronized => false;

        public bool IsReadOnly => false;

        public void CopyTo(int[] array, int index)
        {
            for (var i = 0; i < _collection.Count; i++)
                array[i + index] = _collection[i].Item2;
        }

        public bool Remove(int item)
        {
            var contained = Contains(item);
            for (var i = _collection.Count - 1; i >= 0; i--)
                if (_collection[i].Item2 == item)
                    _collection.RemoveAt(i);
            return contained;
        }

        public void RemoveAt(int i)
        {
            _collection.RemoveAt(i);
        }

        public bool Contains(int item)
        {
            return _collection.Any(t => t.Item2 == item);
        }

        public void Insert(int index, int item)
        {
            _collection.Insert(index, new Tuple<DateTime, int>(DateTime.Now, item));
        }

        public int IndexOf(int item)
        {
            for (var i = 0; i < _collection.Count; i++)
                if (_collection[i].Item2 == item)
                    return i;

            return -1;
        }

        public void Clear()
        {
            _collection.Clear();
        }

        #endregion
    }
}