using System;
using System.Collections.Generic;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.FreeSpace;

namespace VirtualFileSystem.Extensions
{
    internal static class FreeBlockManagerExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="freeBlockManager"></param>
        /// <param name="blocksToFree"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="BlockNotOccupiedException"></exception>
        public static void MarkBlocksAsFree(this IFreeBlockManager freeBlockManager, IEnumerable<int> blocksToFree)
        {
            if (freeBlockManager == null) throw new ArgumentNullException("freeBlockManager");
            if (blocksToFree == null) throw new ArgumentNullException("blocksToFree");

            foreach (int blockIndex in blocksToFree)
            {
                freeBlockManager.MarkBlockAsFree(blockIndex);
            }
        }
    }
}