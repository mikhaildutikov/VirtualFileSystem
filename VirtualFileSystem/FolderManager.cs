using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Extensions;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem
{
    internal class FolderManager
    {
        // TODO: я большой, раздели меня
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

        public FolderManager(FileSystemNodeStorage fileSystemNodeStorage, IEqualityComparer<string> namesComparer, NodeResolver nodeResolver, IFreeBlockManager freeBlockManager, IFolderEnumeratorRegistry folderEnumeratorRegistry, IFileSystemObjectLockingManager lockingManager, BlockReferenceListsEditor blockReferenceListsEditor, PathBuilder pathBuilder, IFileSystemArtifactNamesValidator nameValidator, IPathValidator pathValidator)
        {
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

            _fileSystemNodeStorage = fileSystemNodeStorage;
            _pathValidator = pathValidator;
            _nameValidator = nameValidator;
            _pathBuilder = pathBuilder;
            _blockReferenceListsEditor = blockReferenceListsEditor;
            _lockingManager = lockingManager;
            _folderEnumeratorRegistry = folderEnumeratorRegistry;
            _freeBlockManager = freeBlockManager;
            _nodeResolver = nodeResolver;
            _namesComparer = namesComparer;
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fullPathForFolder, "fullPathForFolder");

            if (_pathBuilder.PointsToRoot(fullPathForFolder))
            {
                throw new InvalidPathException("Не удалось создать папку: в качестве пути для новой папки нельзя задать корень файловой системы");
            }

            _pathValidator.Validate(fullPathForFolder);

            try
            {
                string folderName;

                NodeResolvingResult<FolderNode> nodeResolvingResult = _nodeResolver.ResolveParentFolderNodeByPath(fullPathForFolder, out folderName);

                FolderNode parentFolder = nodeResolvingResult.ResolvedNode;

                var newFolderInfo = new FolderInfo(this.CreateFolder(parentFolder, folderName), fullPathForFolder);

                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(nodeResolvingResult.FoldersPassedWhileResolving);

                return newFolderInfo;
            }
            catch (InsufficientSpaceException)
            {
                throw new InsufficientSpaceException("Недостаточно свободного места на диске для создания папки.");
            }
            catch (MaximumFileSizeReachedException)
            {
                throw new MaximumFolderCountReachedException("Не удалось добавить папку: директория, в которую вы добавляете поддиректорию, не может вместить их больше, чем уже вмещает.");
            }
            catch (NoFreeBlocksException)
            {
                throw new InsufficientSpaceException("Недостаточно свободного места на диске для создания папки.");
            }
            catch (CannotResolvePathException)
            {
                throw new FolderNotFoundException("Не получилось создать папку по указанному пути (\"{0}\"). Не найдена папка, в которой следует создать подпапку. (В текущей версии недостающие папки не создаются автоматически)".FormatWith(fullPathForFolder));
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(currentPathForFolder, "cucurrentPathForFolderrrentPathForFile");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(pathToFolderThatWillContainTheFolderMoved, "pathToFolderThatWillContainTheFolderMoved");

            try
            {
                var newParentNodeResolvingResult =
                    _nodeResolver.ResolveFolderNodeByPath(pathToFolderThatWillContainTheFolderMoved);

                FolderNode foldersNewHome = newParentNodeResolvingResult.ResolvedNode;

                string folderName;

                var currentParentFolder = _nodeResolver.ResolveParentFolderNodeByPath(currentPathForFolder, out folderName);

                FolderNode folderToRemoveDirectoryFrom = currentParentFolder.ResolvedNode;

                var directoriesInFolder = _nodeResolver.GetAllFoldersFrom(folderToRemoveDirectoryFrom);

                FolderNode folderToMove =
                    directoriesInFolder.FirstOrDefault(fileNode => _namesComparer.Equals(fileNode.Name, folderName));

                if (folderToMove == null)
                {
                    throw new FolderNotFoundException("Не найдена папка для перемещения (\"{0}\")".FormatWith(currentPathForFolder));
                }

                if (newParentNodeResolvingResult.FoldersPassedWhileResolving.Any(folderNode => folderNode.Id.Equals(folderToMove.Id)))
                {
                    throw new InvalidOperationException("Операция перемещения папки \"{0}\" в \"{1}\" отменена: невозможно переместить папку в одну из ее же поддиректорий.".FormatWith(currentPathForFolder, pathToFolderThatWillContainTheFolderMoved));
                }

                if (_lockingManager.IsNodeLockedForRenamesAndMoves(folderToMove))
                {
                    throw new FolderLockedException("Не удалось переместить папку (\"{0}\"): она или любая из ее поддиректорий содержит файлы открытые сейчас для чтения или записи.".FormatWith(currentPathForFolder));
                }

                var allSubfoldersOfDestinationFolder = _nodeResolver.GetAllFoldersFrom(foldersNewHome);

                var idsOfAllSubfolders = _nodeResolver.GetIdsOfAllFoldersUnderGivenOne(currentPathForFolder);

                // перемещение в то место, где файл и так уже находится.)
                if (newParentNodeResolvingResult.ResolvedNode.Id.Equals(currentParentFolder.ResolvedNode.Id))
                {
                    return new FolderInfo(allSubfoldersOfDestinationFolder.First(fileNode => _namesComparer.Equals(fileNode.Name, folderName)), currentPathForFolder);
                }

                if (allSubfoldersOfDestinationFolder.Any(fileNode => _namesComparer.Equals(fileNode.Name, folderName)))
                {
                    throw new FolderAlreadyExistsException("Перемещение папки невозможно: в директории \"{0}\" уже существует папка с именем \"{1}\"".FormatWith(pathToFolderThatWillContainTheFolderMoved, folderName));
                }

                // сначала добавляем ссылку в новую папку, потом удаляем ссылку из старой (на добавление может не хватить места, удалить же можно всегда).
                _blockReferenceListsEditor.AddBlockReference(folderToMove.DiskBlockIndex, foldersNewHome.FolderReferencesStreamDefinition, foldersNewHome);
                // TODO: поглядеть на исключения этого метода.
                _blockReferenceListsEditor.TakeOutABlockFromBlockReferenceList(folderToRemoveDirectoryFrom, folderToMove.DiskBlockIndex, folderToRemoveDirectoryFrom.FolderReferencesStreamDefinition);

                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(newParentNodeResolvingResult.FoldersPassedWhileResolving);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(newParentNodeResolvingResult.ResolvedNode.Id);
                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(currentParentFolder.FoldersPassedWhileResolving);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(currentParentFolder.ResolvedNode.Id);
                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(idsOfAllSubfolders);

                allSubfoldersOfDestinationFolder = _nodeResolver.GetAllFoldersFrom(foldersNewHome);

                return new FolderInfo(
                    allSubfoldersOfDestinationFolder.Single(folderNode => _namesComparer.Equals(folderNode.Name, folderName)),
                    _pathBuilder.CombinePaths(pathToFolderThatWillContainTheFolderMoved, folderName));
            }
            catch (InsufficientSpaceException)
            {
                throw new InsufficientSpaceException("Не удалось переместить папку из \"{0}\" в \"{1}\" - не хватает места на диске для проведения операции.".FormatWith(currentPathForFolder, pathToFolderThatWillContainTheFolderMoved));
            }
            catch (MaximumFileSizeReachedException)
            {
                throw new MaximumFolderCountReachedException("Не удалось переместить папку \"{0}\". Папка \"{1}\" не может вместить больше файлов, чем уже вмещает".FormatWith(currentPathForFolder, pathToFolderThatWillContainTheFolderMoved));
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentFolder"></param>
        /// <param name="newFolderName"></param>
        /// <returns></returns>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileSizeReachedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderAlreadyExistsException"></exception>
        private FolderNode CreateFolder(FolderNode parentFolder, string newFolderName)
        {
            MethodArgumentValidator.ThrowIfNull(parentFolder, "parentFolder");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(newFolderName, "newFolderName");

            var subfolders = _nodeResolver.GetAllFoldersFrom(parentFolder);

            var folderWithSameName = subfolders.FirstOrDefault(subfolder => _namesComparer.Equals(subfolder.Name, newFolderName));

            if (folderWithSameName != null)
            {
                throw new FolderAlreadyExistsException("Подпапка с именем \"{0}\" уже существует в папке \"{1}\"".FormatWith(newFolderName, parentFolder.Name));
            }

            var freeBlocks = _freeBlockManager.AcquireFreeBlocks(3);

            int blockToStoreFileReferencesIn = freeBlocks[0];
            int blockToStoreFolderNodeIn = freeBlocks[1];
            int blockToStoreSubfolderReferencesIn = freeBlocks[2];

            var fileReferencesStreamDefinition = new DataStreamDefinition(blockToStoreFileReferencesIn, 0);
            var folderReferencesStreamDefinition = new DataStreamDefinition(blockToStoreSubfolderReferencesIn, 0);

            try
            {
                // Note: может стать последней каплей для переполненного диска, и тогда блоки придется отпустить
                _blockReferenceListsEditor.AddBlockReference(blockToStoreFolderNodeIn, parentFolder.FolderReferencesStreamDefinition, parentFolder);
            }
            catch (Exception)
            {
                _freeBlockManager.MarkBlocksAsFree(freeBlocks);

                throw;
            }

            var creationTime = DateTime.UtcNow;

            var newNode = new FolderNode(newFolderName, Guid.NewGuid(), blockToStoreFolderNodeIn, parentFolder.DiskBlockIndex, fileReferencesStreamDefinition, folderReferencesStreamDefinition, creationTime, Guid.NewGuid());

            _fileSystemNodeStorage.WriteNode(newNode);

            return newNode;
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(folderPath, "folderPath");

            if (_pathBuilder.PointsToRoot(folderPath))
            {
                throw new InvalidPathException("Не получилось удалить папку: нельзя удалить корень файловой системы.");
            }

            try
            {
                string folderName;

                var parentFolderResolvingResult = _nodeResolver.ResolveParentFolderNodeByPath(folderPath, out folderName);

                FolderNode folderToRemoveDirectoryFrom = parentFolderResolvingResult.ResolvedNode;

                var allFolders = _nodeResolver.GetAllFoldersFrom(folderToRemoveDirectoryFrom);

                FolderNode folderToRemove =
                    allFolders.FirstOrDefault(folderNode => _namesComparer.Equals(folderNode.Name, folderName));

                if (folderToRemove == null)
                {
                    throw new FolderNotFoundException("Не найдена папка для удаления (\"0\")".FormatWith(folderPath));
                }

                if ((folderToRemove.FileReferencesStreamDefinition.StreamLengthInBytes > 0) || (folderToRemove.FolderReferencesStreamDefinition.StreamLengthInBytes > 0))
                {
                    throw new FolderNotEmptyException("Не удалось удалить папку: она не пуста"); // Note: блокировки не проверяю. В пустой папке блокировать нечего.
                }

                var idsOfFolderUnderGivenOne = _nodeResolver.GetIdsOfAllFoldersUnderGivenOne(folderPath);

                // TODO: посмотреть на исключения этого метода.
                _blockReferenceListsEditor.TakeOutABlockFromBlockReferenceList(folderToRemoveDirectoryFrom, folderToRemove.DiskBlockIndex, folderToRemoveDirectoryFrom.FolderReferencesStreamDefinition);

                _freeBlockManager.MarkBlocksAsFree(new[]
                                                       {
                                                           folderToRemove.FileReferencesStreamDefinition.ContentsBlockIndex,
                                                           folderToRemove.FolderReferencesStreamDefinition.ContentsBlockIndex,
                                                           folderToRemove.DiskBlockIndex
                                                        });

                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(parentFolderResolvingResult.FoldersPassedWhileResolving);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(parentFolderResolvingResult.ResolvedNode.Id);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(folderToRemove.Id);
                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(idsOfFolderUnderGivenOne);
            }
            catch (CannotResolvePathException)
            {
                throw new FolderNotFoundException("Не удалось найти папку по указанному пути (\"{0}\")".FormatWith(folderPath));
            }
            catch (InvalidPathException)
            {
                throw new FolderNotFoundException("Не удалось найти папку по указанному пути (\"{0}\")".FormatWith(folderPath));
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
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(fullPathForFolder, "fullPathForFolder");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(newFolderName, "newFolderName");

            if (_pathBuilder.PointsToRoot(fullPathForFolder))
            {
                throw new InvalidPathException("Папка не была переименована: имя папки не может указывать на корень файловой системы");
            }

            _nameValidator.Validate(newFolderName);

            try
            {
                string folderName;

                var parentFolderResolvingResult = _nodeResolver.ResolveParentFolderNodeByPath(fullPathForFolder,
                                                                                              out folderName);

                string lastPartOfFullPath = _pathBuilder.GetFileOrFolderName(newFolderName);

                if (_namesComparer.Equals(folderName, lastPartOfFullPath))
                {
                    return;
                }

                FolderNode parentFolderNode = parentFolderResolvingResult.ResolvedNode;

                ReadOnlyCollection<FolderNode> folders = _nodeResolver.GetAllFoldersFrom(parentFolderNode);

                FolderNode folderNode = folders.FirstOrDefault(node => _namesComparer.Equals(node.Name, folderName));

                if (folderNode == null)
                {
                    throw new FolderNotFoundException("Не найдена папка для переименования (\"{0}\")".FormatWith(fullPathForFolder));
                }

                if (_lockingManager.IsNodeLockedForRenamesAndMoves(folderNode))
                {
                    throw new FolderLockedException("Не удалось переименовать папку. В ней (или в одной из ее поддиректорий) есть файлы, которые сейчас открыты.");
                }

                if (folders.Any(node => _namesComparer.Equals(newFolderName, node.Name)))
                {
                    throw new FolderAlreadyExistsException("Не удалось переименовать папку: папка с именем \"{0}\" уже существует в директории, где вы производите переименование".FormatWith(newFolderName));
                }

                var idsOfFolderUnderGivenOne = _nodeResolver.GetIdsOfAllFoldersUnderGivenOne(fullPathForFolder);

                folderNode.Name = newFolderName;

                _fileSystemNodeStorage.WriteNode(folderNode);

                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(parentFolderResolvingResult.FoldersPassedWhileResolving);
                _folderEnumeratorRegistry.InvalidateEnumeratorsForFolder(parentFolderResolvingResult.ResolvedNode.Id);
                _folderEnumeratorRegistry.InvalidateEnumeratorsFor(idsOfFolderUnderGivenOne);
            }
            catch (CannotResolvePathException)
            {
                throw new FileNotFoundException("Не удалось найти папку по указанному пути (\"{0}\")".FormatWith(fullPathForFolder));
            }
            catch (InvalidPathException)
            {
                throw new FileNotFoundException("Не удалось найти папку по указанному пути (\"{0}\")".FormatWith(fullPathForFolder));
            }
        }
    }
}