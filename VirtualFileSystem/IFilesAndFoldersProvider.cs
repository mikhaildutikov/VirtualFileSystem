using System;
using System.Collections.ObjectModel;
using VirtualFileSystem.Exceptions.Irrecoverable;

namespace VirtualFileSystem
{
    internal interface IFilesAndFoldersProvider
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        ReadOnlyCollection<FileInfo> GetAllFilesFrom(string path);

        /// <summary>
        /// Возвращает сведения о всех папках в указанной директории (<paramref name="folderToGetSubfoldersOf"/>).
        /// </summary>
        /// <param name="folderToGetSubfoldersOf">Папка, поддиректории которой надо вернуть.</param>
        /// <returns>Сведения о всех папках в указанной директории (<paramref name="folderToGetSubfoldersOf"/>)</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        ReadOnlyCollection<FolderInfo> GetAllFoldersFrom(string folderToGetSubfoldersOf);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidPathException"></exception>
        /// <exception cref="CannotResolvePathException"></exception>
        FolderInfo GetParentOf(string folderPath);
    }
}