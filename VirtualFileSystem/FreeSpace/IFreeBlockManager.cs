using System;
using System.Collections.ObjectModel;
using VirtualFileSystem.Exceptions.Irrecoverable;

namespace VirtualFileSystem.FreeSpace
{
    internal interface IFreeBlockManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NoFreeBlocksException"></exception>
        int AcquireFreeBlock();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="BlockNotOccupiedException"></exception>
        void MarkBlockAsFree(int blockIndex);

        /// <summary>
        /// </summary>
        /// <param name="numberOfBlocksToAcquire"></param>
        /// <returns></returns>
        /// <exception cref="NoFreeBlocksException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        ReadOnlyCollection<int> AcquireFreeBlocks(int numberOfBlocksToAcquire);

        int FreeBlockCount { get; }
    }
}