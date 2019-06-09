using System;
using System.Collections;
using System.Collections.Generic;

namespace VirtualFileSystem.Toolbox
{
    internal sealed class ListTrackingChanges<T> : IList<T>
    {
        private readonly IList<T> _listToObserve;
        private Int64 _listVersion;

        public ListTrackingChanges(IList<T> listToObserve)
        {
            if (listToObserve == null) throw new ArgumentNullException("listToObserve");

            _listToObserve = listToObserve;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _listToObserve.GetEnumerator();
        }

        public void Add(T item)
        {
            _listToObserve.Add(item);
            this.IncrementVersion();
        }

        public void Clear()
        {
            _listToObserve.Clear();
            this.IncrementVersion();
        }

        public bool Contains(T item)
        {
            return _listToObserve.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _listToObserve.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            bool removedFine = _listToObserve.Remove(item);

            if (removedFine)
            {
                this.IncrementVersion();
            }

            return removedFine;
        }

        public int Count
        {
            get { return _listToObserve.Count; }
        }

        public bool IsReadOnly
        {
            get { return _listToObserve.IsReadOnly; }
        }

        public long ListVersion
        {
            get { return _listVersion; }
        }

        public int IndexOf(T item)
        {
            return _listToObserve.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _listToObserve.Insert(index, item);
            this.IncrementVersion();
        }

        public void RemoveAt(int index)
        {
            _listToObserve.RemoveAt(index);
            this.IncrementVersion();
        }

        public T this[int index]
        {
            get { return _listToObserve[index]; }
            set
            {
                _listToObserve[index] = value;
                this.IncrementVersion();
            }
        }

        private void IncrementVersion()
        {
            unchecked
            {
                _listVersion++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}