using System;
using VirtualFileSystem.Addressing;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.DiskBlockEnumeration
{
    /// <summary>
    /// Note: обобщается на адресацию с любым количеством уровней косвенности, но я не успеваю это сделать (и не думаю, что стоит делать в рамках тестового задания).
    /// </summary>
    internal class NonEmptyDiskBlockEnumerator : IDoubleIndirectDataStreamEnumerator
    {
        private EnumeratorAddressable<int> _singlyIndirectReferencesEnumerator;
        private IntegerListConstrained _singlyIndirectBlockReferences;
        private readonly IVirtualDisk _disk;
        private readonly DataStreamDefinition _dataStreamDefinition;
        private readonly AddressingSystemParameters _addressingSystemParameters;
        private readonly IFileSystemNodeStorage _fileSystemNodeStorage;
        private readonly Node _streamOwningNode;
        private readonly EnumeratorAddressable<int> _doubleIndirectListEnumerator;
        private readonly AddressingSystemBlockSizes _addressingSystemSizesOfLastBlocks;
        private int _position; // номер блока.
        private IDiskBlock _current;
        private readonly int _numberOfBlocks;
        private readonly AddressingBlockSizesCalculator _blockSizesCalculator;

        private const int PositionBeforeFirstElement = EnumeratorAddressable<IDiskBlock>.IndexOfPositionBeforeFirstElement;

        public NonEmptyDiskBlockEnumerator(
            IVirtualDisk disk,
            DataStreamDefinition dataStreamDefinition,
            AddressingSystemParameters addressingSystemParameters,
            IFileSystemNodeStorage fileSystemNodeStorage,
            Node streamOwningNode,
            EnumeratorAddressable<int> doubleIndirectListEnumerator)
        {
            if (disk == null) throw new ArgumentNullException("disk");
            if (dataStreamDefinition == null) throw new ArgumentNullException("dataStreamDefinition");
            if (addressingSystemParameters == null) throw new ArgumentNullException("addressingSystemParameters");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");
            if (streamOwningNode == null) throw new ArgumentNullException("streamOwningNode");
            if (doubleIndirectListEnumerator == null) throw new ArgumentNullException("doubleIndirectListEnumerator");

            _disk = disk;
            _dataStreamDefinition = dataStreamDefinition;
            _addressingSystemParameters = addressingSystemParameters;
            _fileSystemNodeStorage = fileSystemNodeStorage;
            _streamOwningNode = streamOwningNode;
            _doubleIndirectListEnumerator = doubleIndirectListEnumerator;

            if (doubleIndirectListEnumerator.Count == 0)
            {
                throw new ArgumentException("doubleIndirectListEnumerator");
            }

            _numberOfBlocks =
                SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(_dataStreamDefinition.StreamLengthInBytes, _disk.BlockSizeInBytes);

            _blockSizesCalculator = new AddressingBlockSizesCalculator(addressingSystemParameters.IndirectBlockReferencesCountInDoubleIndirectBlock,
                                                                addressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock);

            _addressingSystemSizesOfLastBlocks = _blockSizesCalculator.GetSizesOfAddressingBlocksSufficientToStoreItems(_numberOfBlocks);

            this.SetFirstSingleIndirectBlock();

            _current = new NullDiskBlock();

            _position = PositionBeforeFirstElement;
        }

        private void SetFirstSingleIndirectBlock()
        {
            int firstSingleIndirectBlockSize = _doubleIndirectListEnumerator.Count > 1
                                                  ? _addressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock
                                                  : _addressingSystemSizesOfLastBlocks.LastSingleIndirectBlockSize;

            _doubleIndirectListEnumerator.SetPosition(0);

            this.InitializeSingleIndirectionBlock(_doubleIndirectListEnumerator.Current, firstSingleIndirectBlockSize, PositionBeforeFirstElement);

            _doubleIndirectListEnumerator.Reset();
        }

        public bool IsEmpty
        {
            get { return false; }
        }

        private void InitializeSingleIndirectionBlock(int indexOfBlockToCreateIndirectionBlockFrom, int currentCountOfIntegersInBlock, int currentPositionInBlock)
        {
            byte[] blockContents = _disk.ReadAllBytesFromBlock(indexOfBlockToCreateIndirectionBlockFrom);

            _singlyIndirectBlockReferences = new IntegerListConstrained(blockContents, currentCountOfIntegersInBlock, _addressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock);
            _singlyIndirectReferencesEnumerator = _singlyIndirectBlockReferences.GetAddressableEnumerator();

            if (currentPositionInBlock != PositionBeforeFirstElement)
            {
                _singlyIndirectReferencesEnumerator.SetPosition(currentPositionInBlock);
            }
        }

        /// <summary>
        /// Устанавливает в качестве текущего блок заданного номера.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetPosition(int newPosition)
        {
            if (newPosition == EnumeratorAddressable<IDiskBlock>.IndexOfPositionBeforeFirstElement)
            {
                this.Reset();
                return;
            }

            MethodArgumentValidator.ThrowIfNegative(newPosition, "newPosition");

            if (newPosition >= _numberOfBlocks)
            {
                throw new ArgumentOutOfRangeException("newPosition", "Невозможно установить позицию за границей массива");
            }

            #region совсем топорная версия
//            if (newPosition < _position)
//            {
//                this.Reset();
//            }
//
//            while (_position != newPosition)
//            {
//                this.MoveNext();
//            }
//
            //return;
            #endregion

            var compositeIndex = new CompositeIndex(newPosition,
                                                    new[]
                                                        {
                                                            _addressingSystemParameters.IndirectBlockReferencesCountInDoubleIndirectBlock,
                                                            _addressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock
                                                        });

            var sizes = _blockSizesCalculator.GetSizesOfAddressingBlocksSufficientToStoreItems(newPosition);

            _doubleIndirectListEnumerator.SetPosition(compositeIndex.CompositeValue[0]);

            bool isLastDoubleIndirectionBlock = _doubleIndirectListEnumerator.IsAtLastElement;

            int numberOfSingleIndirectBlockReferences =
                isLastDoubleIndirectionBlock ?
                _addressingSystemSizesOfLastBlocks.LastSingleIndirectBlockSize
                : _addressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock;

            this.InitializeSingleIndirectionBlock(
                _doubleIndirectListEnumerator.Current,
                numberOfSingleIndirectBlockReferences,
                compositeIndex.CompositeValue[1]);

            this.InitializeCurrentBlockBrowser();

            _position = newPosition;
        }

        public int Length
        {
            get { return _dataStreamDefinition.StreamLengthInBytes; }
        }

        private void InitializeCurrentBlockBrowser()
        {
            int divisionRemainder = _dataStreamDefinition.StreamLengthInBytes % _disk.BlockSizeInBytes;

            if (divisionRemainder == 0)
            {
                divisionRemainder = _disk.BlockSizeInBytes;
            }

            // Note: логику по созданию я здесь оставлять не планировал. Очевидно, что она не на месте. Какое Enumerator-у дело до
            // того, какой у нас блок - последний или не последний? Остается на моей совести.

            int occupiedSpace = this.IsAtLastElement
                                    ? divisionRemainder
                                    : _disk.BlockSizeInBytes;

            var diskBlock = new DiskBlock(_disk, _singlyIndirectReferencesEnumerator.Current, occupiedSpace, 0);

            if (!this.IsAtLastElement)
            {
                _current = diskBlock;
            }
            else
            {
                _current = new DiskBlockNodeUpdating(diskBlock, _dataStreamDefinition, _streamOwningNode, _fileSystemNodeStorage);
            }
        }

        #region IEnumeratorAddressable<IDiskBlock> Members

        /// <summary>
        /// Номер блока по счету, если представить весь поток данных разделенным на блоки.
        /// </summary>
        public int Position
        {
            get
            {
                return _position;
            }
        }

        public bool IsAtLastElement
        {
            get { return (_doubleIndirectListEnumerator.IsAtLastElement && _singlyIndirectReferencesEnumerator.IsAtLastElement); }
        }

        public int Count
        {
            get { return _numberOfBlocks; }
        }

        #endregion

        #region IEnumerator<IDiskBlock> Members

        public IDiskBlock Current
        {
            get { return _current; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get { return this.Current; }
        }

        public bool MoveNext()
        {
            bool successfullyMoved = true;

            if (_doubleIndirectListEnumerator.Position == PositionBeforeFirstElement)
            {
                successfullyMoved = _doubleIndirectListEnumerator.MoveNext();
            }

            if (!successfullyMoved)
            {
                return false;
            }

            if (!_singlyIndirectReferencesEnumerator.MoveNext())
            {
                successfullyMoved = _doubleIndirectListEnumerator.MoveNext();

                if (successfullyMoved)
                {
                    int integerCount;

                    if (_doubleIndirectListEnumerator.IsAtLastElement)
                    {
                        integerCount = _addressingSystemSizesOfLastBlocks.LastSingleIndirectBlockSize;
                    }
                    else
                    {
                        integerCount = _addressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock;
                    }

                    this.InitializeSingleIndirectionBlock(_doubleIndirectListEnumerator.Current, integerCount, PositionBeforeFirstElement);
                    successfullyMoved = _singlyIndirectReferencesEnumerator.MoveNext();
                }
            }

            if (successfullyMoved)
            {
                this.InitializeCurrentBlockBrowser();
                _position++;
            }

            return successfullyMoved;
        }

        public void Reset()
        {
            _position = EnumeratorAddressable<IDiskBlock>.IndexOfPositionBeforeFirstElement;
            
            this.SetFirstSingleIndirectBlock();

            _current = new NullDiskBlock();
        }

        #endregion
    }
}