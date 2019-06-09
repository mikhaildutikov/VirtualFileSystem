using System;
using VirtualFileSystem.Disk;

namespace VirtualFileSystem
{
    internal class AddressingSystemParameters
    {
        public static readonly AddressingSystemParameters Default = new AddressingSystemParameters(VirtualDisk.OnlySupportedBlockSize / sizeof(Int32), VirtualDisk.OnlySupportedBlockSize / sizeof(Int32), VirtualDisk.OnlySupportedBlockSize);

        public AddressingSystemParameters(int indirectBlockReferencesCountInDoubleIndirectBlock, int dataBlockReferencesCountInSingleIndirectBlock, int blockSize)
        {
            if (indirectBlockReferencesCountInDoubleIndirectBlock <= 0)
            {
                throw new ArgumentOutOfRangeException("indirectBlockReferencesCountInDoubleIndirectBlock", "Требуется положительное число");
            }

            if (dataBlockReferencesCountInSingleIndirectBlock <= 0)
            {
                throw new ArgumentOutOfRangeException("dataBlockReferencesCountInSingleIndirectBlock", "Требуется положительное число");
            }

            if (blockSize <= 0)
            {
                throw new ArgumentOutOfRangeException("blockSize", "Требуется положительное число");
            }

            IndirectBlockReferencesCountInDoubleIndirectBlock = indirectBlockReferencesCountInDoubleIndirectBlock;
            DataBlockReferencesCountInSingleIndirectBlock = dataBlockReferencesCountInSingleIndirectBlock;
            BlockSize = blockSize;
        }

        public int DataBlockReferencesCountInSingleIndirectBlock { get; private set; }
        public int BlockSize { get; private set; }
        public int IndirectBlockReferencesCountInDoubleIndirectBlock { get; private set; }
    }
}