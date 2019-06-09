using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Disk;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;
using System.Runtime.Serialization.Formatters.Binary;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class VirtualDiskTests
    {
        private TestContext testContextInstance;
        
        [TestMethod]
        public void MakeSureVirtualDisksCorrectlyInitializeThemselvesFromSuitableStreams()
        {
            VirtualDiskWithItsStream virtualDiskConstructionResult = VirtualDiskTestFactory.ConstructDefaultTestDiskWithStream();

            var virtualDisk = virtualDiskConstructionResult.Disk;

            Assert.AreEqual(VirtualDiskTestFactory.DefaultDiskBlockSize, virtualDisk.BlockSizeInBytes);
            Assert.AreEqual(VirtualDiskTestFactory.DefaultDiskSize, virtualDisk.DiskSizeInBytes);

            VirtualDisk diskRecreatedFromTheSameStream = VirtualDisk.CreateFromStream(virtualDiskConstructionResult.BackingStream);

            Assert.AreEqual(virtualDisk.BlockSizeInBytes, diskRecreatedFromTheSameStream.BlockSizeInBytes);
            Assert.AreEqual(VirtualDiskTestFactory.DefaultDiskSize, diskRecreatedFromTheSameStream.DiskSizeInBytes);
        }

        [TestMethod]
        public void TryReadWriteCycleWithAllBlocksOnDisk()
        {
            VirtualDisk virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            for (int i = 0; i < virtualDisk.NumberOfBlocks; i++)
            {
                byte[] bytesToWrite = new byte[virtualDisk.BlockSizeInBytes];
                byte byteToPutInEveryPlaceInArray = (byte)i;

                for (int j = 0; j < virtualDisk.BlockSizeInBytes; j++)
			    {
                    bytesToWrite[j] = byteToPutInEveryPlaceInArray;
			    }

                virtualDisk.WriteBytesToBlock(i, bytesToWrite);
            }

            for (int i = 0; i < virtualDisk.NumberOfBlocks; i++)
            {
                byte[] bytesRead = virtualDisk.ReadAllBytesFromBlock(i);
                byte byteThatMustBeInEveryPlaceInArray = (byte)i;

                Assert.AreEqual(virtualDisk.BlockSizeInBytes, bytesRead.Length);

                for (int j = 0; j < virtualDisk.BlockSizeInBytes; j++)
                {
                    Assert.AreEqual(byteThatMustBeInEveryPlaceInArray, bytesRead[j]);
                }
            }
        }

//        [TestMethod]
//        public void TestContinuosWriteReadCycle()
//        {
//            VirtualDisk virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();
//
//            byte[] interestingData = ByteBufferFactory.BuildSomeGuidsIntoByteArray(1000);
//
//            var dataRead = new byte[interestingData.Length];
//
//            virtualDisk.WriteBytesContinuoslyStartingFromBlock(interestingData, 5, 0, 0);
//
//            int bytesToRead = dataRead.Length;
//            int readArrayPosition = 0;
//            int currentBlockIndex = 5;
//
//            while (bytesToRead != 0)
//            {
//                if (bytesToRead > virtualDisk.BlockSizeInBytes)
//                {
//                    byte[] buffer = virtualDisk.ReadBytesFromBlock(currentBlockIndex, 0, virtualDisk.BlockSizeInBytes);
//                    Array.Copy(buffer, 0, dataRead, readArrayPosition, buffer.Length);
//
//                    bytesToRead -= buffer.Length;
//                    readArrayPosition += buffer.Length;
//                }
//                else
//                {
//                    byte[] buffer = virtualDisk.ReadBytesFromBlock(currentBlockIndex, 0, bytesToRead);
//
//                    Array.Copy(buffer, 0, dataRead, readArrayPosition, buffer.Length);
//                    bytesToRead -= buffer.Length;
//                    readArrayPosition += buffer.Length;
//                }
//            }
//
//            CollectionAssert.AreEqual(interestingData, dataRead);
//        }

        [TestMethod]
        public void TryReadingFromNonExistingBlocksOnDisk()
        {
            VirtualDisk virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate()
                {
                    virtualDisk.ReadAllBytesFromBlock(int.MaxValue);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate()
                {
                    virtualDisk.ReadAllBytesFromBlock(virtualDisk.NumberOfBlocks);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate()
                {
                    virtualDisk.ReadAllBytesFromBlock(-3);
                });
        }

        [TestMethod]
        public void MakeSureYouCannotReadTooMuchDataFromDisk()
        {
            VirtualDisk virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate()
                {
                    virtualDisk.ReadBytesFromBlock(0, 0, virtualDisk.BlockSizeInBytes + 1);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate()
                {
                    virtualDisk.ReadBytesFromBlock(0, 3, virtualDisk.BlockSizeInBytes - 2);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate()
                {
                    virtualDisk.ReadBytesFromBlock(0, int.MaxValue, 0);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate()
                {
                    virtualDisk.ReadBytesFromBlock(0, 0, int.MaxValue);
                });
        }

        [TestMethod]
        public void TryWritingToNonExistingBlocksOnDisk()
        {
            VirtualDisk virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            byte[] bytesToWrite = new byte[100];

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate()
                {
                    virtualDisk.WriteBytesToBlock(-2, bytesToWrite);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate()
                {
                    virtualDisk.WriteBytesToBlock(virtualDisk.NumberOfBlocks, bytesToWrite);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate()
                {
                    virtualDisk.WriteBytesToBlock(int.MaxValue, bytesToWrite);
                });
        }

        [TestMethod]
        public void MakeSureYouCannotWriteTooMuchDataIntoDiskBlock()
        {
            VirtualDisk virtualDisk = VirtualDiskTestFactory.ConstructDefaultTestDisk();

            byte[] bytesToWrite = new byte[virtualDisk.BlockSizeInBytes * 2];

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate()
                {
                    virtualDisk.WriteBytesToBlock(0, bytesToWrite);
                });
        }

        [TestMethod]
        public void MakeSureYouCannotCreateVirtualDiskFromGarbage()
        {
            MemoryStream testStream = new MemoryStream();

            testStream.Write(new byte[2048], 0, 2048);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<VirtualDiskCreationFailedException>(delegate()
            {
                VirtualDisk.CreateFromStream(testStream);
            });
        }

        [TestMethod]
        public void MakeSureYouCanCreateVirtualDiskOnlyOfSizeAlignedWithDiskBlockSize()
        {
            MemoryStream testStream = new MemoryStream();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(delegate()
            {
                VirtualDisk.CreateFormattingTheStream(testStream, 2048, 77777);
            });
        }

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
    }
}