using System;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem
{
    /// <summary>
    /// Содержит некоторые сведения о файловой системе.
    /// </summary>
    public class VirtualFileSystemInfo
    {
        internal VirtualFileSystemInfo(Version version, int blockSizeInBytes, int firstNonReservedDiskBlockIndex, int freeSpaceBitmapStartingBlock)
        {
            if (version == null) throw new ArgumentNullException("version");
            MethodArgumentValidator.ThrowIfNegative(blockSizeInBytes, "blockSizeInBytes");
            MethodArgumentValidator.ThrowIfNegative(firstNonReservedDiskBlockIndex, "firstNonReservedDiskBlockIndex");
            MethodArgumentValidator.ThrowIfNegative(freeSpaceBitmapStartingBlock, "freeSpaceBitmapStartingBlock");

            Version = version;
            BlockSizeInBytes = blockSizeInBytes;
            FirstNonReservedDiskBlockIndex = firstNonReservedDiskBlockIndex;
            FreeSpaceBitmapStartingBlock = freeSpaceBitmapStartingBlock;
        }

        public Version Version
        {
            get;
            private set;
        }

        public int BlockSizeInBytes { get; private set; }

        internal int FirstNonReservedDiskBlockIndex { get; private set; }

        internal int FreeSpaceBitmapStartingBlock { get; private set; }
    }
}