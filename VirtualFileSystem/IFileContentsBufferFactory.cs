using System;
using System.Collections.Generic;

namespace VirtualFileSystem.Visitors
{
    internal interface IFileContentsBufferFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        /// <exception cref="CannotGetFileContentsException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        IEnumerator<byte[]> GetBufferEnumeratorFor(string fullPath);
    }
}