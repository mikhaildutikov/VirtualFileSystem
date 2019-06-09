using System;
using System.IO;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Streaming;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem
{
    internal class BlockReferenceListsEditor
    {
        private readonly IVirtualDisk _virtualDisk;
        private readonly IFreeBlockManager _freeBlockManager;
        private readonly FileSystemNodeStorage _fileSystemNodeStorage;

        public BlockReferenceListsEditor(IVirtualDisk virtualDisk, IFreeBlockManager freeBlockManager, FileSystemNodeStorage fileSystemNodeStorage)
        {
            if (virtualDisk == null) throw new ArgumentNullException("virtualDisk");
            if (freeBlockManager == null) throw new ArgumentNullException("freeBlockManager");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");

            _virtualDisk = virtualDisk;
            _fileSystemNodeStorage = fileSystemNodeStorage;
            _freeBlockManager = freeBlockManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="dataStreamToAddReferenceIn"></param>
        /// <param name="nodeOwningTheStream"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileSizeReachedException"></exception>
        public void AddBlockReference(int blockIndex, DataStreamDefinition dataStreamToAddReferenceIn, Node nodeOwningTheStream)
        {
            if (dataStreamToAddReferenceIn == null) throw new ArgumentNullException("dataStreamToAddReferenceIn");
            if (nodeOwningTheStream == null) throw new ArgumentNullException("nodeOwningTheStream");
            MethodArgumentValidator.ThrowIfNegative(blockIndex, "blockIndex");

            var streamStructureBuilder =
                new DataStreamStructureBuilder(dataStreamToAddReferenceIn, _virtualDisk, _freeBlockManager, _fileSystemNodeStorage, nodeOwningTheStream, AddressingSystemParameters.Default);

            // Note: никаких настоящих блокировок не использует.
            var parentFolderFileReferencesStream = new DataStream(streamStructureBuilder,
                                                                  AddressingSystemParameters.Default.BlockSize,
                                                                  new NullFileSystemObjectLockingManager(),
                                                                  Guid.NewGuid());

            var nodeUpdatingStream = new DataStreamNodeUpdating(parentFolderFileReferencesStream, nodeOwningTheStream, _fileSystemNodeStorage);

            using (nodeUpdatingStream)
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);
                writer.Write(blockIndex);

                nodeUpdatingStream.MoveToEnd();                

                nodeUpdatingStream.Write(stream.ToArray(), 0, (int)stream.Length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderToRemoveDirectoryFrom"></param>
        /// <param name="indexReferenceToRemove"></param>
        /// <param name="dataStreamBeingCorrected"></param>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        public void TakeOutABlockFromBlockReferenceList(FolderNode folderToRemoveDirectoryFrom, int indexReferenceToRemove, DataStreamDefinition dataStreamBeingCorrected)
        {
            var streamStructureBuilder =
                new DataStreamStructureBuilder(dataStreamBeingCorrected, _virtualDisk, _freeBlockManager, _fileSystemNodeStorage, folderToRemoveDirectoryFrom, AddressingSystemParameters.Default);

            var lockingManager = new NullFileSystemObjectLockingManager();

            var folderReferencesStream = new DataStream(streamStructureBuilder, AddressingSystemParameters.Default.BlockSize, lockingManager, Guid.NewGuid());

            var nodeUpdatingStream = new DataStreamNodeUpdating(folderReferencesStream, folderToRemoveDirectoryFrom, _fileSystemNodeStorage);

            var referencesToBlocks = new byte[nodeUpdatingStream.Length];

            nodeUpdatingStream.Read(referencesToBlocks, 0, nodeUpdatingStream.Length);

            var correctedStream = new MemoryStream(nodeUpdatingStream.Length);

            var reader = new BinaryReader(new MemoryStream(referencesToBlocks, true));
            var writer = new BinaryWriter(correctedStream);

            int numberOfReferencesFound = 0;

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                int nodeIndex = reader.ReadInt32();

                if (nodeIndex != indexReferenceToRemove)
                {
                    writer.Write(nodeIndex);
                }
                else
                {
                    numberOfReferencesFound++;
                }
            }

            if (numberOfReferencesFound != 1)
            {
                throw new InconsistentDataDetectedException("Обнаружены неконсистентные данные (произошла попытка удаления несуществующей ссылки на файл или папку). Возможна потеря данных. Обратитесь к разработчикам.");
            }

            nodeUpdatingStream.SetLength(nodeUpdatingStream.Length - Constants.BlockReferenceSizeInBytes);

            var bytes = correctedStream.ToArray();

            nodeUpdatingStream.SetPosition(0);
            nodeUpdatingStream.Write(bytes, 0, bytes.Length);
        }
    }
}