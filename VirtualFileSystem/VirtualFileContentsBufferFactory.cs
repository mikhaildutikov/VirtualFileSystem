using System;
using System.Collections.Generic;
using System.IO;
using VirtualFileSystem.ContentsEnumerators;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Visitors
{
    internal class VirtualFileContentsBufferFactory : IFileContentsBufferFactory
    {
        private readonly VirtualFileSystem _fileSystem;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public VirtualFileContentsBufferFactory(VirtualFileSystem fileSystem)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");

            _fileSystem = fileSystem;
        }

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
                var dataStream = _fileSystem.OpenFileForReading(fullPath);

                return new FileContentsEnumerator(new DataStreamReadableAdaptedToStream(dataStream), 100000);
            }
            catch (ArgumentNullException exception)
            {
                throw CreateGenericCannotGetFileContentsException(fullPath, exception);
            }
            catch (FileNotFoundException exception)
            {
                throw CreateGenericCannotGetFileContentsException(fullPath, exception);
            }
            catch(FileLockedException exception)
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