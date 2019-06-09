using System;
using VirtualFileSystem.Addressing;
using VirtualFileSystem.Exceptions.Irrecoverable;

namespace VirtualFileSystem.Streaming
{
    internal interface IDataStreamStructureBuilder
    {
        int CurrentSize { get; }
        int MaximumSize { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newSize"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="NoFreeBlocksException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="MaximumFileSizeReachedException"></exception>
        void SetSize(int newSize);

        IDoubleIndirectDataStreamEnumerator CreateEnumerator();
    }
}