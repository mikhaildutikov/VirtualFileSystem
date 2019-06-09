using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using VirtualFileSystem.Disk;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Streaming;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.DiskStructuresManagement
{
    /// <summary>
    /// TODO: я слишком большой, займись мной.
    /// </summary>
    internal class NodeResolver
    {
        private readonly IVirtualDisk _virtualDisk;
        private readonly FileSystemNodeStorage _fileSystemNodeStorage;
        private readonly IEqualityComparer<string> _namesComparer;
        private readonly int _rootBlockIndex;
        private readonly string _rootFolderPath;
        private readonly char _directorySeparatorChar;
        private readonly IPathValidator _pathValidator;
        private readonly PathBuilder _pathBuilder;

        public NodeResolver(
            IVirtualDisk virtualDisk,
            FileSystemNodeStorage fileSystemNodeStorage,
            IEqualityComparer<string> namesComparer,
            int rootBlockIndex,
            string rootFolderPath,
            char directorySeparatorChar,
            IPathValidator pathValidator,
            PathBuilder pathBuilder)
        {
            if (virtualDisk == null) throw new ArgumentNullException("virtualDisk");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");
            if (namesComparer == null) throw new ArgumentNullException("namesComparer");
            if (pathBuilder == null) throw new ArgumentNullException("pathBuilder");
            if (String.IsNullOrEmpty(rootFolderPath)) throw new ArgumentNullException("rootFolderPath");
            MethodArgumentValidator.ThrowIfNegative(rootBlockIndex, "rootBlockIndex");

            if (rootBlockIndex >= virtualDisk.NumberOfBlocks)
            {
                throw new ArgumentOutOfRangeException("rootBlockIndex");
            }

            _virtualDisk = virtualDisk;
            _pathBuilder = pathBuilder;
            _fileSystemNodeStorage = fileSystemNodeStorage;
            _namesComparer = namesComparer;
            _rootBlockIndex = rootBlockIndex;
            _rootFolderPath = rootFolderPath;
            _directorySeparatorChar = directorySeparatorChar;
            _pathValidator = pathValidator;
        }

        public ReadOnlyCollection<FolderNode> GetAllFoldersFrom(FolderNode folderToGetContentsOf)
        {
            if (folderToGetContentsOf == null)
            {
                throw new ArgumentNullException("folderToGetContentsOf");
            }

            if (folderToGetContentsOf.FolderReferencesStreamDefinition.StreamLengthInBytes == 0)
            {
                return new List<FolderNode>().AsReadOnly();
            }

            var blocksContainingFileNodes = this.ReadAllBlockReferences(folderToGetContentsOf.FolderReferencesStreamDefinition,
                                                                        folderToGetContentsOf);

            var folderNodes = new List<FolderNode>();

            foreach (int fileNodeIndex in blocksContainingFileNodes)
            {
                folderNodes.Add(_fileSystemNodeStorage.ReadFolderNode(fileNodeIndex));
            }

            return folderNodes.AsReadOnly();
        }

        /// <summary>
        /// Возвращает сведения о всех подпапках в указанной директории (<paramref name="folderToGetSubfoldersOf"/>).
        /// </summary>
        /// <param name="folderToGetSubfoldersOf">Папка, поддиректории которой надо вернуть.</param>
        /// <returns>Сведения о всех папках в указанной директории (<paramref name="folderToGetSubfoldersOf"/>)</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        public ReadOnlyCollection<FolderInfo> GetAllFoldersFrom(string folderToGetSubfoldersOf)
        {
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(folderToGetSubfoldersOf, "folderToGetSubfoldersOf");

            try
            {
                NodeWithSurroundingsResolvingResult<FolderNode> nodeResolvingResult = this.ResolveFolderNodeByPath(folderToGetSubfoldersOf);

                FolderNode parentFolder = nodeResolvingResult.ResolvedNode;

                return this.GetAllFoldersFrom(parentFolder)
                    .Select(folderNode => new FolderInfo(folderNode, _pathBuilder.CombinePaths(folderToGetSubfoldersOf, folderNode.Name)))
                    .ToList()
                    .AsReadOnly();
            }
            catch (InvalidPathException)
            {
                throw new FolderNotFoundException("Не удалось найти папку по указанному пути (\"{0}\")".FormatWith(folderToGetSubfoldersOf));
            }
        }

        internal bool FolderExists(string path)
        {
            try
            {
                this.ResolveFolderNodeByPath(path);
                return true;
            }
            catch (InvalidPathException)
            {
                return false;
            }
            catch (FolderNotFoundException)
            {
                return false;
            }
        }

        /// <exception cref="FileNotFoundException"></exception>
        internal FileNode ResolveFileNode(
            string filePath,
            NodeResolvingResult<FolderNode> parentFolderResolvingResult,
            string fileName,
            string verbForPuttingIntoExceptionMessageTemplate,
            out ReadOnlyCollection<FileNode> filesFromSameFolder)
        {
            FolderNode folderToRemoveFileFrom = parentFolderResolvingResult.ResolvedNode;

            filesFromSameFolder = this.GetAllFilesFrom(folderToRemoveFileFrom);

            var fileNode = filesFromSameFolder.FirstOrDefault(file => _namesComparer.Equals(file.Name, fileName));

            if (fileNode == null)
            {
                throw new FileNotFoundException("Не получилось {0} файл (\"{1}\"): файл не найден".FormatWith(verbForPuttingIntoExceptionMessageTemplate, filePath));
            }

            return fileNode;
        }

        private IEnumerable<int> ReadAllBlockReferences(DataStreamDefinition streamDefinition, Node owningNode)
        {
            IDataStreamStructureBuilder streamStructureBuilder =
                new DataStreamStructureBuilderImmutable(_virtualDisk, streamDefinition, AddressingSystemParameters.Default, _fileSystemNodeStorage, owningNode);

            // Note: никаких настоящих блокировок не использует.
            var fileReferencesStream = new DataStreamToReadableAdapter(new DataStream(streamStructureBuilder, AddressingSystemParameters.Default.BlockSize, new NullFileSystemObjectLockingManager(), Guid.NewGuid()));

            var referencesToBlocks = new byte[fileReferencesStream.Length];

            fileReferencesStream.Read(referencesToBlocks, 0, fileReferencesStream.Length);

            var reader = new BinaryReader(new MemoryStream(referencesToBlocks));

            var blockReferences = new List<int>();

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                int nodeIndex = reader.ReadInt32();

                blockReferences.Add(nodeIndex);
            }

            return blockReferences.AsReadOnly();
        }

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        public NodeWithSurroundingsResolvingResult<FileNode> ResolveFileNodeByPath(string path)
        {
            _pathValidator.Validate(path);

            try
            {
                string lastName;

                var resolvingResult = this.ResolveParentFolderNodeByPath(path, out lastName);

                FolderNode parentFolder = resolvingResult.ResolvedNode;

                // Note: можно выбрать один вместо всех.
                ReadOnlyCollection<FileNode> allFilesUnderFolder = this.GetAllFilesFrom(parentFolder);

                FileNode theFile = allFilesUnderFolder.FirstOrDefault(file => _namesComparer.Equals(lastName, file.Name));

                if (theFile == null)
                {
                    throw new FileNotFoundException("Файл (\"{0}\") не найден".FormatWith(path));
                }

                return new NodeWithSurroundingsResolvingResult<FileNode>(resolvingResult.FoldersPassedWhileResolving, theFile, parentFolder);
            }
            catch (CannotResolvePathException)
            {
                throw new FileNotFoundException("Файл (\"{0}\") не найден".FormatWith(path));
            }
        }

        /// <summary>
        /// Note: без рекурсии.
        /// </summary>
        /// <param name="folderNode"></param>
        /// <returns></returns>
        public ReadOnlyCollection<FileNode> GetAllFilesFrom(FolderNode folderNode)
        {
            if (folderNode == null) throw new ArgumentNullException("folderNode");

            if (folderNode.FileReferencesStreamDefinition.StreamLengthInBytes == 0)
            {
                return new List<FileNode>().AsReadOnly();
            }

            var blocksContainingFileNodes = this.ReadAllBlockReferences(folderNode.FileReferencesStreamDefinition,
                                                                        folderNode);

            var fileNodes = new List<FileNode>();

            foreach (int fileNodeIndex in blocksContainingFileNodes)
            {
                fileNodes.Add(_fileSystemNodeStorage.ReadFileNode(fileNodeIndex));
            }

            return fileNodes.AsReadOnly();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderToSearchIn"></param>
        /// <param name="allFolders"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public IEnumerable<FileInfo> GetAllFilesFromFolderRecursivelyAsPlainLists(string folderToSearchIn, out List<FolderInfo> allFolders)
        {
            var allFiles = new List<FileInfo>();
            allFolders = new List<FolderInfo>();

            var files = this.GetAllFilesFrom(folderToSearchIn);

            allFiles.AddRange(files);

            var folders = this.GetAllFoldersFrom(folderToSearchIn);

            allFolders.AddRange(folders);

            foreach (FolderInfo folder in folders)
            {
                List<FolderInfo> newFolders;
                var filesFromFolder = GetAllFilesFromFolderRecursivelyAsPlainLists(folder.FullPath, out newFolders);

                allFiles.AddRange(filesFromFolder);
                allFolders.AddRange(newFolders);
            }

            return allFiles.AsReadOnly();
        }

        internal ReadOnlyCollection<Guid> GetIdsOfAllFoldersUnderGivenOne(string path)
        {
            List<FolderInfo> folders;

            this.GetAllFilesFromFolderRecursivelyAsPlainLists(path, out folders);

            return folders.Select(folder => folder.Id).ToList().AsReadOnly();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public ReadOnlyCollection<FileInfo> GetAllFilesFrom(string path)
        {
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(path, "path");

            try
            {
                var nodeResolvingResult = this.ResolveFolderNodeByPath(path);

                FolderNode parentFolder = nodeResolvingResult.ResolvedNode;

                return this.GetAllFilesFrom(parentFolder).Select(file => new FileInfo(file, _pathBuilder.CombinePaths(path, file.Name))).ToList().AsReadOnly();
            }
            catch (InvalidPathException exception)
            {
                throw new FolderNotFoundException("Не удалось получить файлы, содержащиеся в папке \"{0}\". {1}".FormatWith(path, exception.Message));
            }
        }

        public FolderNode GetRoot()
        {
            return _fileSystemNodeStorage.ReadFolderNode(_rootBlockIndex);
        }

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        public NodeWithSurroundingsResolvingResult<FolderNode> ResolveFolderNodeByPath(string path)
        {
            _pathValidator.Validate(path);

            if (_rootFolderPath.Equals(path, StringComparison.Ordinal))
            {
                var rootNode = this.GetRoot();

                return new NodeWithSurroundingsResolvingResult<FolderNode>(new List<FolderNode>{rootNode}, rootNode, null);
            }

            try
            {
                string lastName;

                var nodeResolvingResult = this.ResolveParentFolderNodeByPath(path, out lastName);

                FolderNode parentFolder = nodeResolvingResult.ResolvedNode;

                // Note: достаточно выбрать один - все не нужны (но оптимизацию я не делаю).
                ReadOnlyCollection<FolderNode> allFilesUnderFolder = this.GetAllFoldersFrom(parentFolder);

                FolderNode theFolder = allFilesUnderFolder.FirstOrDefault(folder => _namesComparer.Equals(lastName, folder.Name));

                if (theFolder == null)
                {
                    throw new FolderNotFoundException("Папка (\"{0}\") не найдена".FormatWith(path));
                }

                return new NodeWithSurroundingsResolvingResult<FolderNode>(nodeResolvingResult.FoldersPassedWhileResolving, theFolder, parentFolder);
            }
            catch (CannotResolvePathException)
            {
                throw new FolderNotFoundException("Папка (\"{0}\") не найдена".FormatWith(path));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lastName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="CannotResolvePathException"></exception>
        internal NodeResolvingResult<FolderNode> ResolveParentFolderNodeByPath(string path, out string lastName)
        {
            _pathValidator.Validate(path);

            if (_namesComparer.Equals(path, _rootFolderPath))
            {
                throw new InvalidPathException("Нет такой папки, в которой содержится корневая");
            }

            var nodesPassed = new List<FolderNode>();

            string[] pathParts = path.Split(new char[] { _directorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            FolderNode lastNodeResolved = this.GetRoot();
            nodesPassed.Add(lastNodeResolved);

            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                string folderNameToResolve = pathParts[i];

                ReadOnlyCollection<FolderNode> subFolders = this.GetAllFoldersFrom(lastNodeResolved);

                lastNodeResolved =
                    subFolders.FirstOrDefault(subFolder => _namesComparer.Equals(folderNameToResolve, subFolder.Name));

                if (lastNodeResolved == null)
                {
                    throw new CannotResolvePathException("Не удалось найти папку, на которую бы указывал путь \"{0}\"".FormatWith(path));
                }

                nodesPassed.Add(lastNodeResolved);
            }

            lastName = pathParts[pathParts.Length - 1];

            return new NodeResolvingResult<FolderNode>(nodesPassed, lastNodeResolved);
        }
    }
}