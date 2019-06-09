using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using VirtualFileSystem.Disk;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.DiskStructuresManagement
{
    /// <summary>
    /// Note: каждая из примитивных дисковых структур обязана помещаться в одном дисковом блоке (ограничение текущей версии).
    /// </summary>
    internal class FileSystemNodeStorage : IFileSystemNodeStorage
    {
        private readonly IVirtualDisk _virtualDisk;
        private readonly BinaryFormatter _binaryFormatter;
        private readonly object _stateProtectingCriticalSection = new object(); // TODO: revisit

        public FileSystemNodeStorage(IVirtualDisk virtualDisk)
        {
            if (virtualDisk == null) throw new ArgumentNullException("virtualDisk");

            _virtualDisk = virtualDisk;
            _binaryFormatter = new BinaryFormatter();
            _binaryFormatter.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="destinationDiskBlock"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        public void WriteFileSystemHeader(FileSystemHeader header, int destinationDiskBlock)
        {
            if (header == null) throw new ArgumentNullException("header");

            this.WriteObject(header, destinationDiskBlock);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToReadFrom"></param>
        /// <returns></returns>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public FileSystemHeader ReadFileSystemHeader(int indexOfBlockToReadFrom)
        {
            return DeserializeObjectAtBlock<FileSystemHeader>(indexOfBlockToReadFrom);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="numberOfBlockToRead"></param>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private TResult DeserializeObjectAtBlock<TResult>(int numberOfBlockToRead)
        {
            try
            {
                byte[] blockBytes = _virtualDisk.ReadAllBytesFromBlock(numberOfBlockToRead);
                int serializedStreamLength = GetSerializedStreamSize(blockBytes);

                return (TResult)ReadObject(blockBytes, 4, serializedStreamLength);
            }
            catch (InvalidCastException exception)
            {
                throw ConstructGenericCannotReadNodeException(exception);
            }
            catch (SerializationException exception)
            {
                throw ConstructGenericCannotReadNodeException(exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeToWrite"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        public void WriteNode(Node nodeToWrite)
        {
            if (nodeToWrite == null) throw new ArgumentNullException("nodeToWrite");

            FileNode nodeAsFileNode = nodeToWrite as FileNode;

            if (nodeAsFileNode != null)
            {
                nodeAsFileNode.LastModificationTimeUtc = DateTime.UtcNow;
            }

            this.WriteObject(nodeToWrite, nodeToWrite.DiskBlockIndex);
        }

        private static Exception ConstructGenericCannotWriteNodeException(Exception exceptionToWrap)
        {
            return new InconsistentDataDetectedException("Не удалось записать метаданные дисковой структуры. Обратитесь к разработчикам программы.", exceptionToWrap);
        }

        private static Exception ConstructGenericCannotReadNodeException(Exception exceptionToWrap)
        {
            return new InconsistentDataDetectedException("Не удалось прочесть метаданные дисковой структуры. Обратитесь к разработчикам программы.", exceptionToWrap);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectToWrite"></param>
        /// <param name="destinationDiskBlock"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        private void WriteObject(object objectToWrite, int destinationDiskBlock)
        {
            try
            {
                MemoryStream memoryStream = SerializeToMemoryStream(objectToWrite);

                var bytesToWrite = new byte[memoryStream.Length + 4];

                var finalStream = new MemoryStream(bytesToWrite, true);
                var writer = new BinaryWriter(finalStream);

                writer.Write((Int32)memoryStream.Length);
                writer.Write(memoryStream.ToArray());

                _virtualDisk.WriteBytesToBlock(destinationDiskBlock, finalStream.ToArray());
            }
            catch (SerializationException exception)
            {
                throw ConstructGenericCannotWriteNodeException(exception);
            }
            catch (ArgumentException exception)
            {
                throw ConstructGenericCannotWriteNodeException(exception);
            }
            catch (InvalidOperationException exception)
            {
                throw ConstructGenericCannotWriteNodeException(exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectToWrite"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SerializationException"></exception>
        private MemoryStream SerializeToMemoryStream(object objectToWrite)
        {
            var memoryStream = new MemoryStream();

            _binaryFormatter.Serialize(memoryStream, objectToWrite);

            if ((memoryStream.Length + Constants.BytesToStoreChunkOfDataLength) > _virtualDisk.BlockSizeInBytes)
            {
                throw new InvalidOperationException("Не удалось сохранить описание файла/папки. Согласно ограничению текущей версии, размер не должен превышать {0} байт.".FormatWith(_virtualDisk.BlockSizeInBytes - Constants.BytesToStoreChunkOfDataLength));
            }

            return memoryStream;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToReadFrom"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        public FileNode ReadFileNode(int indexOfBlockToReadFrom)
        {
            return DeserializeObjectAtBlock<FileNode>(indexOfBlockToReadFrom);
        }

        private static int GetSerializedStreamSize(byte[] blockBytes)
        {
            var stream = new MemoryStream(blockBytes);
            var reader = new BinaryReader(stream);

            return reader.ReadInt32();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToReadFrom"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        public FolderNode ReadFolderNode(int indexOfBlockToReadFrom)
        {
            return DeserializeObjectAtBlock<FolderNode>(indexOfBlockToReadFrom);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="SerializationException"></exception>
        private object ReadObject(byte[] bytes, int offset, int count)
        {
            var bytesRead = new byte[count];

            Array.Copy(bytes, offset, bytesRead, 0, count);

            var memoryStream = new MemoryStream(bytesRead);

            return _binaryFormatter.Deserialize(memoryStream);
        }
    }
}