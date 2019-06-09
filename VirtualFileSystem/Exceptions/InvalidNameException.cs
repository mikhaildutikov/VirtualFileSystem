using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class InvalidNameException : Exception
    {
        public InvalidNameException()
        {
        }

        public InvalidNameException(string message) : base(message)
        {
        }

        public InvalidNameException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidNameException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}