using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Streaming;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class DataStreamTests
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

        private void TryWritingAndReadingBackSpecifiedByteArrays(byte[] testArray)
        {
            DataStreamReadableWritable dataStream = TestCollaboratorsFactory.CreateEmptyDataStream();

            Assert.AreEqual(0, dataStream.Position);
            Assert.AreEqual(0, dataStream.Length);

            dataStream.Write(testArray, 0, testArray.Length);

            Assert.AreEqual(testArray.Length, dataStream.Position);
            Assert.AreEqual(testArray.Length, dataStream.Length);

            var bytesRead = new byte[testArray.Length];

            Assert.AreEqual(0, dataStream.Read(bytesRead, 0, testArray.Length)); // позиция в DataStream = testArray.Length; Прочитать ничего не получится.

            dataStream.SetPosition(0);

            dataStream.Read(bytesRead, 0, testArray.Length);

            CollectionAssert.AreEqual(testArray, bytesRead);
        }

        private static void FillArrayWithRelativelyInterestingData(byte[] arrayToFill)
        {
            Random random = new Random((int)DateTime.Now.Ticks);

            random.NextBytes(arrayToFill);
        }

        [TestMethod]
        public void TryWritingToDataStreamAndThenReadingWhatWasWritten()
        {
            var dimensionsOfArraysToTest = new[] {5555, 33, 73487, 2048, 4096, 1, 2047, 2049, 512, 2048 * 1000, 2048 * 1023};

            foreach (int dimension in dimensionsOfArraysToTest)
            {
                var array = new byte[dimension];

                FillArrayWithRelativelyInterestingData(array);

                this.TryWritingAndReadingBackSpecifiedByteArrays(array);
            }
        }

        // Этот тест валит процесс хоста, если не проходит. Сам по себе факт не слишком хорош, но я оставляю это как есть (с ходу не могу сказать, как переписать его лучше).
        [TestMethod]
        public void MakeSureThatIfDataStreamsConstructorThrowsItsFinalizerDoesNotExecuteAndTearDownTheProcess()
        {
            try
            {
                new DataStream(null, 14, null, Guid.NewGuid());
            }
            catch (ArgumentException)
            {
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [TestMethod]
        public void TryExpandingTheStreamViaSetLengthAndThenWritingReadingFromIt()
        {
            DataStreamReadableWritable dataStream = TestCollaboratorsFactory.CreateEmptyDataStream();

            const int testArrayLength = 500000;

            dataStream.SetLength(testArrayLength);

            Assert.AreEqual(testArrayLength, dataStream.Length);
            Assert.AreEqual(0, dataStream.Position);

            const int halfTestArray = testArrayLength / 2; //на половине пути запишем что-нибудь интересное.

            dataStream.SetPosition(halfTestArray);

            var bytesToWrite = new byte[halfTestArray];

            for (int i = 0; i < bytesToWrite.Length; i++)
            {
                bytesToWrite[i] = 231;
            }

            dataStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            dataStream.SetPosition(halfTestArray);

            var bytesRead = new byte[bytesToWrite.Length];

            dataStream.Read(bytesRead, 0, bytesRead.Length);

            MakeSureEachByteInArrayIsRight(bytesRead, 231); // и проверим, что записалось что нужно, - новый объем как будто проработан.
        }

        [TestMethod]
        public void MakeSureDataStreamReleasesItsLockWhenDisposed()
        {
            var mocks = new MockRepository();

            IDataStreamStructureBuilder structureBuilderStub = mocks.Stub<IDataStreamStructureBuilder>();
            IFileSystemLockReleaseManager lockingManagerMock = mocks.DynamicMock<IFileSystemLockReleaseManager>();
            Guid lockId = Guid.NewGuid();

            using (mocks.Unordered())
            {
                lockingManagerMock.ReleaseLock(lockId);
                LastCall.Repeat.Once();
            }

            mocks.ReplayAll();

            using (var stream = new DataStream(structureBuilderStub, 2048, lockingManagerMock, lockId))
            {                
            }

            mocks.VerifyAll();
        }

        [TestMethod]
        public void MakeSureDataStreamReleasesItsLockEvenIfNotDisposed()
        {
            var mocks = new MockRepository();

            IDataStreamStructureBuilder structureBuilderStub = mocks.Stub<IDataStreamStructureBuilder>();
            IFileSystemLockReleaseManager lockingManagerMock = mocks.DynamicMock<IFileSystemLockReleaseManager>();
            Guid lockId = Guid.NewGuid();

            using (mocks.Unordered())
            {
                lockingManagerMock.ReleaseLock(lockId);
                LastCall.Repeat.Once();
            }

            mocks.ReplayAll();

            var stream = new DataStream(structureBuilderStub, 2048, lockingManagerMock, lockId);

            stream = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            mocks.VerifyAll();
        }

        [TestMethod]
        public void TryShrinkingTheStreamAndThenWritingReadingFromIt()
        {
            DataStreamReadableWritable dataStream = TestCollaboratorsFactory.CreateEmptyDataStream();

            const int testArrayLength = 50000;
            dataStream.SetLength(testArrayLength);

            byte[] bytesToWrite = new byte[testArrayLength];

            for (int i = 0; i < bytesToWrite.Length; i++)
            {
                bytesToWrite[i] = (byte)(i / 10000);
            }

            dataStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            ShrinkAndMakeSureDataIsIntact(dataStream, 40000);
            ShrinkAndMakeSureDataIsIntact(dataStream, 34567);
            ShrinkAndMakeSureDataIsIntact(dataStream, 25000);
            ShrinkAndMakeSureDataIsIntact(dataStream, 9999);
            ShrinkAndMakeSureDataIsIntact(dataStream, 24);
            ShrinkAndMakeSureDataIsIntact(dataStream, 1);
        }

        [TestMethod]
        public void MakeSureShrinkingFreesDiskSpace()
        {
            var testCollaborators = TestCollaboratorsFactory.CreateCollaboratorsForTestingDataStreams();

            DataStreamReadableWritable dataStream =
                testCollaborators.VirtualFileSystem.OpenFileForWriting(testCollaborators.FileInfo.FullPath);

            int valueCloseToDiskSize = (int)testCollaborators.Disk.DiskSizeInBytes - 25 * testCollaborators.Disk.BlockSizeInBytes;
            dataStream.SetLength(valueCloseToDiskSize);

            var fileSystem = testCollaborators.VirtualFileSystem;

            FileInfo newFileNode = fileSystem.CreateFile(@"\testFile2");

            DataStreamReadableWritable newFileNodeStream = fileSystem.OpenFileForWriting(newFileNode.FullPath);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InsufficientSpaceException>(
                delegate
                    {
                        newFileNodeStream.SetLength(valueCloseToDiskSize);
                    });

            dataStream.SetLength(0);

            newFileNodeStream.SetLength(valueCloseToDiskSize);
            dataStream.SetPosition(0);

            const int answeringToAllQuestionsByte = 42;

            byte[] bytesToWrite = ByteBufferFactory.CreateByteBufferWithAllBytesSet(valueCloseToDiskSize, answeringToAllQuestionsByte);

            newFileNodeStream.Write(bytesToWrite, 0, bytesToWrite.Length);

            newFileNodeStream.SetPosition(0);

            var buffer = new byte[valueCloseToDiskSize];
            newFileNodeStream.Read(buffer, 0, buffer.Length);

            MakeSureEachByteInArrayIsRight(buffer, answeringToAllQuestionsByte);
        }

        private void ShrinkAndMakeSureDataIsIntact(DataStreamReadableWritable dataStream, int newLength)
        {
            dataStream.SetLength(newLength);

            Assert.AreEqual(newLength, dataStream.Length);

            dataStream.SetPosition(0);

            byte[] bytesRead = new byte[newLength];

            dataStream.Read(bytesRead, 0, bytesRead.Length);

            for (int i = 0; i < newLength; i++)
            {
                Assert.AreEqual(bytesRead[i], (byte)(i / 10000));
            }
        }

        [TestMethod]
        public void TryReadingAndWritingPortionsOfDataByteByByte()
        {
            DataStreamReadableWritable dataStream = TestCollaboratorsFactory.CreateEmptyDataStream();

            var stream = new MemoryStream();
            var binaryWriter = new BinaryWriter(stream);

            const int numberOfIntegersToTest = 2561;

            for (int i = 0; i < numberOfIntegersToTest; i++)
            {
                binaryWriter.Write(i);
            }

            byte[] bytes = stream.ToArray();

            for (int i = 0; i < bytes.Length; i++)
            {
                dataStream.Write(new byte[] { bytes[i] }, 0, 1);
            }

            dataStream.SetPosition(0);

            var bytesRead = new byte[dataStream.Length];

            dataStream.Read(bytesRead, 0, dataStream.Length);

            var readingStream = new MemoryStream(bytesRead, false);
            var reader = new BinaryReader(readingStream);

            for (int i = 0; i < numberOfIntegersToTest; i++)
            {
                int number = reader.ReadInt32();

                Assert.AreEqual(i, number);
            }
        }

        [TestMethod]
        public void TryChangingPositionCombinedWithWritingAndReading()
        {
            DataStreamReadableWritable dataStream = TestCollaboratorsFactory.CreateEmptyDataStream();

            byte[] bytes = ByteBufferFactory.CreateByteBufferWithAllBytesSet(123, 123);

            dataStream.Write(bytes, 0, bytes.Length);

            dataStream.SetPosition(0);

            byte[] bytes2 = ByteBufferFactory.CreateByteBufferWithAllBytesSet(14, 77);

            dataStream.Write(bytes2, 0, bytes2.Length);

            var bytesRead = new byte[dataStream.Length];

            dataStream.SetPosition(0);

            dataStream.Read(bytesRead, 0, dataStream.Length);

            for (int i = 0; i < dataStream.Length; i++)
            {
                if (i <= 13)
                {
                    Assert.AreEqual(77, bytesRead[i]);
                }
                else
                {
                    Assert.AreEqual(123, bytesRead[i]);
                }
            }
        }

        [TestMethod]
        public void TryWritingABigFileReadingItAfterSystemReload()
        {
            var testCollaborators = TestCollaboratorsFactory.CreateAllCollaborators(50000, false);

            byte[] interestingBytes = ByteBufferFactory.BuildSomeGuidsIntoByteArray(5000);

            int streamLength;

            const int numberOfGuidPortionsToReadAndWrite = 500;

            using (DataStreamReadableWritable stream = testCollaborators.VirtualFileSystem.CreateAndOpenFileForWriting(@"\hey"))
            {
                for (int i = 0; i < numberOfGuidPortionsToReadAndWrite; i++)
                {
                    stream.Write(interestingBytes, 0, interestingBytes.Length);
                }

                streamLength = stream.Length;
            }

            VirtualFileSystem fileSystemReloaded =
                TestCollaboratorsFactory.CreateFileSystemFromExistingStream(testCollaborators.Stream);

            using (DataStreamReadable stream = fileSystemReloaded.OpenFileForReading(@"\hey"))
            {
                Assert.AreEqual(streamLength, stream.Length);

                var bytesRead = new byte[interestingBytes.Length];

                for (int i = 0; i < numberOfGuidPortionsToReadAndWrite; i++)
                {
                    int numberOfBytesRead = stream.Read(bytesRead, 0, interestingBytes.Length);
                    CollectionAssert.AreEqual(interestingBytes, bytesRead);
                }
            }
        }

        [TestMethod]
        [Ignore]
        public void TryPushingAFileToItsLimitSize()
        {
            var testCollaborators = TestCollaboratorsFactory.CreateCollaboratorsForTestingDataStreamsOneGigabyteDrive();

            DataStreamReadableWritable stream =
                testCollaborators.VirtualFileSystem.OpenFileForWriting(testCollaborators.FileInfo.FullPath);

            var zeroes = new byte[testCollaborators.Disk.BlockSizeInBytes * 1000];

            ExceptionAssert.MakeSureExceptionIsRaisedBy<MaximumFileSizeReachedException>(
                delegate
                    {
                        while (stream.Length != Int32.MaxValue)
                        {
                            stream.Write(zeroes, 0, zeroes.Length);
                        }
                    });
        }

        [TestMethod]
        public void MakeSureReadDoesNotThrowIfReadingBeyondTheStream()
        {
            DataStreamReadableWritable dataStream = TestCollaboratorsFactory.CreateEmptyDataStream();

            byte[] bytes = ByteBufferFactory.CreateByteBufferWithAllBytesSet(55000, 123);

            dataStream.Write(bytes, 0, bytes.Length);

            dataStream.SetPosition(55000 - 35);

            var largeArray = new byte[2000];

            Assert.AreEqual(35, dataStream.Read(largeArray, 0, 2000));
        }

        private static void MakeSureEachByteInArrayIsRight(byte[] bytesToCheck, int theRightByte)
        {
            foreach (byte byteToCheck in bytesToCheck)
            {
                Assert.AreEqual(theRightByte, byteToCheck);
            }
        }
    }
}