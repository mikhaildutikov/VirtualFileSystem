using System.Collections;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Disk;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class FreeSpaceBitmapStoreTests
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
        public void MakeSureWriteReadCycleWorksOnFreeSpaceMap()
        {
            Stream stream = new FileStream(@"e:\diskTests.tst", FileMode.Create);
                // new MemoryStream();

            var disk = VirtualDisk.CreateFormattingTheStream(stream, 2048, 15000 * 2048);

            const int numberOfBlocksToMap = 150000;
            var mapBytes = new byte[numberOfBlocksToMap];

            var bitArray = new BitArray(mapBytes);
            bitArray.Length = numberOfBlocksToMap;

            var freeBlockManager = new FreeBlockManagerBitArrayBased(bitArray, 22, numberOfBlocksToMap);

            freeBlockManager.AcquireFreeBlocks(7000);
            freeBlockManager.AcquireFreeBlocks(24);

            var bitmapStore = new FreeSpaceBitmapStore(disk, VirtualDiskFormatter.FreeSpaceStartingBlockIndex);

            var bytes = new byte[SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(numberOfBlocksToMap, Constants.NumberOfBitsInByte)];

            bitArray.CopyTo(bytes, 0);
            bitmapStore.WriteMap(bytes, numberOfBlocksToMap);

            int bitmapSize;
            var bytesRead = bitmapStore.ReadMap(out bitmapSize);

            Assert.AreEqual(numberOfBlocksToMap, bitmapSize);
            CollectionAssert.AreEqual(bytes, bytesRead);
        }
    }
}