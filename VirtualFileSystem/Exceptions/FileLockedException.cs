using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class FileLockedException : Exception
    {
        public FileLockedException()
        {
        }

        public FileLockedException(string message) : base(message)
        {
        }

        public FileLockedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected FileLockedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}