using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests.FileSystemTests
{
    [TestClass]
    public class CopyFolderTests
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
        public void MakeSureYouCannotCopyFolderToANonExistingFolder() // само по себе ограничение упрощает мне жизнь. Так-то не следует пользователей грузить подобными ограничениями
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folderA = fileSystem.CreateFolder(@"\A");
            fileSystem.CreateFolder(@"\A\B");
            
            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                    {
                        var results = fileSystem.CopyFolder(folderA.FullPath, @"\Folder\ACopied"); //не хватает папки \Folder
                        Console.WriteLine(results.Count);
                    });
        }

        //Note: в качестве теста не оставляю, но и не удаляю. Написано - посмотреть, как оно будет выглядеть для пользователя.
//        [TestMethod]
//        public void MakeSureYouGetAnExceptionIfCopyingWhenSystemIsBeingDisposed()
//        {
//            VirtualFileSystem fileSystem =
//                VirtualFileSystemFactory.CreateASystemWithSeveralFilesUnderGivenFolder(@"\folder", 100);
//
//            var copyingIsAboutToStart = new ManualResetEvent(false);
//
//            var thread1 = new Thread(delegate()
//                                            {
//                                                copyingIsAboutToStart.WaitOne();
//
//                                                Thread.Sleep(100);
//
//                                                fileSystem.Dispose();
//                                            });
//
//            thread1.Start();
//
//            copyingIsAboutToStart.Set();
//
//            var copyingResults = fileSystem.CopyFolder(@"\folder", @"\folderCopy");
//        }

        [TestMethod]
        public void MakeSureYouCanCopyFolderToItself()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folderA = fileSystem.CreateFolder(@"\A");
            var folderB = fileSystem.CreateFolder(@"\A\B");
            var folderC = fileSystem.CreateFolder(@"\A\B\C");
            var fileA = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "AUnderB.txt"));
            var fileB = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "BUnderB.txt"));
            var fileC = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderC.FullPath, "C1.txt"));
            var fileD = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderC.FullPath, "C2.txt"));

            var copyingResults = fileSystem.CopyFolder(folderA.FullPath, @"\");

            foreach (FileTaskResult fileCopyTaskResult in copyingResults)
            {
                Assert.AreEqual(true, fileCopyTaskResult.ExecutedSuccessfully);
            }
        }

        [TestMethod]
        public void MakeSureLockedFilesAndAlreadyExistingFilesAreNotBeingCopied()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folderA = fileSystem.CreateFolder(@"\A");
            var folderB = fileSystem.CreateFolder(@"\A\B");
            var folderC = fileSystem.CreateFolder(@"\A\B\C");
            var fileA = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "AUnderB.txt"));
            var fileB = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "BUnderB.txt"));
            var fileC = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderC.FullPath, "C1.txt"));
            var fileD = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderC.FullPath, "C2.txt"));

            fileSystem.CreateFolder(@"\ABRA");
            fileSystem.CreateFolder(@"\ABRA\B");
            fileSystem.CreateFile(@"\ABRA\B\BUnderB.txt");

            using (fileSystem.OpenFileForWriting(fileA.FullPath))
            {
                var copyingResults = fileSystem.CopyFolder(folderA.FullPath, @"\ABRA");

                foreach (FileTaskResult fileCopyTaskResult in copyingResults)
                {
                    if (fileCopyTaskResult.SourceFile.FullPath.Equals(fileA.FullPath) || (fileCopyTaskResult.SourceFile.FullPath.Equals(fileB.FullPath)))
                    {
                        Assert.AreEqual(false, fileCopyTaskResult.ExecutedSuccessfully);
                    }
                    else
                    {
                        Assert.AreEqual(true, fileCopyTaskResult.ExecutedSuccessfully);
                    }
                }
            }
        }

        [TestMethod]
        public void MakeSureCopyingReallyMimicsTheStructureOfSourceFolderAndItsSubfolders()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folderA = fileSystem.CreateFolder(@"\A");
            var folderB = fileSystem.CreateFolder(@"\A\B");
            var folderC = fileSystem.CreateFolder(@"\A\B\C");
            var fileA = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "AUnderB.txt"));
            var fileB = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderB.FullPath, "BUnderB.txt"));
            var fileC = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderC.FullPath, "C1.txt"));
            var fileD = fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(folderC.FullPath, "C2.txt"));

            var copyingResults = fileSystem.CopyFolder(folderA.FullPath, @"\ACopied");

            CollectionAssert.AreEquivalent(new[] { folderA.Name, "ACopied" }, fileSystem.GetNamesOfAllFoldersFrom(VirtualFileSystem.Root));
            CollectionAssert.AreEquivalent(new[] { folderB.Name }, fileSystem.GetNamesOfAllFoldersFrom(@"\ACopied"));
            CollectionAssert.AreEquivalent(new[] { fileA.Name, fileB.Name }, fileSystem.GetNamesOfAllFilesFrom(@"\ACopied\B"));
            CollectionAssert.AreEquivalent(new[] { folderC.Name }, fileSystem.GetNamesOfAllFoldersFrom(@"\ACopied\B"));
            CollectionAssert.AreEquivalent(new[] { fileC.Name, fileD.Name }, fileSystem.GetNamesOfAllFilesFrom(@"\ACopied\B\c"));
        }
    }
}