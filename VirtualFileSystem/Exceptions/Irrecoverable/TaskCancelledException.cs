using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Exceptions.Irrecoverable
{
    [Serializable]
    public class TaskCancelledException : Exception
    {
        public TaskCancelledException()
        {
        }

        public TaskCancelledException(string message)
            : base(message)
        {
        }

        public TaskCancelledException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected TaskCancelledException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
