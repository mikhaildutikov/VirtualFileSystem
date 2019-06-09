using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests.FileSystemTests
{
    [TestClass]
    public class CopyFileTests
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
        public void CreateSeveralEmptyFilesUnderOneFolderThenCopyThemToAnotherFolderCheckingTheyDoAppearInDestinationFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var fileA = fileSystem.CreateFile(@"\A");
            var fileB = fileSystem.CreateFile(@"\B");
            var fileC = fileSystem.CreateFile(@"\C");

            fileSystem.CreateFolder(@"\A");
            var folderB = fileSystem.CreateFolder(@"\A\B");

            fileSystem.CopyFile(fileA.FullPath, fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "1"));
            fileSystem.CopyFile(fileB.FullPath, fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "2"));
            fileSystem.CopyFile(fileC.FullPath, fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "3"));

            var namesOfFilesUnderRoot = fileSystem.GetNamesOfAllFilesFrom(folderB.FullPath);

            CollectionAssert.AreEquivalent(new[] { "1", "2", "3" }, namesOfFilesUnderRoot);
        }

        [TestMethod]
        public void TryCopyingANonEmptyFileComparingTheContentsOfBothCopiesAfterwards()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            byte[] interestingData = ByteBufferFactory.BuildSomeGuidsIntoByteArray(10000);

            var greatGatsby = fileSystem.CreateFile(@"\TheGreatGatsbyEncoded.bin");

            using (var stream = fileSystem.OpenFileForWriting(greatGatsby.FullPath))
            {
                stream.Write(interestingData, 0, interestingData.Length);
            }

            var folderInfo = fileSystem.CreateFolder(@"\AifdjgodfjgodfjglidflgjalglkrelgkrelllerlnlrklgnLKGLKREL");

            fileSystem.CopyFile(greatGatsby.FullPath,
                                fileSystem.PathBuilder.CombinePaths(folderInfo.FullPath, "HiThereSport.txt"));

            var interestingDataAsReadFromFile = new byte[interestingData.Length];

            using (var stream = fileSystem.OpenFileForReading(fileSystem.PathBuilder.CombinePaths(folderInfo.FullPath, "HiThereSport.txt")))
            {
                stream.Read(interestingDataAsReadFromFile, 0, stream.Length);
            }

            CollectionAssert.AreEqual(interestingData, interestingDataAsReadFromFile);
        }

        [TestMethod]
        public void MakeSureYouCannotCopyAFileThatDoesNotExist()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileNotFoundException>(
                delegate
                    {
                        fileSystem.CopyFile(@"\TheGreatGatsbyEncoded.bin", @"\lkfmldgkdmglfklmgldlgm");
                    });
        }

        [TestMethod]
        public void MakeSureTryingToCopyFileInItsOwnPlaceDoesNotThrow()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var greatGatsby = fileSystem.CreateFile(@"\TheGreatGatsbyEncoded.bin");

            fileSystem.CopyFile(greatGatsby.FullPath, greatGatsby.FullPath);

            CollectionAssert.AreEquivalent(new[] {greatGatsby.Name}, fileSystem.GetNamesOfAllFilesFrom(@"\"));
        }

        [TestMethod]
        public void MakeSureTryingToCopyAFileToAPathPointingToAnExistingFileFails()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\Folder");
            var greatGatsby = fileSystem.CreateFile(@"\Folder\TheGreatGatsbyEncoded.bin");
            var greatGatsbyUnderRoot = fileSystem.CreateFile(@"\TheGreatGatsbyEncoded.bin");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileAlreadyExistsException>(
                delegate
                    {
                        fileSystem.CopyFile(greatGatsbyUnderRoot.FullPath, greatGatsby.FullPath);
                    });
        }

        [TestMethod]
        public void MakeSureYouCannotCopyAFileToNonExistingFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var file1 = fileSystem.CreateFile(@"\file1");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                    {
                        fileSystem.CopyFile(file1.FullPath, @"\A\B\C\ldfmsdkmflsmd");
                    });
        }

        [TestMethod]
        public void MakeSureYouCannotCopyAFileLockedForWritingButCanCopyIfItIsOnlyLockedForReading()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var file1 = fileSystem.CreateFile(@"\file1");

            using (var stream = fileSystem.OpenFileForWriting(file1.FullPath))
            {
                ExceptionAssert.MakeSureExceptionIsRaisedBy<FileLockedException>(
                    delegate
                        {
                            fileSystem.CopyFile(file1.FullPath, @"\File1Copy");
                        });
            }

            using (var stream = fileSystem.OpenFileForReading(file1.FullPath))
            {
                fileSystem.CopyFile(file1.FullPath, @"\File1Copy");
            }

            CollectionAssert.AreEquivalent(new[] { "file1", "File1Copy" }, fileSystem.GetNamesOfAllFilesFrom(@"\"));
        }

        [TestMethod]
        public void MakeSureCopyingFileWhenThereIsNotEnoughDiskSpaceThrowsRemovingTheCopyIfOneHasBeenMade()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var greatGatsby = fileSystem.CreateFile(@"\TheGreatGatsbyEncoded.bin");

            using (var stream = fileSystem.OpenFileForWriting(greatGatsby.FullPath))
            {
                stream.SetLength(fileSystem.FreeSpaceInBytes * 3 / 4);
            }

            int freeSpaceBeforeAttemptingToCopy = fileSystem.FreeSpaceInBytes;

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InsufficientSpaceException>(
                delegate
                    {
                        fileSystem.CopyFile(greatGatsby.FullPath, @"\GreatGatsbyCopy.bin");
                    });
            
            Assert.AreEqual(freeSpaceBeforeAttemptingToCopy, fileSystem.FreeSpaceInBytes);
            CollectionAssert.AreEquivalent(new[] { greatGatsby.Name }, fileSystem.GetNamesOfAllFilesFrom(@"\"));
        }
    }
}