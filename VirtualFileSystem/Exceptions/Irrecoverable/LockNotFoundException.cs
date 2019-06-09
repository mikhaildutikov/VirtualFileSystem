using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Exceptions.Irrecoverable
{
    [Serializable]
    public class LockNotFoundException : Exception
    {
        public LockNotFoundException()
        {
        }

        public LockNotFoundException(string message) : base(message)
        {
        }

        public LockNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        protected LockNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}