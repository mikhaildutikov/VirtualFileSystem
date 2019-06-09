using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Streaming;
using VirtualFileSystem.Tests.TestFactories;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Tests
{
    class TestDataStreamStructureBuilder : DataStreamStructureBuilder
    {
        public TestDataStreamStructureBuilder(DataStreamDefinition dataStreamDefinition, IVirtualDisk disk, IFreeBlockManager freeBlockManager, IFileSystemNodeStorage fileSystemNodeStorage, Node governingNode, AddressingSystemParameters addressingSystemParameters)
            : base(dataStreamDefinition, disk, freeBlockManager, fileSystemNodeStorage, governingNode, addressingSystemParameters)
        {
        }

        public new IntegerListConstrained DoubleIndirectBlocks
        {
            get { return base.DoubleIndirectBlocks; }
        }
    }

    [TestClass]
    public class DataStreamStructureBuilderTests
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
        public void MakeSureStructureBuilderAllocatesExactlyTheNumberOfBlocksNeededToStoreTheData()
        {
            var testCollaborators = TestCollaboratorsFactory.CreateCollaboratorsForTestingDataStreamStructureBuilder();

            MockRepository mocks = new MockRepository();

            IFreeBlockManager freeBlockManager = mocks.DynamicMock<IFreeBlockManager>();

            IFileSystemNodeStorage nodeStorageStub = mocks.Stub<IFileSystemNodeStorage>();

            int freeBlockCounter = 25;
            int numberOfFreeBlocksAllocated = 0;

            using (mocks.Unordered())
            {
                Expect.Call(freeBlockManager.AcquireFreeBlocks(1)).IgnoreArguments().Do(
                    new Func<int, ReadOnlyCollection<int>>(delegate(int i)
                                                               {
                                                                   var blocks = new List<int>();

                                                                   for (int j = 0; j < i; j++)
                                                                   {
                                                                       blocks.Add(freeBlockCounter);
                                                                       freeBlockCounter++;
                                                                   }

                                                                   numberOfFreeBlocksAllocated += i;

                                                                   return blocks.AsReadOnly();
                                                               }));

                Expect.Call(freeBlockManager.AcquireFreeBlock()).IgnoreArguments().Do(
                    new Func<int>(delegate
                                      {
                                          freeBlockCounter++;
                                          numberOfFreeBlocksAllocated ++;
                                          return freeBlockCounter;
                                      }));
            }

            mocks.ReplayAll();

            var dataStreamStructureBuilder =
                new TestDataStreamStructureBuilder(
                    testCollaborators.FileNode.ResolvedNode.FileContentsStreamDefinition,
                    testCollaborators.Disk,
                    freeBlockManager,
                    nodeStorageStub,
                    testCollaborators.FileNode.ResolvedNode,
                    AddressingSystemParameters.Default);

            Assert.AreEqual(0, dataStreamStructureBuilder.DoubleIndirectBlocks.Count);

            dataStreamStructureBuilder.SetSize(1000);

            Assert.AreEqual(1000, dataStreamStructureBuilder.CurrentSize);

            var diskStructureEnumerator = dataStreamStructureBuilder.CreateEnumerator();

            Assert.AreEqual(2, numberOfFreeBlocksAllocated); // один для данных, другой - под single indirect block

            Assert.AreEqual(1, diskStructureEnumerator.Count); // количество дисковых блоков = 1
            Assert.AreEqual(1, dataStreamStructureBuilder.DoubleIndirectBlocks.Count);

            dataStreamStructureBuilder.SetSize(1500);
            Assert.AreEqual(1500, dataStreamStructureBuilder.CurrentSize);

            dataStreamStructureBuilder.SetSize(2000);

            Assert.AreEqual(2000, dataStreamStructureBuilder.CurrentSize);

            diskStructureEnumerator = dataStreamStructureBuilder.CreateEnumerator();

            Assert.AreEqual(2, numberOfFreeBlocksAllocated);

            Assert.AreEqual(1, diskStructureEnumerator.Count); // количество дисковых блоков = 1
            Assert.AreEqual(1, dataStreamStructureBuilder.DoubleIndirectBlocks.Count);

            dataStreamStructureBuilder.SetSize(2048);
            Assert.AreEqual(2048, dataStreamStructureBuilder.CurrentSize);

            diskStructureEnumerator = dataStreamStructureBuilder.CreateEnumerator();

            Assert.AreEqual(2, numberOfFreeBlocksAllocated); // оверхед системы адресации - еще один блок.

            Assert.AreEqual(1, diskStructureEnumerator.Count); // количество дисковых блоков = 2
            Assert.AreEqual(1, dataStreamStructureBuilder.DoubleIndirectBlocks.Count);

            dataStreamStructureBuilder.SetSize(testCollaborators.Disk.BlockSizeInBytes * (testCollaborators.Disk.BlockSizeInBytes / 4) * 3);

            Assert.AreEqual((testCollaborators.Disk.BlockSizeInBytes / 4) * 3 + 3, numberOfFreeBlocksAllocated);

            dataStreamStructureBuilder.SetSize(testCollaborators.Disk.BlockSizeInBytes * (testCollaborators.Disk.BlockSizeInBytes / 4) * 5);
        }
    }
}