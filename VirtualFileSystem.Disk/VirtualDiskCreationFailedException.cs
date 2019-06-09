using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Disk
{
    /// <summary>
    /// Генерируется в случае возникновения ошибки при инициализации виртуального диска.
    /// </summary>
    [Serializable]
    public class VirtualDiskCreationFailedException : Exception
    {
        /// <summary>
        /// Создает новый экземпляр <see cref="VirtualDiskCreationFailedException"/>.
        /// </summary>
        public VirtualDiskCreationFailedException()
        {
        }

        /// <summary>
        /// Создает новый экземпляр <see cref="VirtualDiskCreationFailedException"/>.
        /// </summary>
        public VirtualDiskCreationFailedException(string message)
            : base(message)
        {
        }

        protected VirtualDiskCreationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Создает новый экземпляр <see cref="VirtualDiskCreationFailedException"/>.
        /// </summary>
        public VirtualDiskCreationFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}