using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

// ReSharper disable CheckNamespace
namespace VirtualFileSystem
// ReSharper restore CheckNamespace
{
    ///<summary>
    ///</summary>
    public sealed class FolderContentsEnumerator : IEnumerator<FileInfo>
    {
        private readonly FolderInfo _folderToEnumerateFilesIn;
        private readonly string _patternForMatchingNamesOfFiles;
        private readonly IFolderEnumeratorUnregistrar _enumeratorRegistry;
        private readonly IFilesAndFoldersProvider _filesAndFoldersProvider;
        private volatile bool _enumeratedStructureHasChanged;
        private bool _disposed;
        private bool _finishedEnumeration;
        private readonly HashSet<Guid> _idsOfFilesAndFoldersSeenSoFar = new HashSet<Guid>();
        private readonly Wildcard _wildcardForMatchingNamesOfFiles;
        private FileInfo _current;
        private FolderInfo _currentFolder;
        private List<FolderInfo> _subfoldersOfCurrentFolder;
        private List<FileInfo> _filesOfCurrentFolder;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderToEnumerateFilesIn"></param>
        /// <param name="patternForMatchingNamesOfFiles"></param>
        /// <param name="enumeratorRegistry"></param>
        /// <param name="filesAndFoldersProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal FolderContentsEnumerator(FolderInfo folderToEnumerateFilesIn, string patternForMatchingNamesOfFiles, IFolderEnumeratorUnregistrar enumeratorRegistry, IFilesAndFoldersProvider filesAndFoldersProvider)
        {
            try
            {
                if (folderToEnumerateFilesIn == null) throw new ArgumentNullException("folderToEnumerateFilesIn");
                if (enumeratorRegistry == null) throw new ArgumentNullException("enumeratorRegistry");
                if (filesAndFoldersProvider == null) throw new ArgumentNullException("filesAndFoldersProvider");

                if (String.IsNullOrEmpty(patternForMatchingNamesOfFiles))
                {
                    throw new ArgumentNullException("patternForMatchingNamesOfFiles");
                }

                _folderToEnumerateFilesIn = folderToEnumerateFilesIn;
                _patternForMatchingNamesOfFiles = patternForMatchingNamesOfFiles;
                _enumeratorRegistry = enumeratorRegistry;
                _filesAndFoldersProvider = filesAndFoldersProvider;
                _wildcardForMatchingNamesOfFiles = new Wildcard(patternForMatchingNamesOfFiles);
                _currentFolder = folderToEnumerateFilesIn;
            }
            catch (Exception)
            {
                GC.SuppressFinalize(this); // Finalizer вызывается даже если возникло исключение при конструировании объекта.

                throw;
            }
        }

        /// <summary>
        /// Маска, которая используется итератором для поиска файлов по именам.
        /// </summary>
        public string PatternForMatchingNamesOfFiles
        {
            get { return _patternForMatchingNamesOfFiles; }
        }

        internal void MarkAsInvalid()
        {
            _enumeratedStructureHasChanged = true;
        }

        /// <summary>
        /// Note: может выполняться только в пределах регионов, защищенных критической секцией _stateChangeCriticalSection.
        /// </summary>
        private void MakeSureObjectHasNotBeenDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public void Dispose()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public bool MoveNext()
        {
            this.MakeSureObjectHasNotBeenDisposed();

            if (_finishedEnumeration)
            {
                return false;
            }

            if (_enumeratedStructureHasChanged)
            {
                throw CreateEnumerationNotPossibleException();
            }

            FileInfo nextFile = this.SearchFileInFolderRecursively(_currentFolder);

            if (nextFile == null)
            {
                _finishedEnumeration = true; // Note: можно использовать pattern State.
                return false;
            }
            else
            {
                _current = nextFile;
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderToSearchIn"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        private FileInfo SearchFileInFolderRecursively(FolderInfo folderToSearchIn)
        {
            try
            {
                RefreshCurrentFolder(folderToSearchIn);

                if (!_idsOfFilesAndFoldersSeenSoFar.Contains(folderToSearchIn.Id))
                {
                    foreach (FileInfo fileInfo in _filesOfCurrentFolder)
                    {
                        if (!_idsOfFilesAndFoldersSeenSoFar.Contains(fileInfo.Id))
                        {
                            _idsOfFilesAndFoldersSeenSoFar.Add(fileInfo.Id);

                            if (_wildcardForMatchingNamesOfFiles.CheckStringForMatch(fileInfo.Name))
                            {
                                return fileInfo;
                            }
                        }
                    }

                    foreach (FolderInfo folder in _subfoldersOfCurrentFolder)
                    {
                        if (!_idsOfFilesAndFoldersSeenSoFar.Contains(folder.Id))
                        {
                            FileInfo fileFromFolder = SearchFileInFolderRecursively(folder);

                            if (fileFromFolder != null)
                            {
                                return fileFromFolder;
                            }
                        }
                    }

                    _idsOfFilesAndFoldersSeenSoFar.Add(folderToSearchIn.Id);

                    // пошли к parent-y
                    if (_folderToEnumerateFilesIn.Id != folderToSearchIn.Id)
                    {
                        try
                        {
                            FolderInfo parent = _filesAndFoldersProvider.GetParentOf(folderToSearchIn.FullPath);

                            FileInfo fileFromFolder = this.SearchFileInFolderRecursively(parent);

                            if (fileFromFolder != null)
                            {
                                return fileFromFolder;
                            }
                        }
                        catch (InvalidPathException)
                        {
                            return null;
                        }
                        catch(CannotResolvePathException)
                        {
                            throw new InvalidPathException();
                        }
                    }
                }
            }
            catch(ObjectDisposedException)
            {
                this.Dispose();
                throw new ObjectDisposedException("Продолжение перебора файлов невозможно: файловая система уже была принудительно закрыта (путем вызова метода Dispose()).");
            }
            catch (FolderNotFoundException)
            {
                _enumeratedStructureHasChanged = true;
                throw CreateEnumerationNotPossibleException();
            }
            catch (InvalidPathException)
            {
                _enumeratedStructureHasChanged = true;
                throw CreateEnumerationNotPossibleException();
            }

            return null;
        }

        private void RefreshCurrentFolder(FolderInfo folderToSearchIn)
        {
            if (!Object.ReferenceEquals(_currentFolder, folderToSearchIn) || (_filesOfCurrentFolder == null) || (_subfoldersOfCurrentFolder == null))
            {
                _currentFolder = folderToSearchIn;
                _filesOfCurrentFolder = _filesAndFoldersProvider.GetAllFilesFrom(_currentFolder.FullPath).ToList();
                _subfoldersOfCurrentFolder = _filesAndFoldersProvider.GetAllFoldersFrom(_currentFolder.FullPath).ToList();
            }
        }

        private InvalidOperationException CreateEnumerationNotPossibleException()
        {
            return new InvalidOperationException("Продолжение перебора файлов невозможно: изменилась структура папки, в которой производится поиск (\"{0}\")".FormatWith(FolderBeingEnumerated.FullPath));
        }

        /// <summary>
        /// </summary>
        /// <param name="callingFromFinalizer">Note: намеренно использую инверсию имени параметра. Мне почему-то всегда казалось менее внятным обычное для паттерна название этого параметра - disposing.</param>
        private void Dispose(bool callingFromFinalizer)
        {
            if (_disposed)
            {
                return;
            }

            if (!callingFromFinalizer)
            {
                GC.SuppressFinalize(this);
            }

            _enumeratorRegistry.Unregister(this);
            _disposed = true;
        }

        public void Reset()
        {
            throw new NotSupportedException("В текущей версии операция перезагрузки итератора не поддерживается. Пересоздайте итератор, пожалуйста.");
        }

        /// <summary>
        /// Очередной файл, соответствующий маске. До первого успешного, вернувшего true вызова MoveNext(), здесь null. Также сделаны итераторы .Net FW.
        /// </summary>
        public FileInfo Current
        {
            get
            {
                this.MakeSureObjectHasNotBeenDisposed();
                return _current;
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        internal FolderInfo FolderBeingEnumerated
        {
            get { return _folderToEnumerateFilesIn; }
        }

        ~FolderContentsEnumerator()
        {
            this.Dispose(true);
        }
    }
}