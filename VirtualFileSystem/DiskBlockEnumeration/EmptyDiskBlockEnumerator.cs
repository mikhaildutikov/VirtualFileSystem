using System;
using System.Collections;
using VirtualFileSystem.Addressing;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.DiskBlockEnumeration
{
    internal class EmptyDiskBlockEnumerator : IDoubleIndirectDataStreamEnumerator
    {
        private readonly IDiskBlock _current;

        public EmptyDiskBlockEnumerator()
        {
            _current = new NullDiskBlock();
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }

        public IDiskBlock Current
        {
            get { return _current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public void SetPosition(int newPosition)
        {
            if (newPosition != EnumeratorAddressable<IDiskBlock>.IndexOfPositionBeforeFirstElement)
            {
                throw new InvalidOperationException("Поток не содержит данных");
            }
        }

        public int Position
        {
            get { return EnumeratorAddressable<IDiskBlock>.IndexOfPositionBeforeFirstElement; }
        }

        public bool IsAtLastElement
        {
            get { return true; }
        }

        public int Count
        {
            get { return 0; }
        }

        public bool IsEmpty
        {
            get
            {
                return true;
            }
        }
    }
}