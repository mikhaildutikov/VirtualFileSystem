using System;
using System.Collections.ObjectModel;
using System.IO;
using VirtualFileSystem.Disk;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.FreeSpace
{
    /// <summary>
    /// Note: пожалуй, единственное место, где не сделана запись данных в Little Endian.
    /// </summary>
    internal class FreeSpaceBitmapStore
    {
        private readonly IVirtualDisk _disk;
        private readonly int _freeSpaceMapStartingBlock;

        public FreeSpaceBitmapStore(IVirtualDisk disk, int freeSpaceMapStartingBlock)
        {
            if (disk == null) throw new ArgumentNullException("disk");

            if (!disk.CanBlockBelongToDisk(freeSpaceMapStartingBlock))
            {
                throw new ArgumentOutOfRangeException("freeSpaceMapStartingBlock");
            }

            _disk = disk;
            _freeSpaceMapStartingBlock = freeSpaceMapStartingBlock;
        }

        public void WriteMap(byte[] freeSpaceBitmap, int freeSpaceBitmapLengthInBits)
        {
            if (freeSpaceBitmap == null) throw new ArgumentNullException("freeSpaceBitmap");

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(freeSpaceBitmapLengthInBits);

            _disk.WriteBytesContinuoslyStartingFromBlock(freeSpaceBitmap, _freeSpaceMapStartingBlock, 0, Constants.BytesToStoreChunkOfDataLength); // смещение в 4 байта потому, что в начале первого блока (free space bitmap) записана длина bitmap
            _disk.WriteBytesContinuoslyStartingFromBlock(stream.ToArray(), _freeSpaceMapStartingBlock, 0, 0);
        }

        public byte[] ReadMap(out int bitmapSizeInBits)
        {
            byte[] firstBlockBytes = _disk.ReadAllBytesFromBlock(_freeSpaceMapStartingBlock);

            var stream = new MemoryStream(firstBlockBytes);
            var binaryReader = new BinaryReader(stream);

            int freeSpaceLengthInBits = binaryReader.ReadInt32();

            int freeSpaceLengthInBytes = SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(
                freeSpaceLengthInBits, Constants.NumberOfBitsInByte);

            ReadOnlyCollection<BucketDistribution> numberOfBytesToReadFromEachBlock = ItemDistributor.Distribute(freeSpaceLengthInBytes,
                                                                                                   _disk.BlockSizeInBytes - Constants.BlockReferenceSizeInBytes,
                                                                                                   _disk.BlockSizeInBytes);

            var freeSpaceMap = new byte[freeSpaceLengthInBytes];

            int numberOfCurrentBlock = _freeSpaceMapStartingBlock;
            int initialArrayPosition = 0;

            foreach (BucketDistribution distribution in numberOfBytesToReadFromEachBlock)
            {
                byte[] newArray = _disk.ReadBytesFromBlock(numberOfCurrentBlock + distribution.BucketIndex, distribution.IndexOfFirstItemTheBucketGot,
                                        distribution.NumberOfItemsDistributed);

                Array.Copy(newArray, 0, freeSpaceMap, initialArrayPosition, distribution.NumberOfItemsDistributed);
                initialArrayPosition += distribution.NumberOfItemsDistributed;
            }

            bitmapSizeInBits = freeSpaceLengthInBits;
            return freeSpaceMap;
        }
    }
}