using System;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem
{
    /// <summary>
    /// Содержит сведения о файле виртуальной файловой системы.
    /// Примечание: это неизменяемые, не получающие обновлений, сведения, когда-то полученные от файловой системы. Наличие экземпляра этого класса не гарантирует даже, что
    /// файл существует, с момента получения вами FileInfo файл мог быть удален.
    /// </summary>
    public class FileInfo : FileSystemArtifactInfo
    {
        internal FileInfo(FileNode fileNode, string fullPath) : base(fullPath, fileNode.Name, fileNode.Id, fileNode.Version, fileNode.CreationTimeUtc)
        {
            this.LastModificationTimeUtc = fileNode.LastModificationTimeUtc;
            this.SizeInBytes = fileNode.FileContentsStreamDefinition.StreamLengthInBytes;
        }

        /// <summary>
        /// Конвертирует текущий экземпляр <see cref="FileInfo"/> в <see cref="FileAddressable"/>.
        /// </summary>
        /// <returns></returns>
        public FileAddressable ToFileAddressable()
        {
            return new FileAddressable(base.FullPath, base.Name);
        }

        /// <summary>
        /// Время последнего изменения файла (UTC).
        /// </summary>
        public DateTime LastModificationTimeUtc { get; private set; }

        /// <summary>
        /// Размер файла, в байтах.
        /// </summary>
        public int SizeInBytes { get; private set; }
    }
}