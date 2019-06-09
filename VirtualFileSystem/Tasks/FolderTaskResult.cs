using System;

namespace VirtualFileSystem
{
    /// <summary>
    /// Результат исполнения задачи, связанной с папками в файловой системе.
    /// </summary>
    public class FolderTaskResult : FileSystemTaskResult
    {
        internal FolderTaskResult(FolderAddressable sourceFolderPath, FolderAddressable destinationFolderPath, string error)
            : base(error)
        {
            if (sourceFolderPath == null) throw new ArgumentNullException("sourceFolderPath");
            
            if (!String.IsNullOrEmpty(error) && (destinationFolderPath != null))
            {
                throw new ArgumentException("Если установлено сообщение об ошибке, значит указателя на папку быть не может.", "destinationFolderPath");
            }

            SourceFolder = sourceFolderPath;
            DestinationFolder = destinationFolderPath;
        }

        /// <summary>
        /// Исходная папка.
        /// </summary>
        public FolderAddressable SourceFolder { get; private set; }

        /// <summary>
        /// Папка, указывающая результат операции.
        /// </summary>
        public FolderAddressable DestinationFolder { get; private set; }
    }
}