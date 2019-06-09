using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Exceptions.Irrecoverable
{
    [Serializable]
    public class NoFreeBlocksException : Exception
    {
        public NoFreeBlocksException()
        {
        }

        public NoFreeBlocksException(string message) : base(message)
        {
        }

        public NoFreeBlocksException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NoFreeBlocksException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}