using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Exceptions.Irrecoverable
{
    [Serializable]
    public class LockAlreadyHeldException : Exception
    {
        public LockAlreadyHeldException()
        {
        }

        public LockAlreadyHeldException(string message) : base(message)
        {
        }

        public LockAlreadyHeldException(string message, Exception inner) : base(message, inner)
        {
        }

        protected LockAlreadyHeldException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}