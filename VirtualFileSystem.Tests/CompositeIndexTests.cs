using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class CompositeIndexTests
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
        public void TestJaggedNumberWithOneDimensionalSystem()
        {
            int[] indexCapacities = new[] { 25 };

            CompositeIndex compositeIndex = new CompositeIndex(22, indexCapacities);

            List<int> expectedIndexes = new List<int> { 22 };

            CollectionAssert.AreEqual(expectedIndexes, compositeIndex.CompositeValue);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new CompositeIndex(1500, indexCapacities);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new CompositeIndex(25, indexCapacities);
                });
        }

        [TestMethod]
        public void TestJaggedNumberWithTwoDimensionalSystem()
        {
            int[] dimensions = new[] { 16, 16 };

            CompositeIndex index = new CompositeIndex(254, dimensions);

            List<int> indexCapacities = new List<int> { 15, 14 }; // 0xFE

            CollectionAssert.AreEqual(indexCapacities, index.CompositeValue);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new CompositeIndex(15000, dimensions);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new CompositeIndex(16*16, dimensions);
                });
        }

        [TestMethod]
        public void TestJaggedNumberWithThreeDimensionalSystemJaggedForReal()
        {
            int[] dimensions = new[] { 512, 512, 2048 };

            CompositeIndex number = new CompositeIndex(67045, dimensions);

            List<int> expectedIndexes = new List<int> { 0, 32, 1509 }; // 0 (из 512) -> 32 (из 512) -> 1509 (из 2048).

            CollectionAssert.AreEqual(expectedIndexes, number.CompositeValue);

            Assert.AreEqual(67045, number.Value);

            number = new CompositeIndex(67045000, dimensions);

            expectedIndexes = new List<int> { 63, 480, 1672 }; // 63 (из 512) -> 480 (из 512) -> 1672 (из 2048).

            Assert.AreEqual(67045000, number.Value);

            CollectionAssert.AreEqual(expectedIndexes, number.CompositeValue);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new CompositeIndex(int.MaxValue, dimensions);
                });
        }

        [TestMethod]
        public void MakeSureJaggedNumberThrowsOnBigOrders()
        {
            int[] dimensions = new[] { int.MaxValue / 2, 4, 3 };

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new CompositeIndex(67045, dimensions);
                });
        }

        [TestMethod]
        public void TestJaggedNumberWithFourDimensionalSystemDecimal()
        {
            int[] dimensions = new[] { 10, 10, 10, 10 };

            CompositeIndex number = new CompositeIndex(9876, dimensions);

            List<int> expectedIndexes = new List<int> { 9, 8, 7, 6 };

            CollectionAssert.AreEqual(expectedIndexes, number.CompositeValue);
            Assert.AreEqual(9876, number.Value);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new CompositeIndex(10*10*10*10, dimensions);
                });
        }
    }
}