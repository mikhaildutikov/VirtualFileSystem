namespace VirtualFileSystem.Streaming
{
    internal sealed class DataStreamToReadableAdapter : DataStreamReadable
    {
        private readonly DataStreamReadableWritable _dataStream;

        internal DataStreamToReadableAdapter(DataStreamReadableWritable dataStream)
        {
            _dataStream = dataStream;
        }

        public override void SetPosition(int newPosition)
        {
            _dataStream.SetPosition(newPosition);
        }

        public override int Read(byte[] bufferToReadBytesInto, int offset, int count)
        {
            return _dataStream.Read(bufferToReadBytesInto, offset, count);
        }

        public override int Length
        {
            get { return _dataStream.Length; }
        }

        public override int Position
        {
            get { return _dataStream.Position; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public override void Dispose()
        {
            _dataStream.Dispose();
        }

        public override void MoveToEnd()
        {
            _dataStream.MoveToEnd();
        }
    }
}