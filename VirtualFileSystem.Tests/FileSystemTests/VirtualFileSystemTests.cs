using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class VirtualFileSystemTests
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
        public void MakeSureYouCannotCreateTwoFilesWithSameNamesUnderOneFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFile(@"\file1");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileAlreadyExistsException>(
                delegate()
                    {
                        fileSystem.CreateFile(@"\file1");
                    });
        }

        [TestMethod]
        public void MakeSureYouCanCreateFilesAndFolderWithMaximumLengthSetForTheirNames()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFile(@"\" + new string('A', (int)Constants.FileAndFolderMaximumNameLength));
            fileSystem.CreateFolder(@"\" + new string('A', (int)Constants.FileAndFolderMaximumNameLength));
        }

        [TestMethod]
        public void CreateAHundredFilesUnderOneFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var namesOfFiles = new List<string>();

            ReadOnlyCollection<FileInfo> rootFolderFiles = fileSystem.GetAllFilesFrom(@"\");

            Assert.AreEqual(0, rootFolderFiles.Count);

            for (int i = 0; i < 100; i++)
            {
                string fileName = Guid.NewGuid().ToString("N");

                namesOfFiles.Add(fileName);

                fileSystem.CreateFile(@"\" + fileName);
            }

            var namesOfFilesUnderRoot = fileSystem.GetNamesOfAllFilesFrom(VirtualFileSystem.Root);

            CollectionAssert.AreEquivalent(namesOfFiles, namesOfFilesUnderRoot);
        }

        [TestMethod]
        public void MakeSureFileSystemDoesNotDieWhenAbsolutelyFull() // был такой баг
        {
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateAllCollaborators(15000, false).VirtualFileSystem;

            using (var stream = fileSystem.CreateAndOpenFileForWriting(@"\huge"))
            {
                stream.SetLength((fileSystem.FreeSpaceInBytes/8) * 7);
            }

            try
            {
                while (fileSystem.FreeSpaceInBytes != 0)
                {
                    string fileName = Guid.NewGuid().ToString("N");
                    fileSystem.CreateFile(@"\" + fileName);
                }
            }
            catch (Exception exception)
            {
                if (!(exception is InsufficientSpaceException)) // единственный приемлемый тип исключения в данном случае.
                {
                    throw;
                }
            }
        }

        [TestMethod]
        public void MakeSureCreatingAFileReallyEnlistsItInFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            Assert.AreEqual(0, fileSystem.GetAllFilesFrom(VirtualFileSystem.Root).Count);

            fileSystem.CreateFile(@"\file1");

            CollectionAssert.AreEqual(new[] { "file1" }, fileSystem.GetNamesOfAllFilesFrom(VirtualFileSystem.Root));
        }

        [TestMethod]
        public void MakeSureCreatingAFolderReallyEnlistsItInParentFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            Assert.AreEqual(0, fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root).Count);

            fileSystem.CreateFolder(@"\folder1");

            ReadOnlyCollection<FolderInfo> foldersUnderRoot = fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root);

            Assert.AreEqual(1, foldersUnderRoot.Count);
            Assert.AreEqual("folder1", foldersUnderRoot[0].Name);
        }

        [TestMethod]
        public void MakeSureFileSystemChangesPersistAfterItHasBeenReloaded()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            Assert.AreEqual(0, fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root).Count);

            CreateFilesAndFolders(fileSystem);

            fileSystem = TestCollaboratorsFactory.CreateFileSystemFromExistingStream(stream);

            fileSystem.CreateFile(@"\newFile");
            fileSystem.CreateFolder(@"\newFolder");
        }

        [TestMethod]
        public void MakeSureWeirdBugWithFoldersAndFilesWithDuplicatedNamesIsGone()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            Assert.AreEqual(0, fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root).Count);

            fileSystem.CreateFile(@"\hip");
            fileSystem.CreateFolder(@"\hop");
            fileSystem.CreateFile(@"\chop");
            fileSystem.CreateFolder(@"\chop");

            fileSystem = TestCollaboratorsFactory.CreateFileSystemFromExistingStream(stream);

            fileSystem.CreateFile(@"\chopper");
            fileSystem.CreateFolder(@"\chopper");

            CollectionAssert.AreEquivalent(new[] { "hip", "chop", "chopper" }, fileSystem.GetNamesOfAllFilesFrom(VirtualFileSystem.Root));

            ReadOnlyCollection<FolderInfo> foldersUnderRoot = fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root);

            CollectionAssert.AreEquivalent(new[] { "hop", "chop", "chopper" }, foldersUnderRoot.Select(folder => folder.Name).ToList());
        }

        [TestMethod]
        public void MakeSureDeletingFilesAndFoldersFreesExactlyTheSpaceTheyOccupied()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\folder");

            int amountOfFreeSpaceBeforeStuffHasBeenCreated = fileSystem.FreeSpaceInBytes;

            var folderInfo = fileSystem.CreateFolder(@"\folder23");

            var fileInfo = fileSystem.CreateFile(@"\folder\testFile.txt");

            using (var stream = fileSystem.OpenFileForWriting(fileInfo.FullPath))
            {
                stream.SetLength((fileSystem.FreeSpaceInBytes/4)*3);
            }

            Assert.IsTrue(fileSystem.FreeSpaceInBytes < amountOfFreeSpaceBeforeStuffHasBeenCreated);

            fileSystem.DeleteFile(fileInfo.FullPath);
            fileSystem.DeleteFolder(folderInfo.FullPath);

            Assert.AreEqual(amountOfFreeSpaceBeforeStuffHasBeenCreated, fileSystem.FreeSpaceInBytes);
        }

        [TestMethod]
        public void CreateASmallTreeOfFilesAndFolders()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            const string folder1Path = @"\folder1";
            const string folder2NodePath = @"\folder22";
            
            CreateFilesAndFolders(fileSystem);

            CollectionAssert.AreEquivalent(new[] { "AllWorkAndNoPlay", "MakeJackADullBoy" }, fileSystem.GetNamesOfAllFilesFrom(VirtualFileSystem.Root));

            CollectionAssert.AreEquivalent(new[] { "folder1", "folder22" }, fileSystem.GetNamesOfAllFoldersFrom(VirtualFileSystem.Root));

            ReadOnlyCollection<FolderInfo> foldersUnderRoot = fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root);

            CollectionAssert.AreEquivalent(new[] { folder1Path, folder2NodePath }, foldersUnderRoot.Select(folder => folder.FullPath).ToList());

            Assert.AreEqual(0, fileSystem.GetAllFoldersFrom(folder1Path).Count);            
            
            CollectionAssert.AreEquivalent(new[] { "file1" }, fileSystem.GetNamesOfAllFilesFrom(folder1Path));

            CollectionAssert.AreEquivalent(new[] { "subfolder1" }, fileSystem.GetNamesOfAllFoldersFrom(folder2NodePath));

            CollectionAssert.AreEquivalent(new[] { "file1", "file2" }, fileSystem.GetNamesOfAllFilesFrom(folder2NodePath));
        }

        private void CreateFilesAndFolders(VirtualFileSystem fileSystem)
        {
            fileSystem.CreateFolder(@"\folder1");
            fileSystem.CreateFolder(@"\folder22");

            fileSystem.CreateFolder(@"\folder22\subfolder1");

            fileSystem.CreateFile(@"\folder22\file1");
            fileSystem.CreateFile(@"\folder22\file2");

            fileSystem.CreateFile(@"\folder1\file1");

            fileSystem.CreateFile(@"\AllWorkAndNoPlay");
            fileSystem.CreateFile(@"\MakeJackADullBoy");
        }

        [TestMethod]
        public void TryRemovingAFileFromFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folder1Node = fileSystem.CreateFolder(@"\folder1");

            fileSystem.CreateFile(@"\folder1\file1");
            var file2Info = fileSystem.CreateFile(@"\folder1\file2");
            fileSystem.CreateFile(@"\folder1\file3");

            CollectionAssert.AreEquivalent(new[] { "file1", "file2", "file3" }, fileSystem.GetNamesOfAllFilesFrom(folder1Node.FullPath));

            fileSystem.DeleteFile(file2Info.FullPath);

            fileSystem.CreateFile(@"\hey");
            fileSystem.CreateFile(@"\heyThere");
            fileSystem.CreateFile(@"\GoodMorningIndeed");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileNotFoundException>(
                delegate
                    {
                        fileSystem.DeleteFile(VirtualFileSystem.Root + Guid.NewGuid().ToString("N"));
                    });
        }

        [TestMethod]
        public void MakeSureYouCannotDoAThingWithAFileThatIsLocked()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            FolderInfo folder1Node = fileSystem.CreateFolder(@"\folder1");
            FileInfo newFile = fileSystem.CreateFile(@"\folder1\file1");

            using (DataStreamReadable dataStream = fileSystem.OpenFileForReading(newFile.FullPath))
            {
                ExceptionAssert.MakeSureExceptionIsRaisedBy<FileLockedException>(
                    delegate
                    {
                        fileSystem.OpenFileForWriting(newFile.FullPath);
                    });

                ExceptionAssert.MakeSureExceptionIsRaisedBy<FileLockedException>(
                    delegate
                    {
                        fileSystem.DeleteFile(newFile.FullPath);
                    });

                ExceptionAssert.MakeSureExceptionIsRaisedBy<FileLockedException>(
                    delegate
                    {
                        fileSystem.RenameFile(newFile.FullPath, "newFile.txt");
                    });

                ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderLockedException>(
                    delegate
                    {
                        fileSystem.RenameFolder(folder1Node.FullPath, "Untitled");
                    });
            }
        }

        [TestMethod]
        public void MakeSureYouCannotOpenNonExistingFileForReading()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileNotFoundException>(
                delegate
                    {
                        fileSystem.OpenFileForReading("\\folder1\\folder2\\myFile.lol");
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileNotFoundException>(
                delegate
                {
                    fileSystem.OpenFileForReading("\\folder1\\folder2\\why not?");
                });
        }

        [TestMethod]
        public void MakeSureYouCannotOpenFilePassingNullOrEmptyStringAsFilePath()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    fileSystem.OpenFileForReading(null);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    fileSystem.OpenFileForReading(String.Empty);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    fileSystem.OpenFileForWriting(null);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    fileSystem.OpenFileForWriting(String.Empty);
                });
        }


        [TestMethod]
        public void MakeSureOpeningAFileProhibitsRenamesUpToTheRoot()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            FolderInfo folder1Node = fileSystem.CreateFolder(@"\folder1");
            FolderInfo folder2Node = fileSystem.CreateFolder(@"\folder1\sub1");
            FolderInfo folder3Node = fileSystem.CreateFolder(@"\folder1\sub1\sub2");
            FolderInfo folder4Node = fileSystem.CreateFolder(@"\folder1\sub1\sub2\sub3");
            FolderInfo folder5Node = fileSystem.CreateFolder(@"\folder1\sub1\sub2\sub3\sub4");

            var folders = new List<FolderInfo>{folder1Node, folder2Node, folder3Node, folder4Node, folder5Node};

            FileInfo newFile = fileSystem.CreateFile(folder5Node.FullPath + VirtualFileSystem.DirectorySeparatorChar + "file.txt");

            using (fileSystem.OpenFileForReading(newFile.FullPath))
            {
                MakeSureRenamingFoldersFails(fileSystem, folders);
            }

            using (fileSystem.OpenFileForWriting(newFile.FullPath))
            {
                MakeSureRenamingFoldersFails(fileSystem, folders);
            }
        }

        [TestMethod]
        public void MakeSureWritingToFilePersistsData()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\folder1");

            var capacitiesOfTestArrays = new List<int>{255, 12560, 7777, 1111, 2048, 2049, 2047, 10000000};

            foreach (int testArrayCapacity in capacitiesOfTestArrays)
            {
                var testArray = new byte[testArrayCapacity];
                var random = new Random((int)DateTime.Now.Ticks);
                random.NextBytes(testArray);

                FileInfo fileInfo = fileSystem.CreateFile(@"\folder1\file1.txt");

                using (var dataStream = fileSystem.OpenFileForWriting(fileInfo.FullPath))
                {
                    dataStream.Write(testArray, 0, testArray.Length);
                }

                var bytesRead = new byte[testArrayCapacity];

                using (var dataStream = fileSystem.OpenFileForReading(fileInfo.FullPath))
                {
                    dataStream.Read(bytesRead, 0, dataStream.Length);
                }

                CollectionAssert.AreEqual(testArray, bytesRead);

                fileSystem.DeleteFile(fileInfo.FullPath);
            }
        }

        [TestMethod]
        public void MakeSureOpeningAFileStillAllowsCreatingNewFoldersAndFiles()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            FolderInfo folder1Node = fileSystem.CreateFolder(@"\folder1");
            FolderInfo folder2Node = fileSystem.CreateFolder(@"\folder1\sub1");
            FolderInfo folder3Node = fileSystem.CreateFolder(@"\folder1\sub1\sub2");
            FolderInfo folder4Node = fileSystem.CreateFolder(@"\folder1\sub1\sub2\sub3");
            FolderInfo folder5Node = fileSystem.CreateFolder(@"\folder1\sub1\sub2\sub3\sub4");

            List<FolderInfo> folders = new List<FolderInfo> { folder1Node, folder2Node, folder3Node, folder4Node, folder5Node };

            FileInfo newFile = fileSystem.CreateFile(folder5Node.FullPath + VirtualFileSystem.DirectorySeparatorChar + "file.txt");

            using (fileSystem.OpenFileForReading(newFile.FullPath))
            {
                fileSystem.CreateFile(folder3Node.FullPath + VirtualFileSystem.DirectorySeparatorChar + "file.txt");
                fileSystem.CreateFolder(@"\folderN");
            }

            using (fileSystem.OpenFileForWriting(newFile.FullPath))
            {
                fileSystem.CreateFile(folder4Node.FullPath + VirtualFileSystem.DirectorySeparatorChar + "file.txt");
                fileSystem.CreateFolder(@"\folder1\sub1\folderN");
            }
        }

        private static void MakeSureRenamingFoldersFails(VirtualFileSystem fileSystem, IEnumerable<FolderInfo> folders)
        {
            foreach (FolderInfo folder in folders)
            {
                ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderLockedException>(
                    delegate
                        {
                            fileSystem.RenameFolder(folder.FullPath, "newFolder");
                        });
            }
        }

        [TestMethod]
        public void MakeSureYouReadSameStuffFromTwoStreamsLinkingToTheSameFile()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            FolderInfo folder1Node = fileSystem.CreateFolder(@"\folder1");
            FileInfo newFile = fileSystem.CreateFile(@"\folder1\file1");

            var relativelyInterestingData = new List<byte>();

            for (int i = 0; i < 1000; i++)
            {
                relativelyInterestingData.AddRange(Guid.NewGuid().ToByteArray());
            }

            using (DataStreamReadableWritable dataStream = fileSystem.OpenFileForWriting(newFile.FullPath))
            {
                dataStream.Write(relativelyInterestingData.ToArray(), 0, relativelyInterestingData.Count);
            }

            DataStreamReadable dataStream1 = fileSystem.OpenFileForReading(newFile.FullPath);
            DataStreamReadable dataStream2 = fileSystem.OpenFileForReading(newFile.FullPath);

            Exception anyThreadException = null;

            var bytesReadByThreadOne = new List<byte>();

            var thread1 = new Thread(delegate()
            {
                try
                {
                    var buffer = new byte[1];

                    while (dataStream1.Read(buffer, 0, 1) == 1)
                    {
                        bytesReadByThreadOne.Add(buffer[0]);
                    }
                }
                catch (Exception exception)
                {
                    anyThreadException = new InvalidOperationException("", exception);
                }
            });

            var bytesReadByThreadTwo = new List<byte>();

            var thread2 = new Thread(delegate()
            {
                try
                {
                    while (dataStream2.Position != dataStream2.Length)
                    {
                        var buffer = new byte[1];
                        dataStream2.Read(buffer, 0, 1);
                        bytesReadByThreadTwo.Add(buffer[0]);
                    }
                }
                catch (Exception exception)
                {
                    anyThreadException = new InvalidOperationException("Возникло исключение в одном из тестовых потоков. Детали и стэк вызова - в обернутом исключении.", exception);
                }
            });

            thread1.Start();
            thread2.Start();

            thread2.Join();
            thread1.Join();

            if (anyThreadException != null)
            {
                throw anyThreadException;
            }

            CollectionAssert.AreEqual(relativelyInterestingData, bytesReadByThreadOne);
            CollectionAssert.AreEqual(relativelyInterestingData, bytesReadByThreadTwo);
        }

        [TestMethod]
        public void TryRenamingAFile()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folder1Node = fileSystem.CreateFolder(@"\folder1");

            fileSystem.CreateFile(@"\folder1\file1");
            var file2Info = fileSystem.CreateFile(@"\folder1\file2");
            fileSystem.CreateFile(@"\folder1\file3");

            CollectionAssert.AreEquivalent(new[] { "file1", "file2", "file3" }, fileSystem.GetNamesOfAllFilesFrom(folder1Node.FullPath));

            fileSystem.RenameFile(@"\folder1\file1", "file150");

            CollectionAssert.AreEquivalent(new[] { "file150", "file2", "file3" }, fileSystem.GetNamesOfAllFilesFrom(folder1Node.FullPath));

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileNotFoundException>(
                delegate
                {
                    fileSystem.RenameFile(@"\folder1\" + Guid.NewGuid().ToString("N"), "file150");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FileAlreadyExistsException>(
                delegate
                    {
                        fileSystem.RenameFile(@"\folder1\file150", "file2"); // файл с именем file2 уже есть в той папке.
                    });
        }

        [TestMethod]
        public void MakeSureRenamingFilesAndFoldersToSameNamesTheyHadDoesNotThrow()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folder1 = fileSystem.CreateFolder(@"\folder1");

            var fileInfo = fileSystem.CreateFile(@"\folder1\file1");
            
            fileSystem.RenameFile(fileInfo.FullPath, "file1");
            fileSystem.RenameFolder(folder1.FullPath, "folder1");
        }

        [TestMethod]
        public void MakeSureFileChangeTimeIsUpdatedWhenYouWriteToIt()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folderInfo = fileSystem.CreateFolder(@"\folder1");

            var fileInfo = fileSystem.CreateFile(@"\folder1\file1");

            Thread.Sleep(TimeSpan.FromSeconds(1));

            using (var stream = fileSystem.OpenFileForWriting(fileInfo.FullPath))
            {
                byte[] bytes = new byte[1];
                stream.Write(bytes, 0, bytes.Length);
            }

            var updatedFileInfo = fileSystem.GetAllFilesFrom(folderInfo.FullPath).Single();

            Assert.AreNotEqual(fileInfo.LastModificationTimeUtc, updatedFileInfo.LastModificationTimeUtc);
        }

        [TestMethod]
        public void TryRenamingAFolderSeveralTimes()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\folder1");
            fileSystem.CreateFolder(@"\folder2");
            fileSystem.CreateFolder(@"\folder3");

            CollectionAssert.AreEquivalent(new[] { "folder1", "folder2", "folder3" }, fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root).Select(folder => folder.Name).ToList());

            fileSystem.RenameFolder(@"\folder1", "0xFF");
            fileSystem.RenameFolder(@"\0xff", "DearJohn,");

            var folders = fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root);

            CollectionAssert.AreEquivalent(new[] { "DearJohn,", "folder2", "folder3" }, folders.Select(folder => folder.Name).ToList());

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                {
                    fileSystem.RenameFolder(@"\folder10", "file150");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderAlreadyExistsException>(
                delegate
                {
                    fileSystem.RenameFolder(@"\folder3", "dearjohn,");
                });
        }

        [TestMethod]
        public void TryRenamingAFileSeveralTimes()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            var folderInfo = fileSystem.CreateFolder(@"\folder1");

            var newFile1 = fileSystem.CreateFile(@"\folder1\file1");
            fileSystem.CreateFile(@"\folder1\file2");
            fileSystem.CreateFile(@"\folder1\file3");

            fileSystem.RenameFile(newFile1.FullPath, "fileX");
            fileSystem.RenameFile(@"\folder1\fileX", "fileY");
            fileSystem.RenameFile(@"\folder1\fileY", "fileZ");

            CollectionAssert.AreEquivalent(new[] { "file2", "file3", "fileZ" }, fileSystem.GetNamesOfAllFilesFrom(folderInfo.FullPath));
        }

        [TestMethod]
        public void TryCreatingAFileProvidingNonValidNameForIt()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\folder1");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                    {
                        fileSystem.CreateFile(@"\folder1\*\\&*");
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    fileSystem.CreateFile(@"\&*");
                });
        }

        [TestMethod]
        public void TryCreatingAFileInNonExistingFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\folder1");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                {
                    fileSystem.CreateFile(@"\folder1\folder2\veryWell");
                });
        }

        [TestMethod]
        public void MakeSureYouCannotEnumerateFilesUnderNonExistingFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                    {
                        fileSystem.EnumerateFilesUnderFolder(@"\folder1\folder2", "*");
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotFoundException>(
                delegate
                {
                    fileSystem.EnumerateFilesUnderFolder(@"какой счет?", "*");
                });
        }

        [TestMethod]
        public void TryCreatingAFileProvidingNullOrEmptyStringAsFileName()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    fileSystem.CreateFile(null);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    fileSystem.CreateFile(String.Empty);
                });
        }

        [TestMethod]
        public void MakeSureRemovingANonEmptyFolderFails()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            const string folderPath = @"\folder1";
            fileSystem.CreateFolder(folderPath);

            fileSystem.CreateFile(@"\folder1\file1");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<FolderNotEmptyException>(
                delegate
                {
                    fileSystem.DeleteFolder(folderPath);
                });
        }

        [TestMethod]
        public void MakeSureRemovingRootFolderFails()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    fileSystem.DeleteFolder(VirtualFileSystem.Root);
                });
        }

        [TestMethod]
        public void MakeSureRenamingRootFolderFails()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    fileSystem.DeleteFolder(VirtualFileSystem.Root);
                });
        }

        [TestMethod]
        public void MakeSureRemovingAFolderUnlistsItFromParentFolder()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            Assert.AreEqual(0, fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root).Count);

            const string folderPath = @"\folder1";
            fileSystem.CreateFolder(folderPath);

            CollectionAssert.AreEquivalent(new []{"folder1"}, fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root).Select( folder => folder.Name).ToList());

            fileSystem.DeleteFolder(folderPath);

            Assert.AreEqual(0, fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root).Count);
        }

        [TestMethod]
        public void TryClearingOutABunchOfFilesAndFolderAfterReloadingFileSystem()
        {
            Stream stream;

            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            fileSystem.CreateFolder(@"\emails");
            fileSystem.CreateFolder(@"\system128");
            fileSystem.CreateFolder(@"\folder10dsjknfskdjfnkjsnfkjsnfkjskjfskjfsnkjkdjsfnsjkd");
            fileSystem.CreateFile(@"\hello1");
            fileSystem.CreateFile(@"\GoodWillHunting.dvd");
            fileSystem.CreateFile(@"\Casablanca.flv");

            fileSystem = TestCollaboratorsFactory.CreateFileSystemFromExistingStream(stream);

            CollectionAssert.AreEquivalent(new[] { "emails", "system128", "folder10dsjknfskdjfnkjsnfkjsnfkjskjfskjfsnkjkdjsfnsjkd" }, fileSystem.GetNamesOfAllFoldersFrom(VirtualFileSystem.Root));
            CollectionAssert.AreEquivalent(new[] { "hello1", "GoodWillHunting.dvd", "Casablanca.flv"}, fileSystem.GetNamesOfAllFilesFrom(VirtualFileSystem.Root));

            fileSystem.DeleteFolder(@"\emails");
            fileSystem.DeleteFile(@"\hello1");
            fileSystem.DeleteFolder(@"\system128");
            fileSystem.DeleteFolder(@"\folder10dsjknfskdjfnkjsnfkjsnfkjskjfskjfsnkjkdjsfnsjkd");
            fileSystem.DeleteFile(@"\GoodWillHunting.dvd");
            fileSystem.DeleteFile(@"\Casablanca.flv");

            Assert.AreEqual(0, fileSystem.GetAllFoldersFrom(VirtualFileSystem.Root).Count);
            Assert.AreEqual(0, fileSystem.GetAllFilesFrom(VirtualFileSystem.Root).Count);
        }
    }
}