using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using VirtualFileSystem.Extensions;
using VirtualFileSystem.Import;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Visitors;
using VirtualFileSystem.Toolbox.Extensions;

// ReSharper disable CheckNamespace
namespace VirtualFileSystem
// ReSharper restore CheckNamespace
{
    public static class VirtualFileSystemExtensions
    {
        /// <summary>
        /// Импортирует содержимое указанной папки в локальной файловой системе компьютера в виртуальную файловую систему.
        /// </summary>
        /// <param name="fileSystem">Виртуальная файловая система, в которую надо проимпортировать данные.</param>
        /// <param name="exportingFolderPath">Папка в локальной файловой системе компьютера, откуда следует проэкспортиовать данные.</param>
        /// <param name="virtualDestinationFolder">Папка, в которую следует проимпортировать содержимое <paramref name="exportingFolderPath"/>.</param>
        /// <returns>Объекты, показывающие результат импорта каждой папок, каждого из файлов.</returns>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="CannotGetImportedFolderStructureException"></exception>
        public static ReadOnlyCollection<FileSystemTaskResult> ImportFolderFromRealFileSystem(
            this VirtualFileSystem fileSystem,
            string exportingFolderPath,
            string virtualDestinationFolder)
        {
            return ImportFolderFromRealFileSystem(fileSystem, exportingFolderPath, virtualDestinationFolder,
                                                  new NullFileSystemCancellableTaskToken());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="exportingFolderPath"></param>
        /// <param name="virtualDestinationFolder"></param>
        /// <param name="taskToken"></param>
        /// <returns></returns>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="CannotGetImportedFolderStructureException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        internal static ReadOnlyCollection<FileSystemTaskResult> ImportFolderFromRealFileSystem(
            this VirtualFileSystem fileSystem,
            string exportingFolderPath,
            string virtualDestinationFolder,
            IFileSystemCancellableTaskToken taskToken)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(exportingFolderPath, "exportingFolderPath");
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(virtualDestinationFolder, "virtualDestinationFolder");

            FolderAddressable folderAddressable = CreateFileSystemObjectStructureFromFolder(exportingFolderPath);

            int totalNumberOfFilesToTraverse = folderAddressable.GetTotalFileCount();

            // Note: можно уменьшить связность классов, передав сюда через интерфейс фабрику, которая уж знает, как сделать нужного Visitor-а.

            ImportingAddressableObjectVisitor visitor = new ImportingAddressableObjectVisitor(fileSystem, exportingFolderPath, virtualDestinationFolder,
                                                             new RealFileContentsBufferFactory(), taskToken, totalNumberOfFilesToTraverse);

            if (!fileSystem.FolderExists(virtualDestinationFolder))
            {
                throw new FolderNotFoundException("Не удалось найти папку \"{0}\", в которую следует произвести копирование/импорт.".FormatWith(virtualDestinationFolder));
            }

            folderAddressable.Accept(visitor);
            var results = visitor.GetResult();

            return results;
        }

        /// <summary>
        /// Импортирует содержимое указанной папки в виртуальной файловой системе компьютера в другую виртуальную файловую систему.
        /// </summary>
        /// <param name="destinationFileSystem">Виртуальная файловая система, в которую надо проимпортировать данные.</param>
        /// <param name="sourceVirtualSystem">Виртуальная файловая система, в которую надо импортировать данные.</param>
        /// <param name="exportingFolderPath">Папка в виртуальной файловой системе компьютера, откуда следует проэкспортиовать данные.</param>
        /// <param name="virtualDestinationFolder">Папка, в которую следует проимпортировать содержимое <paramref name="exportingFolderPath"/>.</param>
        /// <returns>Объекты, показывающие результат импорта каждой папок, каждого из файлов.</returns>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="CannotGetImportedFolderStructureException"></exception>
        public static ReadOnlyCollection<FileSystemTaskResult> ImportFolderFromVirtualFileSystem(this VirtualFileSystem destinationFileSystem, VirtualFileSystem sourceVirtualSystem, string exportingFolderPath, string virtualDestinationFolder)
        {
            return ImportFolderFromVirtualFileSystem(destinationFileSystem, sourceVirtualSystem, exportingFolderPath,
                                                     virtualDestinationFolder, new NullFileSystemCancellableTaskToken());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destinationFileSystem"></param>
        /// <param name="sourceVirtualSystem"></param>
        /// <param name="exportingFolderPath"></param>
        /// <param name="virtualDestinationFolder"></param>
        /// <param name="taskToken"></param>
        /// <returns></returns>
        /// <exception cref="CannotGetImportedFolderStructureException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        internal static ReadOnlyCollection<FileSystemTaskResult> ImportFolderFromVirtualFileSystem(this VirtualFileSystem destinationFileSystem, VirtualFileSystem sourceVirtualSystem, string exportingFolderPath, string virtualDestinationFolder, IFileSystemCancellableTaskToken taskToken)
        {
            if (destinationFileSystem == null) throw new ArgumentNullException("destinationFileSystem");
            if (sourceVirtualSystem == null) throw new ArgumentNullException("sourceVirtualSystem");

            FolderAddressable folderAddressable = CreateFileSystemObjectStructureFromVirtualFolder(sourceVirtualSystem, exportingFolderPath);

            int totalNumberOfFilesToTraverse = folderAddressable.GetTotalFileCount();

            // Note: можно уменьшить связность классов, передав сюда через интерфейс фабрику, которая уж знает, как сделать нужного Visitor-а.

            var visitor = new ImportingAddressableObjectVisitor(destinationFileSystem, exportingFolderPath, virtualDestinationFolder,
                                                             new VirtualFileContentsBufferFactory(sourceVirtualSystem), taskToken, totalNumberOfFilesToTraverse);

            if (!destinationFileSystem.FolderExists(virtualDestinationFolder))
            {
                throw new FolderNotFoundException("Не удалось найти папку \"{0}\", в которую следует произвести копирование/импорт.".FormatWith(virtualDestinationFolder));
            }

            folderAddressable.Accept(visitor);
            return visitor.GetResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceVirtualSystem"></param>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        /// <exception cref="CannotGetImportedFolderStructureException"></exception>
        private static FolderAddressable CreateFileSystemObjectStructureFromVirtualFolder(VirtualFileSystem sourceVirtualSystem, string folderPath)
        {
            try
            {
                // TODO: логично сделать построение дерева атомарной операцией файловой системы. Делать я этого не стану - для простоты.
                IEnumerable<FileAddressable> files =
                    ((IFilesAndFoldersProvider)sourceVirtualSystem).GetAllFilesFrom(folderPath).Select(fileInfo => new FileAddressable(fileInfo.FullPath, fileInfo.Name));

                var subfolders = new List<FolderAddressable>();

                var folders = sourceVirtualSystem.GetAllFoldersFrom(folderPath);

                foreach (FolderInfo directoryInfo in folders)
                {
                    try
                    {
                        FolderAddressable folderInfo = CreateFileSystemObjectStructureFromVirtualFolder(sourceVirtualSystem, directoryInfo.FullPath);
                        subfolders.Add(folderInfo);
                    }
                    catch (FolderNotFoundException)
                    {
                    }
                }

                return new FolderAddressable(folderPath, sourceVirtualSystem.PathBuilder.GetFileOrFolderName(folderPath), subfolders, files);
            }
            catch (FolderNotFoundException exception)
            {
                throw new CannotGetImportedFolderStructureException("Не удалось проимпортиовать/скопировать содержимое \"{0}\" - такой папки не существует.".FormatWith(folderPath), exception);
            }
            catch (ObjectDisposedException exception)
            {
                throw new CannotGetImportedFolderStructureException("Не удалось проимпортиовать/скопировать содержимое \"{0}\" - экземпляр виртуальной файловой системы был закрыт явным вызовом Dispose().".FormatWith(folderPath), exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        /// <exception cref="CannotGetImportedFolderStructureException"></exception>
        private static FolderAddressable CreateFileSystemObjectStructureFromFolder(string folderPath)
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(folderPath);

                IEnumerable<FileAddressable> files = di.GetFiles().Select(fileInfo => new FileAddressable(fileInfo.FullName, fileInfo.Name));

                var subfolders = new List<FolderAddressable>();

                DirectoryInfo[] folders = di.GetDirectories();

                foreach (DirectoryInfo directoryInfo in folders)
                {
                    try
                    {
                        FolderAddressable folderInfo = CreateFileSystemObjectStructureFromFolder(directoryInfo.FullName);
                        subfolders.Add(folderInfo);
                    }
                    catch (DirectoryNotFoundException) // уже удалили? что ж - ничего страшного. Точнее, для простоты я говорю: "Ничего страшного". В общем случае это вопрос для обсуждения.
                    {
                    }
                }

                return new FolderAddressable(di.FullName, di.Name, subfolders, files);
            }
            catch (DirectoryNotFoundException)
            {
                throw new CannotGetImportedFolderStructureException("Не удалось проимпортиовать содержимое \"{0}\" - такой папки не существует.".FormatWith(folderPath));
            }
            catch (IOException exception) // увы, XML-комментарии относительно исключений в классах .Net FW точны далеко не всегда.
            {
                throw CreateGenericStructureCreationError(folderPath, exception);
            }
            catch(SecurityException exception)
            {
                throw new CannotGetImportedFolderStructureException("Не удалось проимпортиовать содержимое \"{0}\". Убедитесь, что у вашей учетной записи есть права доступа к папке и всем ее подпапкам. Далее - точные сведения об ошибке.{1}{2}".FormatWith(folderPath, Environment.NewLine, exception.Message));
            }
            catch (ArgumentException exception)
            {
                throw CreateGenericStructureCreationError(folderPath, exception);
            }
        }

        private static Exception CreateGenericStructureCreationError(string folderPath, Exception error)
        {
            return
                new CannotGetImportedFolderStructureException(
                    "Не удалось проимпортиовать содержимое \"{0}\". Далее - точные сведения об ошибке.{1}{2}".FormatWith
                        (folderPath, Environment.NewLine, error.Message));
        }
    }
}