using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class CannotGetFileContentsException : Exception
    {
        public CannotGetFileContentsException()
        {
        }

        public CannotGetFileContentsException(string message) : base(message)
        {
        }

        public CannotGetFileContentsException(string message, Exception inner) : base(message, inner)
        {
        }

        protected CannotGetFileContentsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}