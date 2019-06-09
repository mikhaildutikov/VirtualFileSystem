using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace VirtualFileSystem.ContentsEnumerators
{
    internal class FileContentsEnumerator : IEnumerator<byte[]>
    {
        private readonly Stream _fileStream;
        private readonly uint _bufferSize;

        public FileContentsEnumerator(Stream fileStream, uint bufferSize)
        {
            if (fileStream == null) throw new ArgumentNullException("fileStream");

            if (!fileStream.CanRead)
            {
                throw new ArgumentException("fileStream");
            }

            _fileStream = fileStream;
            _bufferSize = bufferSize;
        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }

        public bool MoveNext()
        {
            var buffer = new byte[_bufferSize];

            int numberOfBytesRead = _fileStream.Read(buffer, 0, (int) _bufferSize);

            if (numberOfBytesRead == 0)
            {
                return false;
            }

            if (numberOfBytesRead != _bufferSize)
            {
                // resizing...

                var bufferOfTheRightSize = new byte[numberOfBytesRead];

                Array.Copy(buffer, bufferOfTheRightSize, numberOfBytesRead);

                this.Current = bufferOfTheRightSize;
            }
            else
            {
                this.Current = buffer;
            }

            return true;
        }

        public void Reset()
        {
            _fileStream.Position = 0;
            this.Current = null;
        }

        public byte[] Current { get; private set; }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}