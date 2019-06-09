using System;
using System.Collections.ObjectModel;
using VirtualFileSystem.Exceptions.Irrecoverable;

namespace VirtualFileSystem.FreeSpace
{
    /// <summary>
    /// Note: здесь необходимы behavior-based тесты на то, что объект сохраняет все после каждого успешного вызова метода по получению/отпуску блока. Устал - не делаю.
    /// </summary>
    internal class FreeBlockManagerDiskWriting : IFreeBlockManager
    {
        private readonly FreeBlockManagerBitArrayBased _manager;
        private readonly FreeSpaceBitmapStore _freeSpaceBitmapStore;
        private readonly object _stateProtectingCriticalSection = new object();

        public FreeBlockManagerDiskWriting(
            FreeSpaceBitmapStore freeSpaceBitmapStore,
            FreeBlockManagerBitArrayBased manager)
        {
            if (freeSpaceBitmapStore == null) throw new ArgumentNullException("freeSpaceBitmapStore");
            if (manager == null) throw new ArgumentNullException("manager");

            _freeSpaceBitmapStore = freeSpaceBitmapStore;
            _manager = manager;
        }

        public int FreeBlockCount
        {
            get
            {
                lock (_stateProtectingCriticalSection)
                {
                    return _manager.FreeBlockCount;
                }
            }
        }

        private void SaveFreeSpaceBitmap()
        {
            // Note: оптимизации нет, но вообще каждый раз записывать все необязательно.
            _freeSpaceBitmapStore.WriteMap(_manager.MapToByteArray(), _manager.FreeSpaceMapSizeInBits);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NoFreeBlocksException"></exception>
        public int AcquireFreeBlock()
        {
            lock (_stateProtectingCriticalSection)
            {
                int newBlockIndex = _manager.AcquireFreeBlock();

                this.SaveFreeSpaceBitmap();

                return newBlockIndex;
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
            lock (_stateProtectingCriticalSection)
            {
                var freeBlocks = _manager.AcquireFreeBlocks(numberOfBlocksToAcquire);

                this.SaveFreeSpaceBitmap();

                return freeBlocks; 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void MarkBlockAsFree(int blockIndex)
        {
            lock (_stateProtectingCriticalSection)
            {
                _manager.MarkBlockAsFree(blockIndex);

                this.SaveFreeSpaceBitmap();
            }
        }
    }
}