using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class FolderNotFoundException : Exception
    {
        public FolderNotFoundException()
        {
        }

        public FolderNotFoundException(string message) : base(message)
        {
        }

        public FolderNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FolderNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}