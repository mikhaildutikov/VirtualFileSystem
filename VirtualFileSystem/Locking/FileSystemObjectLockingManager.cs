using System;
using System.Collections.Generic;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Locking
{
    internal class FileSystemObjectLockingManager : IFileSystemObjectLockingManager
    {
        private readonly Dictionary<Guid, FileLockable> _lockIdsToLocks = new Dictionary<Guid, FileLockable>();
        private readonly Dictionary<Guid, FileLockable> _fileIdsToLocks = new Dictionary<Guid, FileLockable>();
        private readonly Dictionary<Guid, FolderNodeLockable> _nodeIdsToLocks = new Dictionary<Guid, FolderNodeLockable>();
        private readonly object _operationExecutionCriticalSection = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lockId"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="LockNotFoundException"></exception>
        public void ReleaseLock(Guid lockId)
        {
            MethodArgumentValidator.ThrowIfIsDefault<Guid>(lockId, "lockId");

            lock (_operationExecutionCriticalSection)
            {
                if (_lockIdsToLocks.ContainsKey(lockId))
                {
                    FileLockable fileLockable = _lockIdsToLocks[lockId];

                    fileLockable.ReleaseLock(lockId);

                    if (fileLockable.IsFreeForTaking)
                    {
                        _fileIdsToLocks.Remove(fileLockable.FileId); // все: файл разблокирован
                    }

                    // еще и все ветки отпускаем - если их больше ничто не держит.
                    foreach (Guid nodeId in fileLockable.IdsOfParentFoldersUpToRoot)
                    {
                        FolderNodeLockable folderNodeLockable = _nodeIdsToLocks[nodeId];

                        folderNodeLockable.ReleaseLock(lockId);
                        
                        if (!folderNodeLockable.IsLocked)
                        {
                            _nodeIdsToLocks.Remove(folderNodeLockable.IdOfLockedNode);
                        }
                    }

                    _lockIdsToLocks.Remove(lockId);
                }
                else
                {
                    throw new LockNotFoundException("Не найдена блокировка с идентификатором {0}".FormatWith(lockId));
                }
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        public bool IsFileLocked(FileNode nodeToCheck)
        {
            if (nodeToCheck == null) throw new ArgumentNullException("nodeToCheck");

            lock (_operationExecutionCriticalSection)
            {
                return _fileIdsToLocks.ContainsKey(nodeToCheck.Id);
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        public bool IsNodeLockedForRenamesAndMoves(FolderNode nodeToCheck)
        {
            if (nodeToCheck == null) throw new ArgumentNullException("nodeToCheck");

            lock (_operationExecutionCriticalSection)
            {
                return _nodeIdsToLocks.ContainsKey(nodeToCheck.Id);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileToLock"></param>
        /// <param name="lockKind"></param>
        /// <returns></returns>
        /// <exception cref="CannotAcquireLockException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public Guid AcquireLock(NodeWithSurroundingsResolvingResult<FileNode> fileToLock, LockKind lockKind)
        {
            if (fileToLock == null) throw new ArgumentNullException("fileToLock");

            lock (_operationExecutionCriticalSection)
            {
                var idOfFileToLock = fileToLock.ResolvedNode.Id;
                FileLockable fileLockable;
                Guid newLockId;

                if (_fileIdsToLocks.ContainsKey(idOfFileToLock))
                {
                    fileLockable = _fileIdsToLocks[idOfFileToLock];
                }
                else
                {
                    fileLockable = new FileLockable(fileToLock.ResolvedNode, fileToLock.FoldersPassedWhileResolving);
                    _fileIdsToLocks[idOfFileToLock] = fileLockable;
                }

                newLockId = LockTheFile(fileLockable, lockKind);

                ReportToAllFolderNodesLockableThatTheyHaveANewLock(fileToLock, fileLockable, newLockId);

                _lockIdsToLocks[newLockId] = fileLockable;

                return newLockId;
            }
        }

        private void ReportToAllFolderNodesLockableThatTheyHaveANewLock(NodeWithSurroundingsResolvingResult<FileNode> fileToLock, FileLockable fileLockable, Guid newLockId)
        {
            foreach (FolderNode node in fileToLock.FoldersPassedWhileResolving)
            {
                FolderNodeLockable folderNodeLockableToAdjust;

                if (_nodeIdsToLocks.ContainsKey(node.Id))
                {
                    folderNodeLockableToAdjust = _nodeIdsToLocks[node.Id];
                }
                else
                {
                    folderNodeLockableToAdjust = new FolderNodeLockable(node);
                    _nodeIdsToLocks[node.Id] = folderNodeLockableToAdjust;
                }

                folderNodeLockableToAdjust.AddLock(fileLockable, newLockId);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileLockable"></param>
        /// <param name="lockKind"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="CannotAcquireLockException"></exception>
        private static Guid LockTheFile(FileLockable fileLockable, LockKind lockKind)
        {
            Guid newLockId;

            if (lockKind == LockKind.Read)
            {
                newLockId = fileLockable.LockForReading();
            }
            else if (lockKind == LockKind.Write)
            {
                newLockId = fileLockable.LockForWriting();
            }
            else
            {
                throw new ArgumentException("Вид блокировки {0} не поддерживается.".FormatWith(lockKind.ToString()));
            }

            return newLockId;
        }
    }
}