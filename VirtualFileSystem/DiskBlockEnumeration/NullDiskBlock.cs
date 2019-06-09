using System;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.DiskBlockEnumeration
{
    internal class NullDiskBlock : IDiskBlock
    {
        public int OccupiedSpaceInBytes
        {
            get
            {
                return 0;
            }
        }

        public int FreeSpaceInBytes
        {
            get { return 0; }
        }

        public int NumberOfBytesCanBeWrittenAtCurrentPosition
        {
            get { return 0; }
        }

        public bool CanAcceptBytesAtCurrentPosition
        {
            get { return false; }
        }

        public int SizeInBytes
        {
            get { return 0; }
        }

        public int Position
        {
            get { return EnumeratorAddressable<string>.IndexOfPositionBeforeFirstElement; }
            set { throw new NotSupportedException(); }
        }

        public byte[] ReadAll()
        {
            throw new NotSupportedException();
        }

        public void WriteBytes(byte[] array, int startingPosition, int length)
        {
            throw new NotSupportedException();
        }

        public bool IsNull
        {
            get { return true; }
        }

        public bool IsAtEndOfBlock
        {
            get { return false; }
        }

        public bool IsAtEndOfReadableData
        {
            get { return false; }
        }

        public int BlockIndex
        {
            get { throw new NotSupportedException(); }
        }
    }
}