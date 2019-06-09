using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class FileSystemCreationFailedException : Exception
    {
        public FileSystemCreationFailedException()
        {
        }

        public FileSystemCreationFailedException(string message) : base(message)
        {
        }

        public FileSystemCreationFailedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FileSystemCreationFailedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}