using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using VirtualFileSystem.ContentsEnumerators;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class FolderContentsEnumeratorTests
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
        public void MakeSureEnumeratorUnregistersItselWhenLeftUndisposed()
        {
            var mockRepository = new MockRepository();

            IFolderEnumeratorRegistry registryMock = mockRepository.DynamicMock<IFolderEnumeratorRegistry>();
            IFilesAndFoldersProvider fileProviderStub = mockRepository.Stub<IFilesAndFoldersProvider>();

            var folderInfo = new FolderInfo(new FolderNode("dsf", Guid.NewGuid(), 1, 2, new DataStreamDefinition(12, 0), new DataStreamDefinition(11, 0), DateTime.UtcNow, Guid.NewGuid()), "\\folder");
            var enumerator = new FolderContentsEnumerator(folderInfo, "*", registryMock, fileProviderStub);

            using(mockRepository.Unordered())
            {
                registryMock.Unregister(null);
                LastCall.IgnoreArguments().Repeat.Once();
            }

            mockRepository.ReplayAll();

            enumerator = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            mockRepository.VerifyAll();
        }

        [TestMethod]
        public void MakeSureEnumeratorThrowsAfterFolderItEnumeratesOrAnySubfolderUnderItChanges()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\folder1");
            fileSystem.CreateFolder(@"\folder22");

            fileSystem.CreateFolder(@"\folder22\subfolder1");

            fileSystem.CreateFile(@"\folder22\file1");
            fileSystem.CreateFile(@"\folder22\file2");

            fileSystem.CreateFile(@"\folder1\file1");

            IEnumerator<FileInfo> fileEnumerator = fileSystem.EnumerateFilesUnderFolder(VirtualFileSystem.Root, "*");

            fileEnumerator.MoveNext();

            fileSystem.CreateFile(@"\folder22\file22");

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidOperationException>(
                delegate
                    {
                        fileEnumerator.MoveNext();
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidOperationException>(
                delegate
                {
                    fileEnumerator.MoveNext();
                });
        }

        [TestMethod]
        public void MakeSureEnumeratorUnregistersItselfWhenDisposed()
        {
            var mockRepository = new MockRepository();

            IFolderEnumeratorRegistry registryMock = mockRepository.DynamicMock<IFolderEnumeratorRegistry>();
            IFilesAndFoldersProvider fileProviderStub = mockRepository.Stub<IFilesAndFoldersProvider>();

            var folderInfo = new FolderInfo(new FolderNode("dsf", Guid.NewGuid(), 1, 2, new DataStreamDefinition(12, 0), new DataStreamDefinition(11, 0), DateTime.UtcNow, Guid.NewGuid()), "\\folder");
            var enumerator = new FolderContentsEnumerator(folderInfo, "*", registryMock, fileProviderStub);

            using (mockRepository.Unordered())
            {
                registryMock.Unregister(null);
                LastCall.IgnoreArguments().Repeat.Once();
            }

            mockRepository.ReplayAll();

            enumerator.Dispose();

            mockRepository.VerifyAll();
        }

        // Этот тест валит процесс хоста, если не проходит. Сам по себе факт не слишком хорош, но я оставляю это как есть (с ходу не могу сказать, как переписать его лучше).
        [TestMethod]
        public void MakeSureThatIfEnumeratorsConstructorThrowsItsFinalizerDoesNotExecuteAndTearDownTheProcess()
        {
            try
            {
                new FolderContentsEnumerator(null, null, null, null);
            }
            catch (ArgumentException)
            {
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [TestMethod]
        public void MakeSureEnumeratorDoesGiveOutAllFilesWhenSetAccordinglyDataSuite1()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\folder1");
            fileSystem.CreateFolder(@"\folder22");

            fileSystem.CreateFolder(@"\folder22\subfolder1");

            fileSystem.CreateFile(@"\folder22\file1");
            fileSystem.CreateFile(@"\folder22\file2");

            fileSystem.CreateFile(@"\folder1\file1");

            IEnumerator<FileInfo> fileEnumerator = fileSystem.EnumerateFilesUnderFolder(VirtualFileSystem.Root, "*");

            var allFiles = new List<string>();

            while (fileEnumerator.MoveNext())
            {
                allFiles.Add(fileEnumerator.Current.FullPath);
            }

            CollectionAssert.AreEquivalent(new[] { @"\folder22\file1", @"\folder22\file2", @"\folder1\file1" }, allFiles);

            allFiles.Clear();

            fileEnumerator = fileSystem.EnumerateFilesUnderFolder(@"\folder22", "file?");

            while (fileEnumerator.MoveNext())
            {
                allFiles.Add(fileEnumerator.Current.FullPath);
            }

            CollectionAssert.AreEquivalent(new[] { @"\folder22\file1", @"\folder22\file2" }, allFiles);
        }

        [TestMethod]
        public void MakeSureEnumeratorDoesGiveOutAllFilesWhenSetAccordinglyDataSuite2()
        {
            VirtualFileSystem fileSystem = VirtualFileSystemFactory.CreateDefaultFileSystem();

            fileSystem.CreateFolder(@"\folder1");
            fileSystem.CreateFolder(@"\folder22");

            fileSystem.CreateFolder(@"\folder22\subfolder1");

            fileSystem.CreateFile(@"\folder22\file1");
            fileSystem.CreateFile(@"\folder22\file2");

            fileSystem.CreateFile(@"\folder1\file1");

            IEnumerator<FileInfo> fileEnumerator = fileSystem.EnumerateFilesUnderFolder(VirtualFileSystem.Root, "??????????");

            var allFiles = new List<string>();

            while (fileEnumerator.MoveNext())
            {
                allFiles.Add(fileEnumerator.Current.FullPath);
            }

            CollectionAssert.AreEquivalent(new string[] {}, allFiles);
        }
    }
}