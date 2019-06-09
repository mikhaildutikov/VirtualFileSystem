using System;
using System.ComponentModel;

namespace VirtualFileSystem.ViewModel
{
    internal interface IVirtualFileSystemInstanceManager : INotifyPropertyChanged
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <param name="fileSystemId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        VirtualFileSystem CreateFromFile(string fullFilePath, out string fileSystemId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <returns></returns>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        VirtualFileSystem CreateNewFormattingTheFile(string fullFilePath);

        void ReportThatSystemIsNoLongerNeeded(VirtualFileSystem virtualFileSystem);

        bool IsEmpty { get; }
    }
}