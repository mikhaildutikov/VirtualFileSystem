using System;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.DiskStructuresManagement
{
    /// <summary>
    /// Note: не додумано, как обезопасить эту штуку от записи откуда попало.
    /// </summary>
    [Serializable]
    internal class DataStreamDefinition
    {
        private readonly Int32 _contentsBlockIndex;
        private Int32 _streamLengthInBytes;

        public DataStreamDefinition(int contentsBlockReference, int streamLengthInBytes)
        {
            MethodArgumentValidator.ThrowIfNegative(contentsBlockReference, "contentsBlockReference");
            MethodArgumentValidator.ThrowIfNegative(streamLengthInBytes, "streamLengthInBytes");

            _contentsBlockIndex = contentsBlockReference;
            _streamLengthInBytes = streamLengthInBytes;
        }

        public Int32 ContentsBlockIndex
        {
            get
            {
                return _contentsBlockIndex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Int32 StreamLengthInBytes
        {
            get
            {
                return _streamLengthInBytes;
            }
            internal set
            {
                MethodArgumentValidator.ThrowIfNegative(value, "value");

                _streamLengthInBytes = value;
            }
        }
    }
}