using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace VirtualFileSystem.Tests.Helpers
{
    internal static class VirtualFileSystemTestingExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        internal static ReadOnlyCollection<FileInfo> GetAllFilesFrom(this VirtualFileSystem fileSystem, string path)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");

            return ((IFilesAndFoldersProvider)fileSystem).GetAllFilesFrom(path);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        internal static ReadOnlyCollection<string> GetNamesOfAllFilesFrom(this VirtualFileSystem fileSystem, string path)
        {
            return fileSystem.GetAllFilesFrom(path).Select(fileNode => fileNode.Name).ToList().AsReadOnly();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal static ReadOnlyCollection<string> GetNamesOfAllFoldersFrom(this VirtualFileSystem fileSystem, string path)
        {
            return fileSystem.GetAllFoldersFrom(path).Select(folderNode => folderNode.Name).ToList().AsReadOnly();
        }
    }
}