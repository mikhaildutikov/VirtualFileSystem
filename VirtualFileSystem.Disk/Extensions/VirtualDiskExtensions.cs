using System;

// ReSharper disable CheckNamespace
namespace VirtualFileSystem.Disk
// ReSharper restore CheckNamespace
{
    internal static class VirtualDiskExtensions
    {
        public static bool CanBlockBelongToDisk(this IVirtualDisk disk, int blockIndex)
        {
            if (disk == null) throw new ArgumentNullException("disk");

            if (blockIndex < 0)
            {
                return false;
            }

            return (blockIndex < disk.NumberOfBlocks);
        }
    }
}