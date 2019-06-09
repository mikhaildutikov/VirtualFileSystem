using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class VirtualDiskFormatterTests
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void TryFormattingADiskAndInitializingFileSystemWithIt()
        {
            Stream stream = new MemoryStream(); // System.IO.File.Open(@"c:\bs.bin", FileMode.Create);

            var formatter = new VirtualDiskFormatter();

            VirtualDisk disk = VirtualDisk.CreateFormattingTheStream(stream, 2048, 2048 * 5000);

            var diskStructuresManager = new FileSystemNodeStorage(disk);

            formatter.Format(disk, diskStructuresManager);

            FileSystemHeader header = diskStructuresManager.ReadFileSystemHeader(VirtualDiskFormatter.FileSystemHeaderBlockIndex);

            var nameValidator = new FileSystemArtifactNamesValidator(Constants.IllegalCharactersForNames, Constants.FileAndFolderMaximumNameLength);

            var pathValidator = new PathValidator(VirtualFileSystem.Root, Constants.IllegalCharactersForPaths, nameValidator, VirtualFileSystem.DirectorySeparatorChar);

            var pathBuilder = PathBuilder.Default;

            var nodeResolver = new NodeResolver(disk, diskStructuresManager, StringComparer.OrdinalIgnoreCase, header.RootBlockOffset, VirtualFileSystem.Root, VirtualFileSystem.DirectorySeparatorChar, pathValidator, pathBuilder);

            VirtualFileSystem fileSystem = VirtualFileSystem.CreateFromDisk(disk, StringComparer.OrdinalIgnoreCase, nodeResolver, pathBuilder, nameValidator, pathValidator);

            Assert.AreEqual(2048, fileSystem.FileSystemInfo.BlockSizeInBytes);
            Assert.AreEqual(new Version(1, 0, 0, 0), fileSystem.FileSystemInfo.Version);
        }
    }
}