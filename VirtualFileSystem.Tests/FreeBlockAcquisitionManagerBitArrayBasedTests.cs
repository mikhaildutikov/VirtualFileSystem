using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.FreeSpace;
using VirtualFileSystem.Tests.Helpers;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class FreeBlockAcquisitionManagerBitArrayBasedTests
    {
        private const int NumberOfTestBlocks = 25;
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
        public void MakeSureFreeBlockManagerTakesAllBlocksIntoConsideration()
        {
            var bitArray = new BitArray(NumberOfTestBlocks);

            var freeBlockManager =
                new FreeBlockManagerBitArrayBased(bitArray, 0, NumberOfTestBlocks);

            var expectedIndexes = new List<int>(Enumerable.Range(0, NumberOfTestBlocks));

            var actualIndexes = new List<int>();

            for (int i = 0; i < NumberOfTestBlocks; i++)
            {
                int blockIndex = freeBlockManager.AcquireFreeBlock();
                actualIndexes.Add(blockIndex);
                Assert.IsTrue(bitArray[blockIndex]);
            }

            CollectionAssert.AreEqual(expectedIndexes, actualIndexes);
        }

        [TestMethod]
        public void MakeSureFreeBlockManagerThrowsWhenThereAreNoBlocksToGiveAway()
        {
            var bitArray = new BitArray(NumberOfTestBlocks);

            var freeBlockManager =
                new FreeBlockManagerBitArrayBased(bitArray, 0, NumberOfTestBlocks);

            for (int i = 0; i < NumberOfTestBlocks; i++)
            {
                freeBlockManager.AcquireFreeBlock();
            }

            ExceptionAssert.MakeSureExceptionIsRaisedBy<NoFreeBlocksException>(
                delegate
                    {
                        freeBlockManager.AcquireFreeBlock();
                    });
        }

        [TestMethod]
        public void MakeSureFreeBlockManagerAppliesOffsetToIndexesOfBlockGivenAway()
        {
            var bitArray = new BitArray(NumberOfTestBlocks);

            var freeBlockManager = new FreeBlockManagerBitArrayBased(bitArray, 10, NumberOfTestBlocks);

            var expectedIndexes = new List<int>(Enumerable.Range(10, NumberOfTestBlocks));

            var actualIndexes = new List<int>();

            for (int i = 0; i < NumberOfTestBlocks; i++)
            {
                actualIndexes.Add(freeBlockManager.AcquireFreeBlock());
            }

            CollectionAssert.AreEqual(expectedIndexes, actualIndexes);
        }

        [TestMethod]
        public void MakeSureFreeBlockManagerChecksItsArgumentsForSanity()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                    {
                        new FreeBlockManagerBitArrayBased(null, 10, NumberOfTestBlocks);
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    new FreeBlockManagerBitArrayBased(new BitArray(55), -10, NumberOfTestBlocks);
                });
        }

        [TestMethod]
        public void MakeSureFailedAcquisitionOfABunchOfBlocksReleasesTheBlocks()
        {
            var bitArray = new BitArray(NumberOfTestBlocks);

            var freeBlockManager =
                new FreeBlockManagerBitArrayBased(bitArray, 0, NumberOfTestBlocks);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<NoFreeBlocksException>(
                delegate
                    {
                        freeBlockManager.AcquireFreeBlocks(27);
                    });

            var actualIndexes = new HashSet<int>();

            for (int i = 0; i < NumberOfTestBlocks; i++)
            {
                int blockIndex = freeBlockManager.AcquireFreeBlock();
                actualIndexes.Add(blockIndex);
            }

            Assert.AreEqual(NumberOfTestBlocks, actualIndexes.Count);
        }

        [TestMethod]
        public void TestAcquireReleaseRoundTrip()
        {
            var bitArray = new BitArray(NumberOfTestBlocks);

            var freeBlockManager = new FreeBlockManagerBitArrayBased(bitArray, 3, NumberOfTestBlocks);

            var acquiredBlocks = new HashSet<int>(freeBlockManager.AcquireFreeBlocks(NumberOfTestBlocks));

            Assert.AreEqual(NumberOfTestBlocks, acquiredBlocks.Count);

            foreach (int acquiredBlock in acquiredBlocks)
            {
                freeBlockManager.MarkBlockAsFree(acquiredBlock);
            }

            acquiredBlocks = new HashSet<int>(freeBlockManager.AcquireFreeBlocks(NumberOfTestBlocks));

            Assert.AreEqual(NumberOfTestBlocks, acquiredBlocks.Count);
        }

        [TestMethod]
        public void MakeSureReleasingABlockMakesThatBlockEligibleForFurtherAcquisition()
        {
            var bitArray = new BitArray(NumberOfTestBlocks);

            var freeBlockManager = new FreeBlockManagerBitArrayBased(bitArray, 3, NumberOfTestBlocks);

            freeBlockManager.AcquireFreeBlocks(NumberOfTestBlocks - 1);

            int lastBlock = freeBlockManager.AcquireFreeBlock();

            freeBlockManager.MarkBlockAsFree(lastBlock);

            Assert.AreEqual(lastBlock, freeBlockManager.AcquireFreeBlock());
        }
    }
}