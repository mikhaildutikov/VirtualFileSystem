using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class MaximumFileCountReachedException : Exception
    {
        public MaximumFileCountReachedException()
        {
        }

        public MaximumFileCountReachedException(string message) : base(message)
        {
        }

        public MaximumFileCountReachedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MaximumFileCountReachedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}