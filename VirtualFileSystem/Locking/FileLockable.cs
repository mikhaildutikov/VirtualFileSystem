using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Locking
{
    /// <summary>
    /// Note: не thread-safe.
    /// Note: известные мне примитивы синхронизации .Net FW 3.5 не предлагают нужной функциональности (в основном из-за thread affinity). Не аффинные структуры
    /// вроде семафоров использовать можно, но они здесь мало что добавляют.
    /// </summary>
    internal class FileLockable
    {
        private int _readers;
        private int _writers;
        private readonly HashSet<Guid> _locksHeld = new HashSet<Guid>();
        private readonly ReadOnlyCollection<Guid> _idsOfParentFoldersUpToRoot;

        public FileLockable(FileNode fileId, IEnumerable<FolderNode> foldersPassedFromRootToGetToFile)
        {
            if (fileId == null) throw new ArgumentNullException("fileId");
            if (foldersPassedFromRootToGetToFile == null) throw new ArgumentNullException("foldersPassedFromRootToGetToFile");

            FileId = fileId.Id;

            var nodeIds = new HashSet<Guid>(foldersPassedFromRootToGetToFile.Select(folderNode => folderNode.Id));

            _idsOfParentFoldersUpToRoot = nodeIds.ToList().AsReadOnly();

            if (_idsOfParentFoldersUpToRoot.Count == 0)
            {
                throw new ArgumentException(
                    "Нужна хотя бы одна папка, указанная в качестве точки на пути к блокируемому файлу (файлы вне папок не существуют, root - тоже папка)");
            }
        }

        public ReadOnlyCollection<Guid> IdsOfParentFoldersUpToRoot
        {
            get { return _idsOfParentFoldersUpToRoot; }
        }

        public Guid FileId { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CannotAcquireLockException"></exception>
        /// <exception cref="InvalidOperationException">В случае некорректного использования класса. Ловить это исключение не нужно - пусть падает приложение.</exception>
        public Guid LockForReading()
        {
            this.MakeSureInvariantsHold();

            if (_writers > 0)
            {
                throw new CannotAcquireLockException("Блокировка файла на чтение не удалась: он уже блокирован на запись");
            }

            _readers++;

            Guid newLockId = AllocateNewLockRegisteringIt();

            return newLockId;
        }

        public bool IsFreeForTaking
        {
            get
            {
                return ((_writers == 0) && (_readers == 0));
            }
        }

        private Guid AllocateNewLockRegisteringIt()
        {
            Guid newLockId = Guid.NewGuid();

            _locksHeld.Add(newLockId);
            return newLockId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CannotAcquireLockException"></exception>
        /// <exception cref="InvalidOperationException">В случае некорректного использования класса. Ловить это исключение не нужно - пусть падает приложение.</exception>
        public Guid LockForWriting()
        {
            this.MakeSureInvariantsHold();

            if (_readers > 0)
            {
                throw new CannotAcquireLockException("Не удалось блокировать файл на запись: он уже блокирован на чтение");
            }

            if (_writers > 0)
            {
                throw new CannotAcquireLockException("Не удалось блокировать файл на запись: он уже блокирован на запись");
            }

            _writers++;

            Guid newLockId = AllocateNewLockRegisteringIt();

            return newLockId;
        }

        private void MakeSureInvariantsHold()
        {
            if ((_readers > 0) && (_writers > 0))
            {
                throw new InvalidOperationException("Блокировка файла в неконсистентном состоянии. Работу продолжить нельзя.");
            }
        }

        /// <exception cref="LockNotFoundException"></exception>
        public void ReleaseLock(Guid lockId)
        {
            this.MakeSureInvariantsHold();

            if (!_locksHeld.Remove(lockId))
            {
                throw new LockNotFoundException("Не удалось снять блокировку с идентификатором {0} - она не зарегистрирована.".FormatWith(lockId));
            }

            if (_readers > 0)
            {
                _readers--;
            }
            else if (_writers > 0)
            {
                _writers--;
            }
        }
    }
}