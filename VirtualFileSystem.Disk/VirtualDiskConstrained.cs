// Note: идея была в том, чтобы строго ограничить доступ к дисковым блокам (скажем, файл читает только свои блоки, остальные ему недоступны и т.д.).
// Note: В итоге в виду масштабов задачи от идеи я отказался. По хорошему же какое-то ограничение такого рода очень нужно.


//using System.Collections.Generic;
//using VirtualFileSystem.Toolbox.Extensions;

//namespace VirtualFileSystem.Disk
//{
//    internal class VirtualDiskConstrained : IVirtualDisk
//    {
//        private readonly HashSet<int> _indexesOfAccessibleBlocks;
//        private readonly IVirtualDisk _virtualDisk;

//        public VirtualDiskConstrained(IVirtualDisk virtualDisk, IEnumerable<int> indexesOfAccessibleBlocks)
//        {

//            _virtualDisk = virtualDisk;
//            _indexesOfAccessibleBlocks = new HashSet<int>(indexesOfAccessibleBlocks);
//        }

//        public int BlockSizeInBytes
//        {
//            get { return _virtualDisk.BlockSizeInBytes; }
//        }

//        public long DiskSizeInBytes
//        {
//            get { return _virtualDisk.DiskSizeInBytes; }
//        }

//        public int NumberOfBlocks
//        {
//            get { return _virtualDisk.NumberOfBlocks; }
//        }

//        public byte[] ReadAllBytesFromBlock(int indexOfBlockToReadFrom)
//        {
//            this.MakeSureBlockIsAccessible(indexOfBlockToReadFrom);

//            return _virtualDisk.ReadAllBytesFromBlock(indexOfBlockToReadFrom);
//        }

//        private void MakeSureBlockIsAccessible(int indexOfBlockToReadFrom)
//        {
//            if (!_indexesOfAccessibleBlocks.Contains(indexOfBlockToReadFrom))
//            {
//                throw new BlockAccessDeniedException("Доступ к блоку {0} запрещен.".FormatWith(indexOfBlockToReadFrom));
//            }
//        }

//        public byte[] ReadBytesFromBlock(int indexOfBlockToReadFrom, int startingPosition, int numberOfBytesToRead)
//        {
//            this.MakeSureBlockIsAccessible(indexOfBlockToReadFrom);

//            return _virtualDisk.ReadBytesFromBlock(indexOfBlockToReadFrom, startingPosition, numberOfBytesToRead);
//        }

//        public void WriteBytesToBlock(int indexOfBlockToWriteTo, byte[] bytesToWrite)
//        {
//            this.MakeSureBlockIsAccessible(indexOfBlockToWriteTo);

//            _virtualDisk.WriteBytesToBlock(indexOfBlockToWriteTo, bytesToWrite);
//        }

//        public int WriteBytesContinuoslyStartingFromBlock(byte[] bytes, int startingBlockIndex, int arrayOffset, int blockOffset)
//        {
//            this.MakeSureBlockIsAccessible(startingBlockIndex); // Note: недостаточно

//            return _virtualDisk.WriteBytesContinuoslyStartingFromBlock(bytes, startingBlockIndex, arrayOffset, blockOffset);
//        }

//        public void WriteBytesToBlock(int blockIndex, byte[] bytesToWrite, int arrayOffset, int blockOffset, int numberOfBytesToWrite)
//        {
//            this.MakeSureBlockIsAccessible(blockIndex);

//            _virtualDisk.WriteBytesToBlock(blockIndex, bytesToWrite, arrayOffset, blockOffset, numberOfBytesToWrite);
//        }
//    }
//}