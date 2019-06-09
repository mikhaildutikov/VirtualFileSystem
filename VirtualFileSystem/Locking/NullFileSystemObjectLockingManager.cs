using System;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Locking
{
    internal class NullFileSystemObjectLockingManager : IFileSystemObjectLockingManager
    {
        public void ReleaseLock(Guid lockId)
        {
        }

        public Guid AcquireLock(NodeWithSurroundingsResolvingResult<FileNode> fileToLock, LockKind lockKind)
        {
            return Guid.NewGuid();
        }

        public bool IsFileLocked(FileNode nodeToCheck)
        {
            return false;
        }

        public bool IsNodeLockedForRenamesAndMoves(FolderNode nodeToCheck)
        {
            return false;
        }
    }
}