using System;
using System.IO;

namespace VirtualFileSystem.Disk
{
    internal class StreamValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamToValidate"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void Validate(Stream streamToValidate)
        {
            if (streamToValidate == null) throw new ArgumentNullException("streamToValidate");

            if (!streamToValidate.CanRead)
            {
                throw new ArgumentException("Из потока должно быть можно читать данные.", "streamToValidate");
            }

            if (!streamToValidate.CanWrite)
            {
                throw new ArgumentException("В поток должно быть можно писать данные", "streamToValidate");
            }

            if (!streamToValidate.CanSeek)
            {
                throw new ArgumentException("Поток должен поддерживать изменение текущей позиции.", "streamToValidate");
            }
        }
    }
}