using System;
using VirtualFileSystem.Exceptions.Irrecoverable;

namespace VirtualFileSystem.Locking
{
    internal interface IFileSystemLockReleaseManager
    {
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="LockNotFoundException"></exception>
        void ReleaseLock(Guid lockId);
    }
}