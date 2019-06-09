using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class FolderLockedException : Exception
    {
        public FolderLockedException()
        {
        }

        public FolderLockedException(string message) : base(message)
        {
        }

        public FolderLockedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FolderLockedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}