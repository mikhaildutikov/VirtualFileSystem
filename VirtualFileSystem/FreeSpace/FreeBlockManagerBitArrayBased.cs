using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.FreeSpace
{
    internal class FreeBlockManagerBitArrayBased : IFreeBlockManager
    {
        private readonly BitArray _freeSpaceBitmap;
        private readonly int _blockIndexOffset;
        private readonly int _freeSpaceMapLength;
        private int _freeBlockCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="freeSpaceBitmap"></param>
        /// <param name="blockIndexOffset"></param>
        /// <param name="freeSpaceMapLength"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public FreeBlockManagerBitArrayBased(BitArray freeSpaceBitmap, int blockIndexOffset, int freeSpaceMapLength)
        {
            MethodArgumentValidator.ThrowIfNull(freeSpaceBitmap, "freeSpaceBitmap");
            MethodArgumentValidator.ThrowIfNegative(blockIndexOffset, "blockIndexOffset");

            if (freeSpaceBitmap.Length != freeSpaceMapLength)
            {
                throw new ArgumentException("В Bitmap для разметки свободного места должно быть следующее число бит: {0}".FormatWith(freeSpaceMapLength));
            }

            _freeSpaceBitmap = freeSpaceBitmap;
            _blockIndexOffset = blockIndexOffset;
            _freeSpaceMapLength = freeSpaceMapLength;

            int freeBlocksCount = 0;

            for (int i = 0; i < freeSpaceBitmap.Length; i++)
            {
                if (!freeSpaceBitmap[i])
                {
                    freeBlocksCount++;
                }
            }

            _freeBlockCount = freeBlocksCount;
        }

        public byte[] MapToByteArray()
        {
            var bufferForBitmapCopies = new byte[SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(_freeSpaceMapLength, Constants.NumberOfBitsInByte)];

            _freeSpaceBitmap.CopyTo(bufferForBitmapCopies, 0);

            return bufferForBitmapCopies;
        }

        public int FreeBlockCount
        {
            get { return _freeBlockCount; }
        }

        public int FreeSpaceMapSizeInBits
        {
            get
            {
                return _freeSpaceMapLength;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NoFreeBlocksException"></exception>
        public int AcquireFreeBlock()
        {
            for (int blockNumber = 0; blockNumber < _freeSpaceBitmap.Length; blockNumber++)
            {
                if (_freeSpaceBitmap[blockNumber] == false)
                {
                    _freeSpaceBitmap[blockNumber] = true;
                    _freeBlockCount--;
                    return blockNumber + _blockIndexOffset;
                }
            }

            throw new NoFreeBlocksException("Свободных блоков не найдено.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void MarkBlockAsFree(int blockIndex)
        {
            MethodArgumentValidator.ThrowIfNegative(blockIndex, "blockIndex");

            int blockIndexWithOffset = blockIndex - _blockIndexOffset;

            if ((blockIndexWithOffset > _freeSpaceBitmap.Length) || (blockIndexWithOffset < 0))
            {
                throw new ArgumentOutOfRangeException("blockIndex");
            }

            if (!_freeSpaceBitmap[blockIndexWithOffset])
            {
                throw new BlockNotOccupiedException("Не удалось пометить блок с индексом {0} как свободный. Он не занят.".FormatWith(blockIndex));
            }

            _freeSpaceBitmap[blockIndexWithOffset] = false;
            _freeBlockCount++;
        }

        private void ReleaseBlocks(IEnumerable<int> blockIndexes)
        {
            foreach (int blockIndex in blockIndexes)
            {
                _freeSpaceBitmap[blockIndex - _blockIndexOffset] = false;
                _freeBlockCount++;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="numberOfBlocksToAcquire"></param>
        /// <returns></returns>
        /// <exception cref="NoFreeBlocksException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ReadOnlyCollection<int> AcquireFreeBlocks(int numberOfBlocksToAcquire)
        {
            if (numberOfBlocksToAcquire <= 0)
            {
                throw new ArgumentOutOfRangeException("numberOfBlocksToAcquire", "Требуется положительное число");
            }

            // Note: возможны оптимизации

            var freeBlocks = new List<int>(numberOfBlocksToAcquire);

            try
            {
                for (int i = 0; i < numberOfBlocksToAcquire; i++)
                {
                    int blockIndex = this.AcquireFreeBlock();
                    freeBlocks.Add(blockIndex);
                }

                return freeBlocks.AsReadOnly();
            }
            catch (NoFreeBlocksException)
            {
                this.ReleaseBlocks(freeBlocks);

                throw new NoFreeBlocksException("Нужного количества свободных блоков не обнаружено.");
            }
        }
    }
}