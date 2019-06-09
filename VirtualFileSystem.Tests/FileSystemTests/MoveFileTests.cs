using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.TestFactories;
using VirtualFileSystem.Tests.Helpers;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class MoveFileTests
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
        public void MakeSureMovingUnlistsFileFromOldParentAndEnlistsItInNewOne()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var folderA = fileSystem.CreateFolder(@"\A");
            fileSystem.CreateFolder(@"\A\B");
            var folderC = fileSystem.CreateFolder(@"\A\B\C");

            var fileA = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderA.FullPath, "AUnderA.txt"));
            var fileB = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderA.FullPath, "BUnderA.txt"));

            CollectionAssert.AreEquivalent(new[] { fileA.Name, fileB.Name }, fileSystem.GetNamesOfAllFilesFrom(folderA.FullPath));

            fileSystem.MoveFile(fileA.FullPath, folderC.FullPath);

            CollectionAssert.AreEquivalent(new[] { fileB.Name }, fileSystem.GetNamesOfAllFilesFrom(folderA.FullPath));
            CollectionAssert.AreEquivalent(new[] { fileA.Name }, fileSystem.GetNamesOfAllFilesFrom(folderC.FullPath));
        }

        [TestMethod]
        public void MakeSureYouCannotMoveANonExistingFile()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFolder = fileSystem.CreateFolder(@"\Folder");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileNotFoundException>(
                delegate
                    {
                        fileSystem.MoveFile(@"\MyFile.ggg", newFolder.FullPath); 
                    });
        }

        [TestMethod]
        public void MakeSureYouCannotMoveFileToANonExistingFolder()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFile = fileSystem.CreateFile(@"\WarAndPeace.txt");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                {
                    fileSystem.MoveFile(newFile.FullPath, @"\BogusFolder");
                });
        }

        [TestMethod]
        public void MakeSureYouCannotMoveAFileWhileItIsLocked()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFile = fileSystem.CreateFile(@"\WarAndPeace.txt");
            var newFolder = fileSystem.CreateFolder(@"\Books");

            using (var dataStream = fileSystem.OpenFileForReading(newFile.FullPath))
            {
                ExceptionAssert.MakeSureExceptionIsRaisedBy<FileLockedException>(
                    delegate
                    {
                        fileSystem.MoveFile(newFile.FullPath, newFolder.FullPath);
                    });
            }

            using (var dataStream = fileSystem.OpenFileForWriting(newFile.FullPath))
            {
                ExceptionAssert.MakeSureExceptionIsRaisedBy<FileLockedException>(
                    delegate
                    {
                        fileSystem.MoveFile(newFile.FullPath, newFolder.FullPath);
                    });
            }

            fileSystem.MoveFile(newFile.FullPath, newFolder.FullPath);
        }

        [TestMethod]
        public void MakeSureYouCannotMoveAFileWhenDestinationFolderAlreadyContainsFileWithGivenName()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFile = fileSystem.CreateFile(@"\WarAndPeace.txt");
            var newFolder = fileSystem.CreateFolder(@"\Books");
            
            fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(newFolder.FullPath, newFile.Name));
            
            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileAlreadyExistsException>(
                delegate
                {
                    fileSystem.MoveFile(newFile.FullPath, newFolder.FullPath);
                });
        }

        [TestMethod]
        public void MakeSureYouCanMoveFileToTheSameFolderItIsIn()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFolder = fileSystem.CreateFolder(@"\Books");
            var newFile = fileSystem.CreateFile(@"\Books\WarAndPeace.txt");

            fileSystem.MoveFile(newFile.FullPath, newFolder.FullPath);

            CollectionAssert.AreEqual(new[] {newFile.Name}, fileSystem.GetNamesOfAllFilesFrom(newFolder.FullPath));
        }
    }
}