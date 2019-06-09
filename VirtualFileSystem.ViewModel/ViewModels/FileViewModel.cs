using System;
using VirtualFileSystem.ViewModel.Visitors;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal class FileViewModel : FileSystemArtifactViewModel
    {
        internal static FileViewModel FromFileInfo(FileInfo fileInfo)
        {
            if (fileInfo == null) throw new ArgumentNullException("fileInfo");

            return new FileViewModel(fileInfo.FullPath, fileInfo.Name, fileInfo.Id, fileInfo.CreationTimeUtc.ToLocalTime(), fileInfo.LastModificationTimeUtc.ToLocalTime(), fileInfo.SizeInBytes);
        }

        internal FileViewModel(string fullPath, string name, Guid id, DateTime creationTime, DateTime lastModificationTime, int sizeInBytes)
            : base(fullPath, name, id, creationTime)
        {
            LastModificationTime = lastModificationTime;
            SizeInBytes = sizeInBytes;
        }

        /// <summary>
        /// Время последнего изменения файла.
        /// </summary>
        public DateTime LastModificationTime { get; private set; }

        /// <summary>
        /// Размер файла, в байтах.
        /// </summary>
        public int SizeInBytes { get; private set; }

        public override void Accept(IFileSystemArtifactViewModelVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            visitor.VisitFile(this);
        }
    }
}