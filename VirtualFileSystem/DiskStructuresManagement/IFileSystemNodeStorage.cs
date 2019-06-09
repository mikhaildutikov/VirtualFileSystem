using System;
using VirtualFileSystem.Exceptions.Irrecoverable;

namespace VirtualFileSystem.DiskStructuresManagement
{
    internal interface IFileSystemNodeStorage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="destinationDiskBlock"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        void WriteFileSystemHeader(FileSystemHeader header, int destinationDiskBlock);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToReadFrom"></param>
        /// <returns></returns>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        FileSystemHeader ReadFileSystemHeader(int indexOfBlockToReadFrom);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeToWrite"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        void WriteNode(Node nodeToWrite);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToReadFrom"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        FileNode ReadFileNode(int indexOfBlockToReadFrom);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToReadFrom"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        FolderNode ReadFolderNode(int indexOfBlockToReadFrom);
    }
}