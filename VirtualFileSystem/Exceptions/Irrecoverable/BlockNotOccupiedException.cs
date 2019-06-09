using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Exceptions.Irrecoverable
{
    [Serializable]
    public class BlockNotOccupiedException : Exception
    {
        public BlockNotOccupiedException()
        {
        }

        public BlockNotOccupiedException(string message) : base(message)
        {
        }

        public BlockNotOccupiedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BlockNotOccupiedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}