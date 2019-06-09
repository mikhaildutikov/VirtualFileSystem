using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using VirtualFileSystem.ContentsEnumerators;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem
{
    /// <summary>
    /// Виртуальная файловая система.
    /// Note: thread-safe. Должна ли она быть thread-safe - вопрос весьма спорный.
    /// </summary>
    public sealed partial class VirtualFileSystem : IFilesAndFoldersProvider, IDisposable
    {
        /// TODO: разделить большой класс

        private static readonly string _root = System.IO.Path.DirectorySeparatorChar.ToString(); // Note: сильное упрощение. Перенос с одной ОС на другую в текущей версии не поддерживается.
        private static readonly char _directorySeparatorChar = System.IO.Path.DirectorySeparatorChar; // Note: сильное упрощение. Перенос с одной ОС на другую в текущей версии не поддерживается.
        private const int FileCopyingBufferSizeInBytes = 100000;

        private readonly IVirtualDisk _virtualDisk;
        private readonly VirtualFileSystemInfo _fileSystemInfo;
        private readonly IEqualityComparer<string> _namesComparer;
        private readonly NodeResolver _nodeResolver;
        private readonly IFreeBlockManager _freeBlockManager;
        private readonly IFolderEnumeratorRegistry _folderEnumeratorRegistry;
        private readonly PathBuilder _pathBuilder;
        private readonly object _operationExecutionCriticalSection = new object();
        private bool _disposed;
        private readonly FileManager _fileManager;
        private readonly FolderManager _folderManager;

        private VirtualFileSystem(
            IVirtualDisk virtualDisk,
            VirtualFileSystemInfo fileSystemInfo,
            FileSystemNodeStorage fileSystemNodeStorage,
            IEqualityComparer<string> namesComparer,
            NodeResolver nodeResolver,
            IFreeBlockManager freeBlockManager,
            IFolderEnumeratorRegistry folderEnumeratorRegistry,
            BlockReferenceListsEditor blockReferenceListsEditor,
            PathBuilder pathBuilder,
            IFileSystemArtifactNamesValidator nameValidator,
            IPathValidator pathValidator,
            FileManager fileManager,
            FolderManager folderManager)
        {
            if (fileSystemInfo == null) throw new ArgumentNullException("fileSystemInfo");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");
            if (namesComparer == null) throw new ArgumentNullException("namesComparer");
            if (nodeResolver == null) throw new ArgumentNullException("nodeResolver");
            if (freeBlockManager == null) throw new ArgumentNullException("freeBlockManager");
            if (folderEnumeratorRegistry == null) throw new ArgumentNullException("folderEnumeratorRegistry");
            if (blockReferenceListsEditor == null) throw new ArgumentNullException("blockReferenceListsEditor");
            if (pathBuilder == null) throw new ArgumentNullException("pathBuilder");
            if (nameValidator == null) throw new ArgumentNullException("nameValidator");
            if (pathValidator == null) throw new ArgumentNullException("pathValidator");
            if (folderManager == null) throw new ArgumentNullException("folderManager");

            _virtualDisk = virtualDisk;
            _folderManager = folderManager;
            _folderManager = folderManager;
            _folderManager = folderManager;
            _folderManager = folderManager;
            _fileManager = fileManager;
            _pathBuilder = pathBuilder;
            _folderEnumeratorRegistry = folderEnumeratorRegistry;
            _nodeResolver = nodeResolver;
            _freeBlockManager = freeBlockManager;
            _fileSystemInfo = fileSystemInfo;
            _namesComparer = namesComparer;
        }

        /// <summary>
        /// Создает новый файл по указанному пути.
        /// Примечание: в текущей версии не создает недостающие папки.
        /// </summary>
        /// <param name="fullPathToFile">Полный путь (включая имя файла), указывающий, где будет находиться новый файл.</param>
        /// <returns>Сведения о только что созданном файле.</returns>
        /// <exception cref="InvalidPathException">Если указанный путь невалиден.</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException">Если путь (<paramref name="fullPathToFile"/>), за исключением последней части (имени нового файла), указывает на несуществующую папку.</exception>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileCountReachedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public FileInfo CreateFile(string fullPathToFile)
        {
            lock (_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                return _fileManager.CreateFile(fullPathToFile);
            }
        }
        
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        ReadOnlyCollection<FileInfo> IFilesAndFoldersProvider.GetAllFilesFrom(string path)
        {
            lock (_operationExecutionCriticalSection)
            {
                this.ThrowIfDisposed();

                return _nodeResolver.GetAllFilesFrom(path);
            }
        }
        
        /// <summary>
        /// Сведения о файловой системе.
        /// </summary>
        public VirtualFileSystemInfo FileSystemInfo
        {
            get
            {
                return _fileSystemInfo;
            }
        }

        /// <summary>
        /// Путь к корневой папке диска.
        /// </summary>
        public static string Root
        {
            get { return _root; }
        }

        /// <summary>
        /// Символ, используемый для отделения папок друг от друга.
        /// </summary>
        public static char DirectorySeparatorChar
        {
            get { return _directorySeparatorChar; }
        }

        /// <summary>
        /// Создает новую папку по указанному пути.
        /// Примечание: в текущей версии не создает недостающие папки.
        /// </summary>
        /// <param name="fullPathForFolder">Путь, указывающий местоположение новой папки (включая ее имя).</param>
        /// <returns>Описание только что созданной папки.</returns>
        /// <exception cref="InvalidPathException">Если указанный путь невалиден.</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException">Если путь (<paramref name="fullPathForFolder"/>), за исключением последней части (имени новой папки), указывает на несуществующую папку.</exception>
        /// <exception cref="FolderAlreadyExistsException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFolderCountReachedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public FolderInfo CreateFolder(string fullPathForFolder)
        {
            lock(_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                return _folderManager.CreateFolder(fullPathForFolder);
            }
        }

        internal bool FolderExists(string path)
        {
            return _nodeResolver.FolderExists(path);
        }

        /// <summary>
        /// Перемещает файл в указанную папку.
        /// </summary>
        /// <param name="currentPathForFile">Полный путь (от корня) к файлу, который следует переместить.</param>
        /// <param name="pathToFolderThatMustContainTheFile">Полный путь к папке, в которую нужно переместить указанный файл (<paramref name="currentPathForFile"/>).</param>
        /// <returns>Описание перемещенного файла, взятое с учетом его нового местоположения.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLockedException"></exception>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="MaximumFileCountReachedException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        public FileInfo MoveFile(string currentPathForFile, string pathToFolderThatMustContainTheFile)
        {
            lock(_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                return _fileManager.MoveFile(currentPathForFile, pathToFolderThatMustContainTheFile);
            }
        }

        /// <summary>
        /// Перемещает папку в указанную директорию.
        /// </summary>
        /// <param name="currentPathForFolder">Полный путь (от корня) к папке, которую следует переместить.</param>
        /// <param name="pathToFolderThatWillContainTheFolderMoved">Полный путь к папке, в которую нужно переместить указанную директорию (<paramref name="currentPathForFolder"/>).</param>
        /// <returns>Описание перемещенной папки, взятое с учетом ее нового местоположения.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderLockedException"></exception>
        /// <exception cref="FolderAlreadyExistsException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="MaximumFolderCountReachedException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="InvalidOperationException">Если пытаетесь переместить папку в одну из ее собственных поддиректорий.</exception>
        public FolderInfo MoveFolder(string currentPathForFolder, string pathToFolderThatWillContainTheFolderMoved)
        {
            lock(_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                return _folderManager.MoveFolder(currentPathForFolder, pathToFolderThatWillContainTheFolderMoved);
            }
        }

        /// <summary>
        /// Открывает указанный файл для записи и чтения.
        /// </summary>
        /// <param name="fullPathForFile">Путь, указывающий открываемый файл.</param>
        /// <returns>Поток данных файла, допускающий чтение и запись данных</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLockedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public DataStreamReadableWritable OpenFileForWriting(string fullPathForFile)
        {
           lock(_operationExecutionCriticalSection)
           {
               ThrowIfDisposed();

               return _fileManager.OpenFileForWriting(fullPathForFile);
           }
        }

        /// <summary>
        /// Копирует указанный файл.
        /// </summary>
        /// <param name="fullPathForFileToBeCopied">Полный путь (от корня), указывающий файл, который следует скопировать.</param>
        /// <param name="newPathThatWillPointToTheCopyOfFile">Полный путь (от корня), указывающий, где будет находиться и как называться копия файла.</param>
        /// <returns>Описание копии файла.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLockedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidPathException">Если указанный путь невалиден.</exception>
        /// <exception cref="FolderNotFoundException">Если путь, за исключением последней части (имени нового файла), указывает на несуществующую папку.</exception>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileCountReachedException"></exception>
        public FileInfo CopyFile(string fullPathForFileToBeCopied, string newPathThatWillPointToTheCopyOfFile)
        {
            lock (_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();
            }

            return this.CopyFile(fullPathForFileToBeCopied, newPathThatWillPointToTheCopyOfFile, new NullFileSystemCancellableTaskToken());
        }
        
        /// <summary>
        /// Копирует папку со всем ее содержимым в указанное место на виртуальном диске.
        /// 
        /// Note: делает snapshot копируемой папки и упрямо игнорирует все изменения, которые в ней происходят после начала копирования (может подхватить изменения в каком-то из файлов, но не разглядит новых файлов, папок).
        /// Сделано так для простоты. Вариантов реализации этого метода - масса. И все нечеткие, в отличие от других операций файловой системы. К слову, не без хорошей причины такого метода нет (и вряд ли будет) в .Net FW.
        /// </summary>
        /// <param name="fullPathForFolderToCopy">Полный (от корня) путь, указывающий папку, которую следует скопировать.</param>
        /// <param name="destinationPathForFolderAndItsContents"></param>
        /// <returns>Набор объектов, содержащих данные о результатах копирования того или иного файла.</returns>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFolderCountReachedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public ReadOnlyCollection<FileSystemTaskResult> CopyFolder(string fullPathForFolderToCopy, string destinationPathForFolderAndItsContents)
        {
            return this.CopyFolder(fullPathForFolderToCopy, destinationPathForFolderAndItsContents,
                                   new NullFileSystemCancellableTaskToken());
        }

        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFolderCountReachedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        internal ReadOnlyCollection<FileSystemTaskResult> CopyFolder(string fullPathForFolderToCopy, string destinationPathForFolderAndItsContents, IFileSystemCancellableTaskToken taskToken)
        {
            if (taskToken == null) throw new ArgumentNullException("taskToken");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fullPathForFolderToCopy, "fullPathForFolderToCopy");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(destinationPathForFolderAndItsContents, "destinationPathForFolderAndItsContents");

            // TODO: выделить общее у копированиея папок и импорта - там много.

            var allFilesToBeCopied = new List<FileInfo>();

            CreateFoldersNeededToCopy(fullPathForFolderToCopy, destinationPathForFolderAndItsContents, allFilesToBeCopied);

            var copyTaskResults = new List<FileSystemTaskResult>();
            int filesCopied = 0;
            int percentage = 0;

            foreach (FileInfo fileInfo in allFilesToBeCopied)
            {
                try
                {
                    if (taskToken.HasBeenCancelled)
                    {
                        copyTaskResults.Add(new FileTaskResult(fileInfo.ToFileAddressable(), null, "Задача снята."));
                    }
                    else
                    {
                        string relativePathToFile = _pathBuilder.GetRelativePath(fullPathForFolderToCopy, fileInfo.FullPath);
                        string destinationPath = this.PathBuilder.CombinePaths(destinationPathForFolderAndItsContents, relativePathToFile);

                        var tokenWrapper = new TaskTokenPartialWrapper(taskToken);
                        var copiedFileInfo = this.CopyFile(fileInfo.FullPath, destinationPath, tokenWrapper);
                        copyTaskResults.Add(new FileTaskResult(fileInfo.ToFileAddressable(), copiedFileInfo.ToFileAddressable(), String.Empty));
                    }
                }
                catch(TaskCancelledException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                catch (FileNotFoundException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                catch (FileLockedException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                catch(ObjectDisposedException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                catch (InvalidPathException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                catch (FolderNotFoundException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                catch(FileAlreadyExistsException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                catch (InsufficientSpaceException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                catch (MaximumFileCountReachedException exception)
                {
                    copyTaskResults.Add(CreateCopyTaskFailureFromException(fileInfo, exception));
                }
                finally
                {
                    // Note: дурацкая часть по высчету прогресса везде общая. Надо выносить.

                    filesCopied++;

                    int newPercentage = (filesCopied * 100) / allFilesToBeCopied.Count;

                    if (newPercentage != percentage)
                    {
                        percentage = newPercentage;
                        taskToken.ReportProgressChange(newPercentage);
                    }
                }
            }

            return copyTaskResults.AsReadOnly();
        }

        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFolderCountReachedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        private void CreateFoldersNeededToCopy(string fullPathForFolderToCopy, string destinationPathForFolderAndItsContents, List<FileInfo> allFilesToBeCopied)
        {
            Monitor.Enter(_operationExecutionCriticalSection);

            try
            {
                this.ThrowIfDisposed();

                try
                {
                    if (!_namesComparer.Equals(destinationPathForFolderAndItsContents, _root))
                    {
                        this.CreateFolder(destinationPathForFolderAndItsContents);
                    }
                }
                catch (FolderAlreadyExistsException)
                {
                }
                
                List<FolderInfo> subfoldersToCreate;
                var filesToCopy = _nodeResolver.GetAllFilesFromFolderRecursivelyAsPlainLists(fullPathForFolderToCopy, out subfoldersToCreate);

                allFilesToBeCopied.AddRange(filesToCopy);

                foreach (FolderInfo folderInfo in subfoldersToCreate) // Note: гарантируется, что те, что ближе к корню, идут в списке раньше остальных
                {
                    try
                    {
                        string newFolderPathRelative = _pathBuilder.GetRelativePath(fullPathForFolderToCopy, folderInfo.FullPath);

//                        Note: совершенно никак не блокирую папки. Ждущий в бесконечном цикле поток вполне может поудалять созданные папки, как только текущий поток выберется из
//                        критического региона, так что
//                        ни один из файлов (см. код ниже) скопировать не удастся. Я не считаю, что это отлично, делаю так для простоты.

                        this.CreateFolder(this.PathBuilder.CombinePaths(destinationPathForFolderAndItsContents, newFolderPathRelative));
                    }
                    catch (FolderAlreadyExistsException) // в остальных случаях выпадаем - так и задумано. Для простоты очистку игнорирую.
                    {
                    }
                }
            }
            finally
            {
                Monitor.Exit(_operationExecutionCriticalSection);
            }
        }

        private static FileTaskResult CreateCopyTaskFailureFromException(FileInfo fileCopied, Exception exception)
        {
            return new FileTaskResult(fileCopied.ToFileAddressable(), null, exception.Message);
        }

        /// <summary>
        /// Создает указанный файл и тут же (атомарно) открывает его для записи и чтения.
        /// </summary>
        /// <param name="fullPathForFile">Путь, указывающий открываемый файл</param>
        /// <returns>Поток данных файла, допускающий чтение и запись данных</returns>
        /// <exception cref="InvalidPathException">Если указанный путь невалиден</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException">Если путь, за исключением последней части (имени нового файла), указывает на несуществующую папку.</exception>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileCountReachedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public DataStreamReadableWritable CreateAndOpenFileForWriting(string fullPathForFile)
        {
            lock (_operationExecutionCriticalSection)
            {
                this.CreateFile(fullPathForFile);
                return this.OpenFileForWriting(fullPathForFile);
            }
        }

        /// <summary>
        /// Открывает файл для чтения (блокируя тем самым ряд действий над всеми папками вплоть до корня файловой системы).
        /// </summary>
        /// <param name="fullPathForFile">Полный путь к файлу, который следует открыть на чтение.</param>
        /// <returns>Поток данных файла, из которого можно только читать данные.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLockedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public DataStreamReadable OpenFileForReading(string fullPathForFile)
        {
            lock(_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                return _fileManager.OpenFileForReading(fullPathForFile);
            }
        }

        /// <summary>
        /// Возвращает сведения о всех подпапках в указанной директории (<paramref name="folderToGetSubfoldersOf"/>). Работает без рекурсии.
        /// </summary>
        /// <param name="folderToGetSubfoldersOf">Папка, поддиректории которой надо вернуть.</param>
        /// <returns>Сведения о всех папках в указанной директории (<paramref name="folderToGetSubfoldersOf"/>)</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public ReadOnlyCollection<FolderInfo> GetAllFoldersFrom(string folderToGetSubfoldersOf)
        {
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(folderToGetSubfoldersOf, "folderToGetSubfoldersOf");

            lock (_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();
                return _nodeResolver.GetAllFoldersFrom(folderToGetSubfoldersOf);
            }
        }

        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="CannotResolvePathException"></exception>
        FolderInfo IFilesAndFoldersProvider.GetParentOf(string folderPath)
        {
            lock (_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                string fileOrFolderName;

                var nodeResolvingResult =
                    _nodeResolver.ResolveParentFolderNodeByPath(folderPath, out fileOrFolderName).ResolvedNode;

                string parentPath = folderPath.Substring(0, folderPath.Length - fileOrFolderName.Length);

                if (!_pathBuilder.PointsToRoot(parentPath))
                {
                    parentPath = parentPath.TrimEnd(new[] {VirtualFileSystem.DirectorySeparatorChar});
                }

                return new FolderInfo(nodeResolvingResult, parentPath);
            }
        }

        /// <summary>
        /// Удаляет указанный файл.
        /// </summary>
        /// <param name="filePath">Путь, указывающий удаляемый файл.</param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLockedException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DeleteFile(string filePath)
        {
            lock (_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                _fileManager.DeleteFile(filePath);
            }
        }

        /// <summary>
        /// Удаляет пустую папку, расположенную по указанному пути. Непустые папки в этой версии удалить вызовом одного метода
        /// нельзя.
        /// </summary>
        /// <param name="folderPath">Путь, указывающий, какую папку надо удалить.</param>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="FolderNotEmptyException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void DeleteFolder(string folderPath)
        {
            lock(_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                _folderManager.DeleteFolder(folderPath);
            }
        }

        /// <summary>
        /// Переименовывает указанный файл.
        /// </summary>
        /// <param name="fullPathForFile">Путь, указывающий файл для переименования.</param>
        /// <param name="newFileName">Новое имя файла (без пути).</param>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidNameException"></exception>
        /// <exception cref="FileLockedException"></exception>
        public void RenameFile(string fullPathForFile, string newFileName)
        {
            lock(_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                _fileManager.RenameFile(fullPathForFile, newFileName);
            }
        }

        /// <summary>
        /// Создает Iterator для перебора файлов в папке, имена которых удовлетворяют заданной маске (<paramref name="patternNamesOfFilesMustMatch"/>).
        /// Работает рекурсивно - то есть перебирает и файлы во всех подпапках указанной папки.
        /// Note: генерирует исключение (<see cref="InvalidOperationException"/>), если папка, по которой производится обход, меняется во время обхода (итераторы не thread-safe, я бы их лучше не показывал, но это явно зафиксированное требования).
        /// </summary>
        /// <param name="folderToEnumerateFilesIn">Папка, в которой следует искать файлы.</param>
        /// <param name="patternNamesOfFilesMustMatch">Маска для поиска файлов. Звездочка означает любую последовательность символов. Знак вопроса - один любой символ.</param>
        /// <returns>Iterator для перебора файлов в папке, имена которых удовлетворяют заданной маске (<paramref name="patternNamesOfFilesMustMatch"/>)</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public IEnumerator<FileInfo> EnumerateFilesUnderFolder(string folderToEnumerateFilesIn, string patternNamesOfFilesMustMatch)
        {
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(folderToEnumerateFilesIn, "folderToEnumerateFilesIn");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(patternNamesOfFilesMustMatch, "patternNamesOfFilesMustMatch");

            Monitor.Enter(_operationExecutionCriticalSection);

            try
            {
                ThrowIfDisposed();

                var nodeResolvingResult = _nodeResolver.ResolveFolderNodeByPath(folderToEnumerateFilesIn);

                var folder = new FolderInfo(nodeResolvingResult.ResolvedNode, folderToEnumerateFilesIn);

                var enumerator = new FolderContentsEnumerator(folder, patternNamesOfFilesMustMatch, _folderEnumeratorRegistry, this);
                _folderEnumeratorRegistry.RegisterEnumerator(enumerator);

                return enumerator;
            }
            catch(InvalidPathException)
            {
                throw new FolderNotFoundException(
                    "Не удалось найти папку по указанному пути (\"{0}\")".FormatWith(folderToEnumerateFilesIn));
            }
            finally
            {
                Monitor.Exit(_operationExecutionCriticalSection);
            }
        }

        /// <summary>
        /// Копирует указанный файл.
        /// </summary>
        /// <param name="fullPathForFileToBeCopied">Полный путь (от корня), указывающий файл, который следует скопировать.</param>
        /// <param name="newPathThatWillPointToTheCopyOfFile">Полный путь (от корня), указывающий, где будет находиться и как называться копия файла.</param>
        /// <param name="taskToken"></param>
        /// <returns>Описание копии файла.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLockedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileCountReachedException"></exception>
        /// <exception cref="TaskCancelledException"></exception>
        internal FileInfo CopyFile(string fullPathForFileToBeCopied, string newPathThatWillPointToTheCopyOfFile, IFileSystemCancellableTaskToken taskToken)
        {
            if (taskToken == null) throw new ArgumentNullException("taskToken");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fullPathForFileToBeCopied, "fullPathForFileToBeCopied");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(newPathThatWillPointToTheCopyOfFile, "newPathThatWillPointToTheCopyOfFile");

            if (_namesComparer.Equals(fullPathForFileToBeCopied, newPathThatWillPointToTheCopyOfFile))
            {
                return
                    new FileInfo(_nodeResolver.ResolveFileNodeByPath(newPathThatWillPointToTheCopyOfFile).ResolvedNode, newPathThatWillPointToTheCopyOfFile);
            }

            lock(_operationExecutionCriticalSection)
            {
                ThrowIfDisposed(); // так себе проверка на этот раз.
            }

            // Note: не смотрю, хватит ли там места. Хотя отрицательный ответ на такой вопрос мог бы стать поводом, чтобы ничего не копировать.
            try
            {
                CopyFileContents(taskToken, fullPathForFileToBeCopied, newPathThatWillPointToTheCopyOfFile);

                return
                    new FileInfo(_nodeResolver.ResolveFileNodeByPath(newPathThatWillPointToTheCopyOfFile).ResolvedNode, newPathThatWillPointToTheCopyOfFile);
            }
            catch (Exception)
            {
                try
                {
                    this.DeleteFile(newPathThatWillPointToTheCopyOfFile);
                }
                catch (InvalidPathException)
                {
                }
                catch (ObjectDisposedException)
                {
                    throw new ObjectDisposedException("Работа с файловой системой была вами завершена принудительным вызовом метода Dispose() - до того, как был докопирован файл. Рекомендуется удалить следующий файл при очередном запуске системы (простите - в текущей версии ничего более удобного не сделано): \"{0}\"".FormatWith(newPathThatWillPointToTheCopyOfFile));
                }
                catch (FileNotFoundException)
                {
                }

                throw;
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLockedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileCountReachedException"></exception>
        /// <exception cref="MaximumFileSizeReachedException"></exception>
        /// <exception cref="TaskCancelledException"></exception>
        private void CopyFileContents(IFileSystemCancellableTaskToken taskToken, string fullPathForFileToBeCopied, string newPathThatWillPointToTheCopyOfFile)
        {
            using (var readingStream = this.OpenFileForReading(fullPathForFileToBeCopied))
            {
                double totalNumberOfBytesToWrite = readingStream.Length;
                int percentage = 0;
                double numberOfBytesWritten = 0;

                var buffer = new byte[FileCopyingBufferSizeInBytes];

                using (var writingStream = this.CreateAndOpenFileForWriting(newPathThatWillPointToTheCopyOfFile))
                {
                    int numberOfBytesRead;

                    while ((numberOfBytesRead = readingStream.Read(buffer, 0, FileCopyingBufferSizeInBytes)) != 0)
                    {
                        writingStream.Write(buffer, 0, numberOfBytesRead);

                        numberOfBytesWritten += numberOfBytesRead;

                        int newPercentage = (int)((numberOfBytesWritten / totalNumberOfBytesToWrite) * 100);

                        if (newPercentage != percentage)
                        {
                            percentage = newPercentage;
                            taskToken.ReportProgressChange(newPercentage);
                        }

                        if (taskToken.HasBeenCancelled)
                        {
                            throw new TaskCancelledException("Задача снята");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Переименовывает папку.
        /// </summary>
        /// <param name="fullPathForFolder">Путь, указывающий папку, которую следует переименовать.</param>
        /// <param name="newFolderName">Новое имя папки (только имя - без пути)</param>
        /// <exception cref="FolderAlreadyExistsException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="FolderLockedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidNameException"></exception>
        public void RenameFolder(string fullPathForFolder, string newFolderName)
        {
            lock(_operationExecutionCriticalSection)
            {
                ThrowIfDisposed();

                _folderManager.RenameFolder(fullPathForFolder, newFolderName);
            }
        }

        /// <summary>
        /// Объем свободного дискового пространства (в байтах).
        /// </summary>
        public int FreeSpaceInBytes
        {
            get
            {
                lock(_operationExecutionCriticalSection)
                {
                    ThrowIfDisposed();
                    return _freeBlockManager.FreeBlockCount * _virtualDisk.BlockSizeInBytes;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Файловая система ({0})".FormatWith(this.GetType().FullName), "Работа объекта уже была вами завершена (путем вызова метода Dispose).");
            }
        }

        /// <summary>
        /// Освободить ресурсы файловой системы (в частности - закрыть файл, который система использует в качестве хранилища данных).
        /// После вызова этого метода совершать операции с файловой системой будет нельзя.
        /// Note: в текущей версии этот метод можно вызывать только, когда вы завершили все операции копирования. В противном случае вы можете получить
        /// неконсистентность в данных копий файлов, которую вы делали, когда вызвали Dispose. Это обходится - при помощи довольно тягомотного кода, который я сейчас писать не стану. Умный Dispose для
        /// thread-safe вещей стоит немало (и всегда, кстати, спорный вопрос, как он должен себя вести).
        /// Design Note: Проще не иметь здесь IDisposable вовсе, но, считаю, у пользователя все же должна быть возможность
        /// явно отпустить все Unmanaged-ресурсы, в данном случае - файл, в котором система хранит данные. Хоть при этом у них и появляется другая возможность - помешать собственным же операциям копирования.
        /// Design Note: По-хорошему, все, что использует IDisposable-вещи, должно быть само IDisposable. У меня же здесь сделано, что
        /// IDisposable реализует только сама файловая система и виртуальный диск: кроме файловой системы пользователю все равно из библиотеки
        /// почти ничего не доступно (не public).
        /// </summary>
        public void Dispose()
        {
            lock(_operationExecutionCriticalSection)
            {
                if (_disposed)
                {
                    return;
                }
           
                _disposed = true; // ничего делать больше нельзя
                _virtualDisk.Dispose();
            }
        }

        /// <summary>
        /// Предоставляет скромный функционал для работы с путями в файловой системе.
        /// </summary>
        public PathBuilder PathBuilder
        {
            get { return _pathBuilder; }
        }
    }
}