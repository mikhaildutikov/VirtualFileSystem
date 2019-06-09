using System;
using System.Collections.Generic;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Locking
{
    internal class FolderNodeLockable
    {
        private readonly Guid _idOfLockedNode;
        private readonly HashSet<Guid> _idsOfAssociatedFileLocks = new HashSet<Guid>();

        public FolderNodeLockable(FolderNode nodeToLock)
        {
            if (nodeToLock == null) throw new ArgumentNullException("nodeToLock");

            _idOfLockedNode = nodeToLock.Id;
        }

        public Guid IdOfLockedNode
        {
            get { return _idOfLockedNode; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="associatedFileLock"></param>
        /// <param name="lockId"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="LockAlreadyHeldException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void AddLock(FileLockable associatedFileLock, Guid lockId)
        {
            if (associatedFileLock == null) throw new ArgumentNullException("associatedFileLock");
            MethodArgumentValidator.ThrowIfIsDefault<Guid>(lockId, "lockId");

            if (_idsOfAssociatedFileLocks.Contains(lockId))
            {
                throw new LockAlreadyHeldException("Блокировка с идентификатором {0} уже привязана к блокировке папки.".FormatWith(associatedFileLock.FileId));
            }

            if (!associatedFileLock.IdsOfParentFoldersUpToRoot.Contains(_idOfLockedNode))
            {
                throw new ArgumentException("Папка (идентификатор - {0}) не содержится в списке зависимых для файловой блокировки, которую вы хотите связать с блокировкой этой папки.".FormatWith(_idOfLockedNode));
            }

            _idsOfAssociatedFileLocks.Add(lockId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="LockNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void ReleaseLock(Guid associatedLockId)
        {
            MethodArgumentValidator.ThrowIfIsDefault<Guid>(associatedLockId, "associatedLockId");
            bool removedSuccessfully = _idsOfAssociatedFileLocks.Remove(associatedLockId);

            if (!removedSuccessfully)
            {
                throw new LockNotFoundException("Блокировка с идентификатором {0} не найдена.".FormatWith(associatedLockId));
            }
        }

        public bool IsLocked
        {
            get { return (_idsOfAssociatedFileLocks.Count > 0); }
        }
    }
}