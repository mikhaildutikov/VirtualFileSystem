using System;
using System.Collections;
using System.Collections.Generic;

namespace VirtualFileSystem.Toolbox
{
    internal class IntegerListToIListAdapter : IList<int>
    {
        private readonly IntegerListConstrained _listConstrained;

        public IntegerListToIListAdapter(IntegerListConstrained listConstrained)
        {
            if (listConstrained == null) throw new ArgumentNullException("listConstrained");

            _listConstrained = listConstrained;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return _listConstrained.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(int item)
        {
            _listConstrained.AddInteger(item);
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(int item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(int item)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return _listConstrained.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(int item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, int item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public int this[int index]
        {
            get { return _listConstrained[index]; }
            set { _listConstrained[index] = value; }
        }
    }
}