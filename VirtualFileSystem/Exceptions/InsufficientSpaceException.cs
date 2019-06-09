using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class InsufficientSpaceException : Exception
    {
        public InsufficientSpaceException()
        {
        }

        public InsufficientSpaceException(string message) : base(message)
        {
        }

        public InsufficientSpaceException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InsufficientSpaceException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}