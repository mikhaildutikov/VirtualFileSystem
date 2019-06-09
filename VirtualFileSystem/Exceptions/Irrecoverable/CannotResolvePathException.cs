using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Exceptions.Irrecoverable
{
    [Serializable]
    public class CannotResolvePathException : Exception
    {
        public CannotResolvePathException()
        {
        }

        public CannotResolvePathException(string message) : base(message)
        {
        }

        public CannotResolvePathException(string message, Exception inner) : base(message, inner)
        {
        }

        protected CannotResolvePathException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}