using System;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.DiskBlockEnumeration
{
    internal class DiskBlockNodeUpdating : IDiskBlock
    {
        private readonly IDiskBlock _diskBlockWrapped;
        private readonly DataStreamDefinition _owningStreamDefinition;
        private readonly Node _blockOwningNode;
        private readonly IFileSystemNodeStorage _fileSystemNodeStorage;

        public DiskBlockNodeUpdating(IDiskBlock diskBlockWrapped, DataStreamDefinition owningStreamDefinition, Node blockOwningNode, IFileSystemNodeStorage fileSystemNodeStorage)
        {
            if (diskBlockWrapped == null) throw new ArgumentNullException("diskBlockWrapped");
            if (owningStreamDefinition == null) throw new ArgumentNullException("owningStreamDefinition");
            if (blockOwningNode == null) throw new ArgumentNullException("blockOwningNode");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");

            _diskBlockWrapped = diskBlockWrapped;
            _owningStreamDefinition = owningStreamDefinition;
            _blockOwningNode = blockOwningNode;
            _fileSystemNodeStorage = fileSystemNodeStorage;
        }

        public byte[] ReadAll()
        {
            return _diskBlockWrapped.ReadAll();
        }

        public void WriteBytes(byte[] array, int startingPosition, int length)
        {
            int freeSpaceBeforeWriting = this.FreeSpaceInBytes;

            _diskBlockWrapped.WriteBytes(array, startingPosition, length);

            int freeSpaceAfterWriting = this.FreeSpaceInBytes;

            var delta = (freeSpaceBeforeWriting - freeSpaceAfterWriting);

            if (delta > 0)
            {
                _owningStreamDefinition.StreamLengthInBytes += delta;
                _fileSystemNodeStorage.WriteNode(_blockOwningNode);
            }
        }

        public bool IsNull
        {
            get { return _diskBlockWrapped.IsNull; }
        }

        public bool IsAtEndOfBlock
        {
            get { return _diskBlockWrapped.IsAtEndOfBlock; }
        }

        public bool IsAtEndOfReadableData
        {
            get { return _diskBlockWrapped.IsAtEndOfReadableData; }
        }

        public int BlockIndex
        {
            get { return _diskBlockWrapped.BlockIndex; }
        }

        public int Position
        {
            get { return _diskBlockWrapped.Position; }
            set { _diskBlockWrapped.Position = value; }
        }

        public int SizeInBytes
        {
            get { return _diskBlockWrapped.SizeInBytes; }
        }

        public bool CanAcceptBytesAtCurrentPosition
        {
            get { return _diskBlockWrapped.CanAcceptBytesAtCurrentPosition; }
        }

        public int NumberOfBytesCanBeWrittenAtCurrentPosition
        {
            get { return _diskBlockWrapped.NumberOfBytesCanBeWrittenAtCurrentPosition; }
        }

        public int FreeSpaceInBytes
        {
            get { return _diskBlockWrapped.FreeSpaceInBytes; }
        }

        public int OccupiedSpaceInBytes
        {
            get { return _diskBlockWrapped.OccupiedSpaceInBytes; }
        }
    }
}