using System;

namespace VirtualFileSystem
{
    public class FileTaskResult : FileSystemTaskResult
    {
        internal FileTaskResult(FileAddressable sourceFilePath, FileAddressable destinationFilePath, string error) :base(error)
        {
            if (sourceFilePath == null) throw new ArgumentNullException("sourceFilePath");
            
            if (!String.IsNullOrEmpty(error) && (destinationFilePath != null))
            {
                throw new ArgumentException("Если установлено сообщение об ошибке, значит указателя на файл быть не может.", "destinationFilePath");
            }

            SourceFile = sourceFilePath;
            DestinationFile = destinationFilePath;
        }

        public FileAddressable SourceFile { get; private set; }
        public FileAddressable DestinationFile { get; private set; }
    }
}