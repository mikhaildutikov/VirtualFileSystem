using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.DiskBlockEnumeration;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class DiskBlockTests
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
        public void TryWriteReadCycleForABuffer()
        {
            var virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            DiskBlock block = new DiskBlock(virtualDisk, 10, 0, 0);

            for (int i = 0; i < 2048 / 4; i++)
            {
                byte[] fourByteBlock = new byte[4];
                MemoryStream writerStream = new MemoryStream(fourByteBlock);
                BinaryWriter writer = new BinaryWriter(writerStream);

                writer.Write(i);

                block.WriteBytes(fourByteBlock, 0, fourByteBlock.Length);
            }

            byte[] allBytes = block.ReadAll();

            MemoryStream readerStream = new MemoryStream(allBytes);
            BinaryReader reader = new BinaryReader(readerStream);

            for (int i = 0; i < 2048 / 4; i++)
            {
                Assert.AreEqual(i, reader.ReadInt32());
            }

            Assert.IsFalse(block.CanAcceptBytesAtCurrentPosition);
        }

        [TestMethod]
        public void MakeSureThePositionChangesAsYouAreWritingToTheBlock()
        {
            var virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            DiskBlock block = new DiskBlock(virtualDisk, 10, 0, 0);

            Assert.AreEqual(0, block.Position);

            block.WriteBytes(new byte[15], 0, 15);

            Assert.AreEqual(15, block.Position); // после последнего записанного байта.

            block.WriteBytes(new byte[15], 9, 6);

            Assert.AreEqual(21, block.Position); // после последнего записанного байта.
        }

        [TestMethod]
        public void MakeSureTheBlockChecksArgumentsBeforeWritingAnything()
        {
            var virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            DiskBlock block = new DiskBlock(virtualDisk, 10, 0, 0);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                    {
                        block.WriteBytes(null, 10, 11);
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    block.WriteBytes(new byte[2], 10, 11);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    block.WriteBytes(new byte[2], -10, 11);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    block.WriteBytes(new byte[2], 10, -1);
                });
        }

        [TestMethod]
        public void MakeSureBlockConstructionIsPossibleOnlyWithSaneArguments()
        {
            var virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            DiskBlock block = new DiskBlock(virtualDisk, 10, 0, 0);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    new DiskBlock(virtualDisk, 100000000, 0, 0);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    new DiskBlock(null, 10, 0, 0);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    new DiskBlock(virtualDisk, -2, 0, 0);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    new DiskBlock(virtualDisk, 15, virtualDisk.BlockSizeInBytes * 5, 0);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    new DiskBlock(virtualDisk, 15, virtualDisk.BlockSizeInBytes, 56789);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    new DiskBlock(virtualDisk, 15, virtualDisk.BlockSizeInBytes, -56789);
                });
        }
    }
}