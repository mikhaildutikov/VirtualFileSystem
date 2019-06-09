using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class FolderNotEmptyException : Exception
    {
        public FolderNotEmptyException()
        {
        }

        public FolderNotEmptyException(string message) : base(message)
        {
        }

        public FolderNotEmptyException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FolderNotEmptyException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}