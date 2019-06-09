using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem.Exceptions.Irrecoverable
{
    /// <summary>
    /// Возникновение этого исключения свидетельствует о нарушении консистентности данных файловой системы.
    /// Ловить его не следует, корректо его обработать невозможно. В релизной версии оно возникать не должно.
    /// Если возникло, то есть шансы, что часть данных вы потеряли. Это что-то вроде ExecutionEngineException в .Net FW.
    /// </summary>
    [Serializable]
    public class InconsistentDataDetectedException : Exception
    {
        /// <summary>
        /// Конструирует новый экземпляр <see cref="InconsistentDataDetectedException"/>.
        /// </summary>
        public InconsistentDataDetectedException()
        {
        }

        /// <summary>
        /// Конструирует новый экземпляр <see cref="InconsistentDataDetectedException"/>.
        /// </summary>
        /// <param name="message"></param>
        public InconsistentDataDetectedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Конструирует новый экземпляр <see cref="InconsistentDataDetectedException"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public InconsistentDataDetectedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InconsistentDataDetectedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}