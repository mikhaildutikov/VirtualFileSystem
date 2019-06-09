using System;
using VirtualFileSystem.Addressing;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskBlockEnumeration;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Streaming
{
    internal class DataStreamStructureBuilderImmutable : IDataStreamStructureBuilder
    {
        private readonly IVirtualDisk _disk;
        private readonly DataStreamDefinition _dataStreamDefinition;
        private readonly AddressingSystemParameters _addressingSystemParameters;
        private readonly IFileSystemNodeStorage _fileSystemNodeStorage;
        private readonly Node _governingNode;
        private readonly IntegerListConstrained _doubleIndirectBlocks;
        private readonly AddressingBlockSizesCalculator _addressingBlockSizesCalculator;
        private readonly AddressingSystemBlockSizes _sizesOfLastAddressingBlocks;

        public DataStreamStructureBuilderImmutable(IVirtualDisk disk, DataStreamDefinition dataStreamDefinition, AddressingSystemParameters addressingSystemParameters, IFileSystemNodeStorage fileSystemNodeStorage, Node governingNode)
        {
            if (disk == null) throw new ArgumentNullException("disk");
            if (dataStreamDefinition == null) throw new ArgumentNullException("dataStreamDefinition");
            if (addressingSystemParameters == null) throw new ArgumentNullException("addressingSystemParameters");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");
            if (governingNode == null) throw new ArgumentNullException("governingNode");

            _disk = disk;
            _governingNode = governingNode;
            _fileSystemNodeStorage = fileSystemNodeStorage;
            _addressingSystemParameters = addressingSystemParameters;
            _dataStreamDefinition = dataStreamDefinition;

            int numberOfBlocks =
                SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(
                    dataStreamDefinition.StreamLengthInBytes, addressingSystemParameters.BlockSize);

            _addressingBlockSizesCalculator = new AddressingBlockSizesCalculator(addressingSystemParameters.IndirectBlockReferencesCountInDoubleIndirectBlock,
                                                                addressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock);

            _sizesOfLastAddressingBlocks = AddressingBlockSizesCalculator.GetSizesOfAddressingBlocksSufficientToStoreItems(numberOfBlocks);

            int doubleIndirectBlockSize = _sizesOfLastAddressingBlocks.DoubleIndirectBlockSize;

            _doubleIndirectBlocks = new IntegerListConstrained(disk.ReadAllBytesFromBlock(dataStreamDefinition.ContentsBlockIndex), doubleIndirectBlockSize, _addressingSystemParameters.IndirectBlockReferencesCountInDoubleIndirectBlock);

            CalculateAndSetMaximumSize(addressingSystemParameters);
        }

        private void CalculateAndSetMaximumSize(AddressingSystemParameters addressingSystemParameters)
        {
            var maximumSize = new DoubleIndirectionCompositeIndex(
                0,
                addressingSystemParameters.IndirectBlockReferencesCountInDoubleIndirectBlock,
                addressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock,
                addressingSystemParameters.BlockSize);

            this.MaximumSize = maximumSize.MaxValue;
        }

        public int CurrentSize
        {
            get { return DataStreamDefinition.StreamLengthInBytes; }
        }

        public int MaximumSize { get; private set; }

        protected IVirtualDisk Disk
        {
            get { return _disk; }
        }

        protected DataStreamDefinition DataStreamDefinition
        {
            get { return _dataStreamDefinition; }
        }

        protected AddressingSystemParameters AddressingSystemParameters
        {
            get { return _addressingSystemParameters; }
        }

        protected IFileSystemNodeStorage FileSystemNodeStorage
        {
            get { return _fileSystemNodeStorage; }
        }

        protected Node GoverningNode
        {
            get { return _governingNode; }
        }

        protected IntegerListConstrained DoubleIndirectBlocks
        {
            get { return _doubleIndirectBlocks; }
        }

        protected AddressingBlockSizesCalculator AddressingBlockSizesCalculator
        {
            get { return _addressingBlockSizesCalculator; }
        }

        public virtual void SetSize(int newSize)
        {
            throw new NotSupportedException();
        }

        public IDoubleIndirectDataStreamEnumerator CreateEnumerator()
        {
            if (this.CurrentSize == 0)
            {
                return new EmptyDiskBlockEnumerator();
            }

            var enumerator = new NonEmptyDiskBlockEnumerator(Disk, DataStreamDefinition, AddressingSystemParameters, FileSystemNodeStorage, GoverningNode, DoubleIndirectBlocks.GetAddressableEnumerator());

            enumerator.MoveNext();

            return enumerator;
        }
    }
}