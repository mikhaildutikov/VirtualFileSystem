using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class MaximumFolderCountReachedException : Exception
    {
        public MaximumFolderCountReachedException()
        {
        }

        public MaximumFolderCountReachedException(string message) : base(message)
        {
        }

        public MaximumFolderCountReachedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MaximumFolderCountReachedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}