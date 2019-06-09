using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using System;
using System.IO;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class IntegerListConstrainedTests
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
        public void MakeSureYouCannotAddressIntegeresOutsideOfInitializedSpace()
        {
            byte[] bytes = new byte[4096];

            IntegerListConstrained integersList = new IntegerListConstrained(bytes, 0, 24);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate()
                {
                    integersList[1] = 25;
                });
        }

        [TestMethod]
        public void MakeSureTheIntegeresAreBeingProperlyUpdatedAndSerialized()
        {
            byte[] bytes = new byte[4096];
            const int numberOfIntegers = 10;

            IntegerListConstrained integersList = new IntegerListConstrained(bytes, numberOfIntegers, numberOfIntegers);

            for (int i = 0; i < numberOfIntegers; i++)
			{
                integersList[i] = i;
                Assert.AreEqual(i, integersList[i]);
			}

            IntegerListConstrained recreatedBlock = new IntegerListConstrained(integersList.ToByteArray(), integersList.Count, integersList.Count);

            for (int i = 0; i < numberOfIntegers; i++)
            {
                Assert.AreEqual(i, recreatedBlock[i]);
            }
        }

        [TestMethod]
        public void MakeSureYouCannotAddIntegersIfTheBlockIsFull()
        {
            byte[] bytes = new byte[4096];
            const int numberOfIntegers = 10;

            IntegerListConstrained integersList = new IntegerListConstrained(bytes, numberOfIntegers, numberOfIntegers + 1);

            int initialCount = integersList.Count;

            Assert.AreEqual(numberOfIntegers, initialCount);

            integersList.AddInteger(1234);

            int count = integersList.Count;

            Assert.AreEqual(initialCount + 1, count);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidOperationException>(
                delegate
                {
                    integersList.AddInteger(44);
                });
        }

        [TestMethod]
        public void MakeSureYouCanConstructAnArrayOnlyWithSaneParameters()
        {
            byte[] bytes = new byte[4096];
            const int numberOfIntegers = 10;

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    new IntegerListConstrained(bytes, -1, numberOfIntegers + 1);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    new IntegerListConstrained(null, 1, 2);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    new IntegerListConstrained(bytes, 0, -4);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new IntegerListConstrained(bytes, 1234567, 1234567);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new IntegerListConstrained(bytes, 12, 0);
                });
        }

        [TestMethod]
        public void MakeSureYouCannotPutNegativeNumbersInTheBlock()
        {
            byte[] bytes = new byte[4096];
            const int numberOfIntegers = 10;

            var block = new IntegerListConstrained(bytes, 2, numberOfIntegers + 1);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    block[0] = -1;
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    block.AddInteger(-2);
                });

            MemoryStream stream = new MemoryStream(bytes, true);
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((int)-56);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new IntegerListConstrained(bytes, 2, numberOfIntegers + 1);
                });
        }
    }
}