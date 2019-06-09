using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using VirtualFileSystem.ContentsEnumerators;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Visitors
{
    internal class RealFileContentsBufferFactory : IFileContentsBufferFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        /// <exception cref="CannotGetFileContentsException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public IEnumerator<byte[]> GetBufferEnumeratorFor(string fullPath)
        {
            try
            {
                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                return new FileContentsEnumerator(fileStream, 100000);
            }
            catch (IOException exception)
            {
                throw CreateGenericCannotGetFileContentsException(fullPath, exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw CreateGenericCannotGetFileContentsException(fullPath, exception);
            }
            catch (SecurityException exception)
            {
                throw CreateGenericCannotGetFileContentsException(fullPath, exception);
            }
            catch (NotSupportedException exception)
            {
                throw CreateGenericCannotGetFileContentsException(fullPath, exception);
            }
        }

        private static CannotGetFileContentsException CreateGenericCannotGetFileContentsException(string fullPath, Exception exception)
        {
            return new CannotGetFileContentsException(
                "Не удалось получить содержимое файла \"{0}\"".FormatWith(fullPath), exception);
        }
    }
}