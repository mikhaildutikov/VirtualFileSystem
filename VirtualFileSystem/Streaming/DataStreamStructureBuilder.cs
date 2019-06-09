using System;
using System.Collections.Generic;
using System.Linq;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Extensions;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Streaming
{
    internal class DataStreamStructureBuilder : DataStreamStructureBuilderImmutable
    {
        private readonly IFreeBlockManager _freeBlockManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataStreamDefinition"></param>
        /// <param name="disk"></param>
        /// <param name="freeBlockManager"></param>
        /// <param name="fileSystemNodeStorage"></param>
        /// <param name="governingNode"></param>
        /// <param name="addressingSystemParameters"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DataStreamStructureBuilder(DataStreamDefinition dataStreamDefinition, IVirtualDisk disk, IFreeBlockManager freeBlockManager, IFileSystemNodeStorage fileSystemNodeStorage, Node governingNode, AddressingSystemParameters addressingSystemParameters)
            : base(disk, dataStreamDefinition, addressingSystemParameters, fileSystemNodeStorage, governingNode)
        {
            if (freeBlockManager == null) throw new ArgumentNullException("freeBlockManager");

            _freeBlockManager = freeBlockManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newSize"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="NoFreeBlocksException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="MaximumFileSizeReachedException"></exception>
        public override void SetSize(int newSize)
        {
            MethodArgumentValidator.ThrowIfNegative(newSize, "newSize");

            if (newSize == this.CurrentSize)
            {
                return;
            }

            if (newSize > this.MaximumSize)
            {
                throw new MaximumFileSizeReachedException(
                    "Не удалось установить размер структуры в {0} байт. Максимальный размер - {1} байт.".FormatWith(
                        newSize, this.MaximumSize));
            }

            if (newSize > this.CurrentSize)
            {
                this.MakeLonger(newSize - this.CurrentSize);
            }
            else
            {
                this.MakeShorter(newSize);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numberOfBytesToAdd"></param>
        /// <exception cref="NoFreeBlocksException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void MakeLonger(int numberOfBytesToAdd)
        {
            int numberOfBlocksCommitted =
                SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(base.CurrentSize,
                                                                               base.AddressingSystemParameters.BlockSize);

            int numberOfBlocksToAdd = SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(base.CurrentSize + numberOfBytesToAdd,
                                                                               base.AddressingSystemParameters.BlockSize) - numberOfBlocksCommitted;

            if ((numberOfBlocksToAdd == 0) && (numberOfBytesToAdd > 0))
            {
                base.DataStreamDefinition.StreamLengthInBytes += numberOfBytesToAdd;
                return;
            }
            else if (numberOfBlocksToAdd == 0)
            {
                return;
            }

            List<int> allBlocksAcquired = _freeBlockManager.AcquireFreeBlocks(numberOfBlocksToAdd).ToList();

            var blocksToDistribute = new Stack<int>(allBlocksAcquired);

            PushFreeBlocksToAddressingSystem(numberOfBytesToAdd, numberOfBlocksCommitted, allBlocksAcquired, blocksToDistribute);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numberOfBytesToAdd"></param>
        /// <param name="numberOfBlocksCommitted"></param>
        /// <param name="allBlocksAcquired"></param>
        /// <param name="blocksToDistribute"></param>
        /// <exception cref="NoFreeBlocksException"></exception>
        private void PushFreeBlocksToAddressingSystem(int numberOfBytesToAdd, int numberOfBlocksCommitted, List<int> allBlocksAcquired, Stack<int> blocksToDistribute)
        {
            IntegerListConstrained lastSinglyIndirectBlock = null;
            int lastSingleIndirectBlockSize = 0;

            var addressingSystemBlockSizes =
                base.AddressingBlockSizesCalculator.GetSizesOfAddressingBlocksSufficientToStoreItems(numberOfBlocksCommitted);

            lastSingleIndirectBlockSize = addressingSystemBlockSizes.LastSingleIndirectBlockSize;

            if (this.CurrentSize != 0)
            {
                var lastSingleIndirectBlockIndex = base.DoubleIndirectBlocks[base.DoubleIndirectBlocks.Count - 1];

                lastSinglyIndirectBlock = new IntegerListConstrained(
                    base.Disk.ReadAllBytesFromBlock(lastSingleIndirectBlockIndex),
                    lastSingleIndirectBlockSize,
                    base.AddressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock);
            }

            try
            {
                while (blocksToDistribute.Count != 0)
                {
                    if ((lastSinglyIndirectBlock != null) && (!lastSinglyIndirectBlock.IsFull))
                    {
                        lastSinglyIndirectBlock.AddInteger(blocksToDistribute.Pop());
                    }
                    else
                    {
                        if (lastSinglyIndirectBlock != null)
                        {
                            base.Disk.WriteBytesToBlock(base.DoubleIndirectBlocks[base.DoubleIndirectBlocks.Count - 1],
                                                        lastSinglyIndirectBlock.ToByteArray());
                        }

                        int newBlockIndex = _freeBlockManager.AcquireFreeBlock();
                        allBlocksAcquired.Add(newBlockIndex);

                        base.DoubleIndirectBlocks.AddInteger(newBlockIndex);

                        base.Disk.WriteBytesToBlock(base.DataStreamDefinition.ContentsBlockIndex, base.DoubleIndirectBlocks.ToByteArray());

                        lastSinglyIndirectBlock = new IntegerListConstrained(new byte[0], 0, base.AddressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock);
                    }
                }

                base.Disk.WriteBytesToBlock(base.DoubleIndirectBlocks[base.DoubleIndirectBlocks.Count - 1], lastSinglyIndirectBlock.ToByteArray());
                base.DataStreamDefinition.StreamLengthInBytes += (numberOfBytesToAdd);
            }
            catch (NoFreeBlocksException)
            {
                _freeBlockManager.MarkBlocksAsFree(allBlocksAcquired);
                
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newLength"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        private void MakeShorter(int newLength)
        {
            int numberOfBlocksCommitted =
                SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(base.CurrentSize,
                                                                               base.AddressingSystemParameters.BlockSize);

            int numberOfBlocksNeededToStoreNewData =
                SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(newLength,
                                                                               base.AddressingSystemParameters.BlockSize);

            int numberOfBlocksToRemove = numberOfBlocksCommitted - numberOfBlocksNeededToStoreNewData;

            if (numberOfBlocksToRemove == 0) //изменения в пределах одного блока.
            {
                base.DataStreamDefinition.StreamLengthInBytes = newLength;
                base.FileSystemNodeStorage.WriteNode(base.GoverningNode);

                return;
            }

            var oldAddressingSystemBlockSizes =
                base.AddressingBlockSizesCalculator.GetSizesOfAddressingBlocksSufficientToStoreItems(numberOfBlocksCommitted);

            var newAddressingSystemBlockSizes =
                base.AddressingBlockSizesCalculator.GetSizesOfAddressingBlocksSufficientToStoreItems(numberOfBlocksNeededToStoreNewData);

            int doubleIndirectBlockIndexToStopAt = newAddressingSystemBlockSizes.DoubleIndirectBlockSize == 0
                                                       ? 0
                                                       : newAddressingSystemBlockSizes.DoubleIndirectBlockSize;

            for (int i = oldAddressingSystemBlockSizes.DoubleIndirectBlockSize - 1; i >= doubleIndirectBlockIndexToStopAt; i--)
            {
                int singlyIndirectBlockIndex = base.DoubleIndirectBlocks[i];
                int newSize;
                int currentSize;

                if (i == oldAddressingSystemBlockSizes.DoubleIndirectBlockSize - 1)
                {
                    currentSize = oldAddressingSystemBlockSizes.LastSingleIndirectBlockSize;
                }
                else
                {
                    currentSize = base.AddressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock;
                }

                if (i == doubleIndirectBlockIndexToStopAt) // ок, этот - остается.
                {
                    newSize = newAddressingSystemBlockSizes.LastSingleIndirectBlockSize;
                }
                else
                {
                    newSize = 0;
                }

                var listConstrained =
                        new IntegerListConstrained(base.Disk.ReadAllBytesFromBlock(singlyIndirectBlockIndex), currentSize, base.AddressingSystemParameters.DataBlockReferencesCountInSingleIndirectBlock);

                _freeBlockManager.MarkBlocksAsFree(listConstrained.Shrink(newSize));
            }

            _freeBlockManager.MarkBlocksAsFree(base.DoubleIndirectBlocks.Shrink(newAddressingSystemBlockSizes.DoubleIndirectBlockSize));

            base.Disk.WriteBytesToBlock(base.DataStreamDefinition.ContentsBlockIndex, base.DoubleIndirectBlocks.ToByteArray());

            base.DataStreamDefinition.StreamLengthInBytes = newLength;
            base.FileSystemNodeStorage.WriteNode(base.GoverningNode);
        }
    }
}