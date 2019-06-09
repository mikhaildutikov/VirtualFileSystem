using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem
{
    public partial class VirtualFileSystem
    {
        // Note: этот код специально прилеплен к и без того немаленькому VirtualFileSystem. Это повышает discoverability фабричных методов (по сравнению с выделением фабрики) и усиливает ограничения (private-конструктор против internal-конструктора).

        /// <summary>
        /// Создает виртуальную файловую систему с настройками по умолчанию, на основе указанного файла.
        /// </summary>
        /// <param name="fullPathForFile"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        public static VirtualFileSystem OpenExisting(string fullPathForFile)
        {
            if (String.IsNullOrEmpty(fullPathForFile))
            {
                throw new ArgumentNullException("fullPathForFile");
            }

            FileStream stream = CreateStreamFromExistingFileWrappingExceptions(fullPathForFile);

            try
            {
                VirtualDisk disk = VirtualDisk.CreateFromStream(stream);

                var diskStructuresManager = new FileSystemNodeStorage(disk);

                FileSystemHeader header = diskStructuresManager.ReadFileSystemHeader(VirtualDiskFormatter.FileSystemHeaderBlockIndex);

                var nameValidator = FileSystemArtifactNamesValidator.Default;

                var pathValidator = PathValidator.Default;

                var pathBuilder = PathBuilder.Default;

                var nodeResolver = new NodeResolver(disk, diskStructuresManager, StringComparer.OrdinalIgnoreCase, header.RootBlockOffset, VirtualFileSystem.Root, VirtualFileSystem.DirectorySeparatorChar, pathValidator, pathBuilder);

                return VirtualFileSystem.CreateFromDisk(disk, StringComparer.OrdinalIgnoreCase, nodeResolver, pathBuilder, nameValidator, pathValidator);
            }
            catch(VirtualDiskCreationFailedException exception)
            {
                throw CreateGenericSystemCreationFromExistingFileException(exception, fullPathForFile);
            }
            catch (InconsistentDataDetectedException exception)
            {
                throw CreateGenericSystemCreationFromExistingFileException(exception, fullPathForFile);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPathForFile"></param>
        /// <returns></returns>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        private static FileStream CreateStreamFromExistingFileWrappingExceptions(string fullPathForFile)
        {
            try
            {
                return new FileStream(fullPathForFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            }
            catch(ArgumentException exception)
            {
                throw WrapStreamFromExistingFileCreationException(exception, fullPathForFile);
            }
            catch(IOException exception)
            {
                throw WrapStreamFromExistingFileCreationException(exception, fullPathForFile);
            }
            catch(SecurityException exception)
            {
                throw WrapStreamFromExistingFileCreationException(exception, fullPathForFile);
            }
            catch(UnauthorizedAccessException exception)
            {
                throw WrapStreamFromExistingFileCreationException(exception, fullPathForFile);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="fullPathForFile"></param>
        /// <returns></returns>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        private static Exception WrapStreamFromExistingFileCreationException(Exception exception, string fullPathForFile)
        {
            return new FileSystemCreationFailedException(
                "Не удалось создать экземпляр файловой системы из файла \"{0}\". Убедитесь, что файл с таким именем существует, что у вашей учетной записи есть к нему доступ, права на то, писать и читать из него данные. Далее - подробные сведения об ошибке.{1}{2}"
                    .FormatWith(fullPathForFile, Environment.NewLine, exception.Message), exception);
        }

        /// <summary>
        /// Создает виртуальную файловую систему с настройками по умолчанию, на основе указанного файла.
        /// </summary>
        /// <param name="fullPathForFile">Файл, который файловая система должна использовать для хранения своих данных. Принимаются только пути, ведущие к несуществующим файлам.</param>
        /// <param name="desiredSizeInBytes">Размер нового диска. В текущей версии должен быть кратен 2048 байтам, не больше 1Гб.</param>
        /// <returns>Экземпляр файловой системы, в качестве хранилища использующей указанный файл.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        public static VirtualFileSystem CreateNew(string fullPathForFile, int desiredSizeInBytes)
        {
            if (String.IsNullOrEmpty(fullPathForFile))
            {
                throw new ArgumentNullException("fullPathForFile", "Путь к файлу не может быть пустым.");
            }

            var stream = CreateStreamWrappingExceptions(fullPathForFile);

            try
            {
                var formatter = new VirtualDiskFormatter();

                VirtualDisk disk = VirtualDisk.CreateFormattingTheStream(stream, VirtualDisk.OnlySupportedBlockSize, desiredSizeInBytes);

                var diskStructuresManager = new FileSystemNodeStorage(disk);

                formatter.Format(disk, diskStructuresManager);

                FileSystemHeader header = diskStructuresManager.ReadFileSystemHeader(VirtualDiskFormatter.FileSystemHeaderBlockIndex);

                var nameValidator = FileSystemArtifactNamesValidator.Default;

                var pathValidator = PathValidator.Default;

                var pathBuilder = PathBuilder.Default;

                var nodeResolver = new NodeResolver(disk, diskStructuresManager, StringComparer.OrdinalIgnoreCase, header.RootBlockOffset, VirtualFileSystem.Root, VirtualFileSystem.DirectorySeparatorChar, pathValidator, pathBuilder);

                var newVirtualFileSystem = VirtualFileSystem.CreateFromDisk(disk, StringComparer.OrdinalIgnoreCase, nodeResolver, pathBuilder, nameValidator, pathValidator);

                return newVirtualFileSystem;
            }
            catch (InconsistentDataDetectedException exception)
            {
                CleanUpInconsistentFileSystemContainer(fullPathForFile, stream);

                throw CreateGenericSystemCreationException(exception, fullPathForFile);
            }
            catch (VirtualDiskCreationFailedException exception)
            {
                CleanUpInconsistentFileSystemContainer(fullPathForFile, stream);

                throw CreateGenericSystemCreationException(exception, fullPathForFile);
            }
        }

        private static void CleanUpInconsistentFileSystemContainer(string fullPathForFile, FileStream stream)
        {
            stream.Dispose();

            try
            {
                File.Delete(fullPathForFile);
            }
            catch (IOException)
            {
            }
            catch(UnauthorizedAccessException)
            {
            }
        }

        private static Exception CreateGenericSystemCreationFromExistingFileException(Exception exception, string fullPathForFile)
        {
            return new FileSystemCreationFailedException(
                "Не удалось создать экземпляр файловой системы из файла \"{0}\". Убедитесь, что действительно открываете файл, содержащий данные виртуальной файловой системы. Далее - подробные сведения об ошибке.{1}{2}"
                    .FormatWith(fullPathForFile, Environment.NewLine, exception.Message), exception);
        }

        private static Exception CreateGenericSystemCreationException(Exception exception, string fullPathForFile)
        {
            return new FileSystemCreationFailedException(
                "Не удалось создать экземпляр файловой системы из файла \"{0}\". Далее - подробные сведения об ошибке.{1}{2}"
                    .FormatWith(fullPathForFile, Environment.NewLine, exception.Message), exception);
        }

        private static Exception WrapStreamCreationException(Exception exception, string fullPathForFile)
        {
            return new FileSystemCreationFailedException(
                "Не удалось создать экземпляр файловой системы из файла \"{0}\". Убедитесь, что файла с таким именем не существует, что у вас есть права на то, чтобы его создать в указанной вами папке, на то, чтобы писать и читать из него данные. Далее - подробные сведения об ошибке.{1}{2}"
                    .FormatWith(fullPathForFile, Environment.NewLine, exception.Message), exception);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPathForFile"></param>
        /// <returns></returns>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        private static FileStream CreateStreamWrappingExceptions(string fullPathForFile)
        {
            try
            {
                return new FileStream(fullPathForFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            }
            catch(ArgumentException exception)
            {
                throw WrapStreamCreationException(exception, fullPathForFile);
            }
            catch(IOException exception)
            {
                throw WrapStreamCreationException(exception, fullPathForFile);
            }
            catch(SecurityException exception)
            {
                throw WrapStreamCreationException(exception, fullPathForFile);
            }
            catch(UnauthorizedAccessException exception)
            {
                throw WrapStreamCreationException(exception, fullPathForFile);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disk"></param>
        /// <param name="namesComparer"></param>
        /// <param name="nodeResolver"></param>
        /// <param name="pathBuilder"></param>
        /// <param name="namesValidator"></param>
        /// <param name="pathValidator"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InconsistentDataDetectedException"></exception>
        internal static VirtualFileSystem CreateFromDisk(IVirtualDisk disk, IEqualityComparer<string> namesComparer, NodeResolver nodeResolver, PathBuilder pathBuilder, IFileSystemArtifactNamesValidator namesValidator, IPathValidator pathValidator)
        {
            if (disk == null) throw new ArgumentNullException("disk");
            if (namesComparer == null) throw new ArgumentNullException("namesComparer");
            if (nodeResolver == null) throw new ArgumentNullException("nodeResolver");
            if (pathBuilder == null) throw new ArgumentNullException("pathBuilder");
            if (namesValidator == null) throw new ArgumentNullException("namesValidator");
            if (pathValidator == null) throw new ArgumentNullException("pathValidator");

            VirtualFileSystemInfo fileSystemInfo;
            var fileSystemNodeStorage = new FileSystemNodeStorage(disk);

            const int headerBlockIndex = VirtualDiskFormatter.FileSystemHeaderBlockIndex;
            const int freeBlockBitmapStartingBlockIndex = VirtualDiskFormatter.FreeSpaceStartingBlockIndex;

            var header = fileSystemNodeStorage.ReadFileSystemHeader(headerBlockIndex);

            fileSystemInfo = new VirtualFileSystemInfo(header.Version, disk.BlockSizeInBytes, header.RootBlockOffset, freeBlockBitmapStartingBlockIndex);

            var freeSpaceBitmapStore = new FreeSpaceBitmapStore(disk, VirtualDiskFormatter.FreeSpaceStartingBlockIndex);

            int bitmapSize;
            var freeSpaceMap = freeSpaceBitmapStore.ReadMap(out bitmapSize);

            var freeSpaceBitArray = new BitArray(freeSpaceMap) { Length = bitmapSize };

            var freeBlockManagerBitArrayBased = new FreeBlockManagerBitArrayBased(freeSpaceBitArray,
                                                                     fileSystemInfo.FirstNonReservedDiskBlockIndex,
                                                                     bitmapSize);

            IFreeBlockManager freeBlockManager = new FreeBlockManagerDiskWriting(freeSpaceBitmapStore, freeBlockManagerBitArrayBased);

            IFolderEnumeratorRegistry folderEnumeratorRegistry = new FolderEnumeratorRegistry();

            IFileSystemObjectLockingManager lockingManager = new FileSystemObjectLockingManager();

            var blockReferenceEditor = new BlockReferenceListsEditor(disk, freeBlockManager, fileSystemNodeStorage);

            //Note: много общих коллабораторов у трех классов. Недорефакторено.

            var fileManager = new FileManager(disk, fileSystemNodeStorage, namesComparer, nodeResolver, freeBlockManager, folderEnumeratorRegistry, lockingManager, blockReferenceEditor, pathBuilder, namesValidator, pathValidator);
            var folderManager = new FolderManager(fileSystemNodeStorage, namesComparer, nodeResolver, freeBlockManager, folderEnumeratorRegistry, lockingManager, blockReferenceEditor, pathBuilder, namesValidator, pathValidator);

            return new VirtualFileSystem(disk, fileSystemInfo, fileSystemNodeStorage, namesComparer, nodeResolver, freeBlockManager, folderEnumeratorRegistry, blockReferenceEditor, pathBuilder, namesValidator, pathValidator, fileManager, folderManager);
        }
    }
}