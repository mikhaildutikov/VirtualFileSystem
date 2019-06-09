using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class MaximumFileSizeReachedException : Exception
    {
        public MaximumFileSizeReachedException()
        {
        }

        public MaximumFileSizeReachedException(string message) : base(message)
        {
        }

        public MaximumFileSizeReachedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MaximumFileSizeReachedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}