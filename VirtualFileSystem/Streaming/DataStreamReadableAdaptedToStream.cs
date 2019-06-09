using System;
using System.IO;

namespace VirtualFileSystem.Visitors
{
    internal class DataStreamReadableAdaptedToStream : Stream
    {
        private readonly DataStreamReadable _dataStreamReadable;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataStreamReadable"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DataStreamReadableAdaptedToStream(DataStreamReadable dataStreamReadable)
        {
            if (dataStreamReadable == null) throw new ArgumentNullException("dataStreamReadable");
            _dataStreamReadable = dataStreamReadable;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _dataStreamReadable.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _dataStreamReadable.Length; }
        }

        public override long Position
        {
            get { return _dataStreamReadable.Position; }
            set { _dataStreamReadable.SetPosition((int)value); }
        }
    }
}