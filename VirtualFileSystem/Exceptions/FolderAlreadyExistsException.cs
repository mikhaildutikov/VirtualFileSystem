using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class FolderAlreadyExistsException : Exception
    {
        public FolderAlreadyExistsException()
        {
        }

        public FolderAlreadyExistsException(string message) : base(message)
        {
        }

        public FolderAlreadyExistsException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FolderAlreadyExistsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}