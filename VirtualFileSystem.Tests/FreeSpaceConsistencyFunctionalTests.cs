using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class FreeSpaceConsistencyFunctionalTests
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
        public void TryCreatingAndThenRemovingABunchOfFilesCheckingFreeSpace()
        {
            Stream stream;
            VirtualFileSystem fileSystem = TestCollaboratorsFactory.CreateFileSystem(out stream);

            int freeSpaceBeforeAnything = fileSystem.FreeSpaceInBytes;

            int numberOfItemsToCreate = 50;

            var files = new List<string>();
            var folders = new List<string>();

            for (int i = 0; i < numberOfItemsToCreate; i++)
            {
                var fileName = Guid.NewGuid().ToString("N");

                files.Add(fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(VirtualFileSystem.Root, fileName)).FullPath);
            }

            for (int i = 0; i < numberOfItemsToCreate; i++)
            {
                var folderName = Guid.NewGuid().ToString("N");

                folders.Add(fileSystem.CreateFolder(fileSystem.PathBuilder.CombinePaths(VirtualFileSystem.Root, folderName)).FullPath);
            }

            int freeSpaceAsLastReported = fileSystem.FreeSpaceInBytes;

            fileSystem = TestCollaboratorsFactory.CreateFileSystemFromExistingStream(stream);

            Assert.AreEqual(freeSpaceAsLastReported, fileSystem.FreeSpaceInBytes);

            foreach (string filePath in files)
            {
                fileSystem.DeleteFile(filePath);
            }

            foreach (string folderPath in folders)
            {
                fileSystem.DeleteFolder(folderPath);
            }

            Assert.AreEqual(freeSpaceBeforeAnything, fileSystem.FreeSpaceInBytes);
        }

        [TestMethod]
        public void MakeSureDeletingAFileFreesSpaceForSure() // был такой баг.
        {
            var collaborators = TestCollaboratorsFactory.CreateAllCollaborators(50000, false);
            VirtualFileSystem fileSystem = collaborators.VirtualFileSystem;

            int freeSpaceBeforeAnything = fileSystem.FreeSpaceInBytes;

            byte[] interestingData = ByteBufferFactory.BuildSomeGuidsIntoByteArray(10000);

            using (var dataStream = fileSystem.CreateAndOpenFileForWriting(@"\hey"))
            {
                for (int i = 0; i < 50; i++)
                {
                    dataStream.Write(interestingData, 0, interestingData.Length);
                }
            }

            fileSystem.DeleteFile(@"\hey");

            Assert.AreEqual(freeSpaceBeforeAnything, fileSystem.FreeSpaceInBytes);

            fileSystem = TestCollaboratorsFactory.CreateFileSystemFromExistingStream(collaborators.Stream);

            Assert.AreEqual(freeSpaceBeforeAnything, fileSystem.FreeSpaceInBytes);
        }
    }
}