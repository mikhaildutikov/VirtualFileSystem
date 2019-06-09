using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class FileNotFoundException : Exception
    {
        public FileNotFoundException()
        {
        }

        public FileNotFoundException(string message)
            : base(message)
        {
        }

        public FileNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected FileNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}