using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class InvalidPathException : Exception
    {
        public InvalidPathException()
        {
        }

        public InvalidPathException(string message) : base(message)
        {
        }

        public InvalidPathException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidPathException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}