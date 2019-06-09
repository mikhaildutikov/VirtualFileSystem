using System;
using System.Collections;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem
{
    internal class VirtualDiskFormatter
    {
        /// <summary>
        /// Note: статических связей и настроек я стараюсь оставлять как можно меньше. Эти (+ Constants, + еще мелочи) остаются - я уже достаточно времени потратил на задачку.
        /// </summary>
        internal const int FileSystemHeaderReservedBlocks = 1;
        internal const int FileSystemHeaderBlockIndex = 0;
        internal const int FreeSpaceStartingBlockIndex = 1;

        public void Format(IVirtualDisk virtualDisk, FileSystemNodeStorage fileSystemNodeStorage)
        {
            var numberOfBlocksToMap = virtualDisk.NumberOfBlocks - FileSystemHeaderReservedBlocks;

            int numberOfBytesNeededToStoreTheMap =
                SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(numberOfBlocksToMap, Constants.NumberOfBitsInByte);

            int numberOfBlocksNeededToStoreTheMap =
                SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(numberOfBytesNeededToStoreTheMap,
                                                                               virtualDisk.BlockSizeInBytes);

            numberOfBlocksToMap -= numberOfBlocksNeededToStoreTheMap;

            var freeSpaceBitmap = new BitArray(numberOfBytesNeededToStoreTheMap);
            freeSpaceBitmap.Length = numberOfBlocksToMap;

            var store = new FreeSpaceBitmapStore(virtualDisk, VirtualDiskFormatter.FreeSpaceStartingBlockIndex);

            var freeBlockManagerBitArrayBased =
                new FreeBlockManagerBitArrayBased(freeSpaceBitmap, FreeSpaceStartingBlockIndex + numberOfBlocksNeededToStoreTheMap, numberOfBlocksToMap);

            var freeBlockManager = new FreeBlockManagerDiskWriting(store, freeBlockManagerBitArrayBased);

            var freeBlocks = freeBlockManager.AcquireFreeBlocks(3);

            int rootBlockIndex = freeBlocks[0];
            int rootFileReferencesBlock = freeBlocks[1];
            int rootFolderReferencesBlock = freeBlocks[2];

            var fileContentsStreamDefinition = new DataStreamDefinition(rootFileReferencesBlock, 0);
            var folderContentsStreamDefinition = new DataStreamDefinition(rootFolderReferencesBlock, 0);

            var rootNode = new FolderNode("root", Guid.NewGuid(), rootBlockIndex, 0, fileContentsStreamDefinition, folderContentsStreamDefinition, DateTime.UtcNow, Guid.NewGuid());

            fileSystemNodeStorage.WriteNode(rootNode);

            fileSystemNodeStorage.WriteFileSystemHeader(new FileSystemHeader(rootBlockIndex, new Version(1, 0, 0, 0)), FileSystemHeaderBlockIndex);
        }
    }
}