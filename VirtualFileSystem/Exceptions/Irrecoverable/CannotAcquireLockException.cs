using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Exceptions.Irrecoverable
{
    [Serializable]
    public class CannotAcquireLockException : Exception
    {
        public CannotAcquireLockException()
        {
        }

        public CannotAcquireLockException(string message) : base(message)
        {
        }

        public CannotAcquireLockException(string message, Exception inner) : base(message, inner)
        {
        }

        protected CannotAcquireLockException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}