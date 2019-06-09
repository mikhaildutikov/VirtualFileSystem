using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class MoveFolderTests
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
        public void MakeSureMovingUnlistsFolderFromOldParentAndEnlistsItInNewOnePreservingChildren()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var folderA = fileSystem.CreateFolder(@"\A");
            var folderB = fileSystem.CreateFolder(@"\A\B");
            var folderC = fileSystem.CreateFolder(@"\A\B\C");
            var fileA = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "AUnderB.txt"));
            var fileB = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "BUnderB.txt"));
            var fileC = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderC.FullPath, "C1.txt"));
            var fileD = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderC.FullPath, "C2.txt"));

            var folderBMoved = fileSystem.MoveFolder(folderB.FullPath, VirtualFileSystem.Root);
            Assert.AreEqual(@"\B", folderBMoved.FullPath);

            CollectionAssert.AreEquivalent(new List<string>(), fileSystem.GetNamesOfAllFilesFrom(folderA.FullPath));
            CollectionAssert.AreEquivalent(new List<string>(), fileSystem.GetNamesOfAllFoldersFrom(folderA.FullPath));
            CollectionAssert.AreEquivalent(new[] { folderA.Name, folderB.Name }, fileSystem.GetNamesOfAllFoldersFrom(VirtualFileSystem.Root));
            CollectionAssert.AreEquivalent(new[] { fileA.Name, fileB.Name }, fileSystem.GetNamesOfAllFilesFrom(folderBMoved.FullPath));
            CollectionAssert.AreEquivalent(new[] { folderC.Name }, fileSystem.GetNamesOfAllFoldersFrom(folderBMoved.FullPath));
            CollectionAssert.AreEquivalent(new[] { fileC.Name, fileD.Name }, fileSystem.GetNamesOfAllFilesFrom(@"\B\c"));
        }

        [TestMethod]
        public void MakeSureYouCannotMoveANonExistingFolder()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFolder = fileSystem.CreateFolder(@"\Folder");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                {
                    fileSystem.MoveFolder(@"\Guid.NewGuid()", newFolder.FullPath);
                });
        }

        [TestMethod]
        public void MakeSureYouCannotMoveFolderToANonExistingFolder()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFolder = fileSystem.CreateFolder(@"\WarAndPeace.Editions");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                {
                    fileSystem.MoveFile(newFolder.FullPath, @"\BogusFolder");
                });
        }

        [TestMethod]
        public void MakeSureMovingAFolderInvalidatesAllEnumeratorsUnderIt()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var folderA = fileSystem.CreateFolder(@"\A");
            var folderB = fileSystem.CreateFolder(@"\A\B");
            var folderC = fileSystem.CreateFolder(@"\A\B\C");
            var folderBUnderRoot = fileSystem.CreateFolder(@"\B");

            var enumeratorA = fileSystem.EnumerateFilesUnderFolder(folderA.FullPath, "*");
            var enumeratorB = fileSystem.EnumerateFilesUnderFolder(folderB.FullPath, "*");
            var enumeratorC = fileSystem.EnumerateFilesUnderFolder(folderC.FullPath, "*");

            fileSystem.MoveFolder(folderA.FullPath, folderBUnderRoot.FullPath);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidOperationException>(
                delegate
                    {
                        enumeratorA.MoveNext();
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidOperationException>(
                delegate
                {
                    enumeratorB.MoveNext();
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidOperationException>(
                delegate
                {
                    enumeratorC.MoveNext();
                });
        }

        [TestMethod]
        public void MakeSureYouCannotMoveAFolderWhenDestinationFolderAlreadyContainsDirectoryWithGivenName()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFolder = fileSystem.CreateFolder(@"\Fitzgerald");
            var newSubfolder = fileSystem.CreateFolder(@"\Books");
            fileSystem.CreateFolder(@"\Books\Fitzgerald");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderAlreadyExistsException>(
                delegate
                {
                    fileSystem.MoveFolder(newFolder.FullPath, newSubfolder.FullPath);
                });
        }

        [TestMethod]
        public void MakeSureYouCanMoveFolderToTheSameFolderItIsIn()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var newFolder = fileSystem.CreateFolder(@"\Books");
            var movedFolder = fileSystem.MoveFolder(newFolder.FullPath, VirtualFileSystem.Root);

            var namesOfFolders = fileSystem.GetNamesOfAllFoldersFrom(VirtualFileSystem.Root);

            CollectionAssert.AreEqual(new[] { newFolder.Name }, namesOfFolders);
        }

        [TestMethod]
        public void MakeSureYouCannotMoveAFolderWhileItOrItsSubfoldersIsLocked()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            fileSystem.CreateFolder(@"\Books");
            var literacyFolder = fileSystem.CreateFolder(@"\Books\Literacy");
            var tolstoysFolder = fileSystem.CreateFolder(@"\books\literacy\tolstoj");
            var warAndPeace = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(tolstoysFolder.FullPath, "WarAndPeace.txt"));
            
            using (var dataStream = fileSystem.OpenFileForReading(warAndPeace.FullPath))
            {
                ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderLockedException>(
                    delegate
                    {
                        fileSystem.MoveFolder(literacyFolder.FullPath, VirtualFileSystem.Root);
                        fileSystem.MoveFolder(tolstoysFolder.FullPath, VirtualFileSystem.Root);
                    });
            }

            using (var dataStream = fileSystem.OpenFileForWriting(warAndPeace.FullPath))
            {
                ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderLockedException>(
                    delegate
                    {
                        fileSystem.MoveFolder(literacyFolder.FullPath, VirtualFileSystem.Root);
                        fileSystem.MoveFolder(tolstoysFolder.FullPath, VirtualFileSystem.Root);
                    });
            }

            fileSystem.MoveFolder(literacyFolder.FullPath, VirtualFileSystem.Root);
        }

        [TestMethod]
        public void MakeSureMovingAFolderIsProhibitedIfYouAreMovingItDownItsOwnPathToRoot()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            var folderA = fileSystem.CreateFolder(@"\A");
            fileSystem.CreateFolder(@"\A\B");
            var folderC = fileSystem.CreateFolder(@"\A\B\C");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidOperationException>(
                delegate
                    {
                        fileSystem.MoveFolder(folderA.FullPath, folderC.FullPath);
                    });
        }
    }
}