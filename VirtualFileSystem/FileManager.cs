using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Extensions;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Streaming;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem
{
    /// <summary>
    /// TODO: слишком большой класс.
    /// </summary>
    internal class FileManager
    {
        private readonly IVirtualDisk _virtualDisk;
        private readonly FileSystemNodeStorage _fileSystemNodeStorage;
        private readonly IEqualityComparer<string> _namesComparer;
        private readonly NodeResolver _nodeResolver;
        private readonly IFreeBlockManager _freeBlockManager;
        private readonly IFolderEnumeratorRegistry _folderEnumeratorRegistry;
        private readonly IFileSystemObjectLockingManager _lockingManager;
        private readonly BlockReferenceListsEditor _blockReferenceListsEditor;
        private readonly PathBuilder _pathBuilder;
        private readonly IFileSystemArtifactNamesValidator _nameValidator;
        private readonly IPathValidator _pathValidator;

        public FileManager(IVirtualDisk virtualDisk, FileSystemNodeStorage fileSystemNodeStorage, IEqualityComparer<string> namesComparer, NodeResolver nodeResolver, IFreeBlockManager freeBlockManager, IFolderEnumeratorRegistry folderEnumeratorRegistry, IFileSystemObjectLockingManager lockingManager, BlockReferenceListsEditor blockReferenceListsEditor, PathBuilder pathBuilder, IFileSystemArtifactNamesValidator nameValidator, IPathValidator pathValidator)
        {
            if (virtualDisk == null) throw new ArgumentNullException("virtualDisk");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");
            if (namesComparer == null) throw new ArgumentNullException("namesComparer");
            if (nodeResolver == null) throw new ArgumentNullException("nodeResolver");
            if (freeBlockManager == null) throw new ArgumentNullException("freeBlockManager");
            if (folderEnumeratorRegistry == null) throw new ArgumentNullException("folderEnumeratorRegistry");
            if (lockingManager == null) throw new ArgumentNullException("lockingManager");
            if (blockReferenceListsEditor == null) throw new ArgumentNullException("blockReferenceListsEditor");
            if (pathBuilder == null) throw new ArgumentNullException("pathBuilder");
            if (nameValidator == null) throw new ArgumentNullException("nameValidator");
            if (pathValidator == null) throw new ArgumentNullException("pathValidator");

            _virtualDisk = virtualDisk;
            _pathValidator = pathValidator;
            _nameValidator = nameValidator;
            _pathBuilder = pathBuilder;
            _blockReferenceListsEditor = blockReferenceListsEditor;
            _lockingManager = lockingManager;
            _folderEnumeratorRegistry = folderEnumeratorRegistry;
            _freeBlockManager = freeBlockManager;
            _nodeResolver = nodeResolver;
            _namesComparer = namesComparer;
            _fileSystemNodeStorage = fileSystemNodeStorage;
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fullPathToFile, "fullPathToFile");

            if (_pathBuilder.PointsToRoot(fullPathToFile))
            {
                throw new InvalidPathException("Не удалось создать файл: в качестве пути к файлу нельзя указать корень файловой системы");
            }

            _pathValidator.Validate(fullPathToFile);

            try
            {
                string fileName;

                var nodeResolvingResult = _nodeResolver.ResolveParentFolderNodeByPath(fullPathToFile, out fileName);

                FolderNode parentFolder = nodeResolvingResult.ResolvedNode;

                var newFileInfo = new FileInfo(this.CreateFile(parentFolder, fileName), fullPathToFile);

                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(nodeResolvingResult.FoldersPassedWhileResolving);

                return newFileInfo;
            }
            catch (InsufficientSpaceException)
            {
                throw new InsufficientSpaceException("Недостаточно свободного места на диске для создания файла.");
            }
            catch (MaximumFileSizeReachedException)
            {
                throw new MaximumFileCountReachedException("Не удалось добавить файл: папка, в которую вы добавляете файл, не может вместить их больше, чем уже вмещает.");
            }
            catch (NoFreeBlocksException)
            {
                throw new InsufficientSpaceException("Недостаточно свободного места на диске для создания файла.");
            }
            catch (InvalidPathException)
            {
                throw new FolderNotFoundException("Не получилось создать файл по указанному пути (\"{0}\"). Не найдена папка, в которой следует создать файл. (В текущей версии недостающие папки не создаются автоматически)".FormatWith(fullPathToFile));
            }
            catch (CannotResolvePathException)
            {
                throw new FolderNotFoundException("Не получилось создать файл по указанному пути (\"{0}\"). Не найдена папка, в которой следует создать файл. (В текущей версии недостающие папки не создаются автоматически)".FormatWith(fullPathToFile));
            }
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="NoFreeBlocksException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileSizeReachedException"></exception>
        private FileNode CreateFile(FolderNode parentFolder, string fileName)
        {
            MethodArgumentValidator.ThrowIfNull(parentFolder, "parentFolder");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fileName, "fileName");

            var filesInTheFolder = _nodeResolver.GetAllFilesFrom(parentFolder);

            var fileWithSameName = filesInTheFolder.FirstOrDefault(file => _namesComparer.Equals(file.Name, fileName));

            if (fileWithSameName != null)
            {
                throw new FileAlreadyExistsException("Файл с именем \"{0}\" уже существует в папке \"{1}\"".FormatWith(fileName, parentFolder.Name));
            }

            var freeBlocks = _freeBlockManager.AcquireFreeBlocks(2);

            int blockToStoreDefinitionIn = freeBlocks[0];
            int blockToStoreFileNodeIn = freeBlocks[1];

            var fileContentsStreamDefinition = new DataStreamDefinition(blockToStoreDefinitionIn, 0);

            try
            {
                // Note: это добавление ссылки может стать последней каплей для заполненного диска, тогда блоки придется отпустить
                _blockReferenceListsEditor.AddBlockReference(blockToStoreFileNodeIn, parentFolder.FileReferencesStreamDefinition, parentFolder);
            }
            catch (Exception)
            {
                _freeBlockManager.MarkBlocksAsFree(freeBlocks);

                throw;
            }

            var creationTime = DateTime.UtcNow;

            var fileNode = new FileNode(fileName, Guid.NewGuid(), blockToStoreFileNodeIn, fileContentsStreamDefinition, creationTime, creationTime, Guid.NewGuid());

            _fileSystemNodeStorage.WriteNode(fileNode);

            return fileNode;
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(filePath, "filePath");

            if (_pathBuilder.PointsToRoot(filePath))
            {
                throw new InvalidPathException("Не получилось удалить файл: нельзя удалить корень файловой системы.");
            }

            try
            {
                string fileName;

                var parentFolderResolvingResult = _nodeResolver.ResolveParentFolderNodeByPath(filePath, out fileName);

                ReadOnlyCollection<FileNode> files;
                FolderNode folderToRemoveFileFrom = parentFolderResolvingResult.ResolvedNode;
                FileNode fileToRemove = ResolveFileNodeThrowingUserFriendlyErrors(filePath, parentFolderResolvingResult, fileName, "удалить", out files);

                // очистка всех данных.
                using (var dataStream = this.OpenFileForWriting(filePath))
                {
                    dataStream.Truncate();
                }

                // TODO: поглядеть на исключения этого метода.
                _blockReferenceListsEditor.TakeOutABlockFromBlockReferenceList(folderToRemoveFileFrom, fileToRemove.DiskBlockIndex, folderToRemoveFileFrom.FileReferencesStreamDefinition);

                _freeBlockManager.MarkBlockAsFree(fileToRemove.DiskBlockIndex);
                _freeBlockManager.MarkBlockAsFree(fileToRemove.FileContentsStreamDefinition.ContentsBlockIndex);

                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(parentFolderResolvingResult.FoldersPassedWhileResolving);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(parentFolderResolvingResult.ResolvedNode.Id);
            }
            catch (CannotResolvePathException)
            {
                throw new FileNotFoundException("Не удалось найти файл {0}".FormatWith(filePath));
            }
            catch (InvalidPathException)
            {
                throw new FileNotFoundException("Не удалось найти файл {0}".FormatWith(filePath));
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fullPathForFile, "fullPathForFile");

            try
            {
                NodeWithSurroundingsResolvingResult<FileNode> nodeResolvingResult;

                DataStreamStructureBuilder dataStreamStructureBuilder = GetDataStreamStructureBuilderResolvingTheFile(fullPathForFile, out nodeResolvingResult);

                Guid lockId = _lockingManager.AcquireLock(nodeResolvingResult, LockKind.Read);

                return new DataStreamToReadableAdapter(new DataStream(dataStreamStructureBuilder, AddressingSystemParameters.Default.BlockSize, _lockingManager, lockId));
            }
            catch (InvalidPathException)
            {
                throw new FileNotFoundException("Файл не найден по указанному пути (\"{0}\")".FormatWith(fullPathForFile));
            }
            catch (CannotAcquireLockException)
            {
                throw new FileLockedException("Не удалось открыть файл \"{0}\" с блокировкой на чтение: он уже заблокирован на запись.".FormatWith(fullPathForFile));
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fullPathForFile, "fullPathForFile");

            try
            {
                NodeWithSurroundingsResolvingResult<FileNode> nodeResolvingResult;
                DataStreamStructureBuilder dataStreamStructureBuilder = GetDataStreamStructureBuilderResolvingTheFile(fullPathForFile, out nodeResolvingResult);

                Guid lockId = _lockingManager.AcquireLock(nodeResolvingResult, LockKind.Write);

                var stream = new DataStream(dataStreamStructureBuilder, AddressingSystemParameters.Default.BlockSize, _lockingManager, lockId);

                var nodeUpdatingStream = new DataStreamNodeUpdating(stream, nodeResolvingResult.ResolvedNode, _fileSystemNodeStorage);

                return nodeUpdatingStream;
            }
            catch (InvalidPathException)
            {
                throw new FileNotFoundException("Файл не найден по указанному пути (\"{0}\")".FormatWith(fullPathForFile));
            }
            catch (CannotAcquireLockException)
            {
                throw new FileLockedException(
                    "Не удалось открыть файл \"{0}\" с блокировкой на запись: он уже заблокирован на чтение или запись."
                        .FormatWith(fullPathForFile));
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fullPathForFile, "fullPathForFile");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(newFileName, "newFileName");

            if (_pathBuilder.PointsToRoot(fullPathForFile))
            {
                throw new InvalidPathException("Файл не был переименован: имя файла, который следует переименовать, не может указывать на корень файловой системы");
            }

            _nameValidator.Validate(newFileName);

            try
            {
                string fileName;

                var parentFolderResolvingResult = _nodeResolver.ResolveParentFolderNodeByPath(fullPathForFile, out fileName);

                string lastPartOfFullPath = _pathBuilder.GetFileOrFolderName(fullPathForFile);

                if (_namesComparer.Equals(newFileName, lastPartOfFullPath))
                {
                    return;
                }

                ReadOnlyCollection<FileNode> files;
                FileNode fileNode = this.ResolveFileNodeThrowingUserFriendlyErrors(fullPathForFile, parentFolderResolvingResult, fileName, "переименовать", out files);

                if (files.Any(node => _namesComparer.Equals(newFileName, node.Name)))
                {
                    throw new FileAlreadyExistsException("Не удалось переименовать файл: файл с именем \"{0}\" уже существует в папке, где вы производите переименование".FormatWith(newFileName));
                }

                fileNode.Name = newFileName;

                _fileSystemNodeStorage.WriteNode(fileNode);

                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(parentFolderResolvingResult.FoldersPassedWhileResolving);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(parentFolderResolvingResult.ResolvedNode.Id);
            }
            catch (CannotResolvePathException)
            {
                throw new FileNotFoundException("Не удалось найти файл {0}".FormatWith(fullPathForFile));
            }
            catch (InvalidPathException)
            {
                throw new FileNotFoundException("Не удалось найти файл {0}".FormatWith(fullPathForFile));
            }
        }

        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLockedException"></exception>
        private FileNode ResolveFileNodeThrowingUserFriendlyErrors(
            string filePath,
            NodeResolvingResult<FolderNode> parentFolderResolvingResult,
            string fileName,
            string verbForPuttingIntoExceptionMessageTemplate,
            out ReadOnlyCollection<FileNode> filesFromSameFolder)
        {
            FileNode fileNode = _nodeResolver.ResolveFileNode(filePath, parentFolderResolvingResult,
                                                                              fileName,
                                                                              verbForPuttingIntoExceptionMessageTemplate,
                                                                              out filesFromSameFolder);

            if (_lockingManager.IsFileLocked(fileNode))
            {
                throw new FileLockedException("Не получилось {0} файл (\"{1}\"): он открыт для чтения или записи.".FormatWith(verbForPuttingIntoExceptionMessageTemplate, filePath));
            }

            return fileNode;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPathForFile"></param>
        /// <param name="nodeResolvingResult"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        private DataStreamStructureBuilder GetDataStreamStructureBuilderResolvingTheFile(string fullPathForFile, out NodeWithSurroundingsResolvingResult<FileNode> nodeResolvingResult)
        {
            nodeResolvingResult = _nodeResolver.ResolveFileNodeByPath(fullPathForFile);

            FileNode fileNode = nodeResolvingResult.ResolvedNode;

            return new DataStreamStructureBuilder(fileNode.FileContentsStreamDefinition, _virtualDisk, _freeBlockManager, _fileSystemNodeStorage, fileNode, AddressingSystemParameters.Default);
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(currentPathForFile, "currentPathForFile");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(pathToFolderThatMustContainTheFile, "pathToFolderThatMustContainTheFile");

            try
            {
                var newParentNodeResolvingResult = _nodeResolver.ResolveFolderNodeByPath(pathToFolderThatMustContainTheFile);

                FolderNode filesNewHome = newParentNodeResolvingResult.ResolvedNode;

                string fileName;

                var currentParentFolder = _nodeResolver.ResolveParentFolderNodeByPath(currentPathForFile, out fileName);

                ReadOnlyCollection<FileNode> allFilesInTheFolder;
                FolderNode folderToRemoveFileFrom = currentParentFolder.ResolvedNode;
                FileNode fileToMove = this.ResolveFileNodeThrowingUserFriendlyErrors(currentPathForFile, currentParentFolder, fileName, "переместить", out allFilesInTheFolder);

                var allFilesInDestinationFolder = _nodeResolver.GetAllFilesFrom(filesNewHome);

                // перемещение в то место, где файл и так уже находится.
                if (newParentNodeResolvingResult.ResolvedNode.Id.Equals(currentParentFolder.ResolvedNode.Id))
                {
                    return new FileInfo(allFilesInDestinationFolder.First(fileNode => _namesComparer.Equals(fileNode.Name, fileName)), currentPathForFile);
                }

                if (allFilesInDestinationFolder.Any(fileNode => _namesComparer.Equals(fileNode.Name, fileName)))
                {
                    throw new FileAlreadyExistsException("Перемещение файла невозможно: в папке \"{0}\" уже существует файл с именем \"{1}\"".FormatWith(pathToFolderThatMustContainTheFile, fileName));
                }

                // сначала добавляем ссылку в новую папку, потом удаляем ссылку из старой (на добавление может не хватить места, удалить же можно всегда).
                _blockReferenceListsEditor.AddBlockReference(fileToMove.DiskBlockIndex, filesNewHome.FileReferencesStreamDefinition, filesNewHome);
                // TODO: поглядеть на исключения этого метода.
                _blockReferenceListsEditor.TakeOutABlockFromBlockReferenceList(folderToRemoveFileFrom, fileToMove.DiskBlockIndex, folderToRemoveFileFrom.FileReferencesStreamDefinition);

                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(newParentNodeResolvingResult.FoldersPassedWhileResolving);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(newParentNodeResolvingResult.ResolvedNode.Id);
                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(currentParentFolder.FoldersPassedWhileResolving);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(currentParentFolder.ResolvedNode.Id);

                allFilesInDestinationFolder = _nodeResolver.GetAllFilesFrom(filesNewHome);

                return new FileInfo(
                    allFilesInDestinationFolder.Single(fileNode => _namesComparer.Equals(fileNode.Name, fileName)),
                    _pathBuilder.CombinePaths(pathToFolderThatMustContainTheFile, fileName));
            }
            catch (InsufficientSpaceException)
            {
                throw new InsufficientSpaceException("Не удалось переместить файл из \"{0}\" в \"{1}\" - не хватает места на диске для проведения операции.".FormatWith(currentPathForFile, pathToFolderThatMustContainTheFile));
            }
            catch (MaximumFileSizeReachedException)
            {
                throw new MaximumFileCountReachedException("Не удалось переместить файл \"{0}\". Папка \"{1}\" не может вместить больше файлов, чем уже вмещает".FormatWith(currentPathForFile, pathToFolderThatMustContainTheFile));
            }
            catch (CannotResolvePathException exception)
            {
                throw new FolderNotFoundException("Не удалось найти папку, где находится перемещаемый файл, или папку, в которую его следует переместить. {0}".FormatWith(exception.Message));
            }
            catch (InvalidPathException exception)
            {
                throw new FolderNotFoundException("Не удалось найти папку, где находится перемещаемый файл, или папку, в которую его следует переместить. {0}".FormatWith(exception.Message));
            }
        }
    }
}