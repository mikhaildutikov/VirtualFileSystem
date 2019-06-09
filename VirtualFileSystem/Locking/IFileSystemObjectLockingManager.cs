using System;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;

namespace VirtualFileSystem.Locking
{
    internal interface IFileSystemObjectLockingManager : IFileSystemLockReleaseManager
    {
        /// <summary>
        /// Проверяет, запрещена ли операция переименования/перемещения для заданной папки.
        /// </summary>
        /// <param name="nodeToCheck">Папка, которую нужно проверить.</param>
        /// <returns>True, если переименование/перемещение папки производить нельзя. False - если можно.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        bool IsNodeLockedForRenamesAndMoves(FolderNode nodeToCheck);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileToLock"></param>
        /// <param name="lockKind"></param>
        /// <returns></returns>
        /// <exception cref="CannotAcquireLockException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        Guid AcquireLock(NodeWithSurroundingsResolvingResult<FileNode> fileToLock, LockKind lockKind);

        /// <exception cref="ArgumentNullException"></exception>
        bool IsFileLocked(FileNode nodeToCheck);
    }
}