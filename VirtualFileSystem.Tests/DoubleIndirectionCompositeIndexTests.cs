using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class DoubleIndirectionCompositeIndexTests
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
        public void MakeSureEquivalentIndexesConstructedViaDifferentMethodsAreIndeedEquivalent()
        {
            ConstructIndexesViaDifferentConstructorsAndCompareThem(7777, 512, 512, 2048);

            ConstructIndexesViaDifferentConstructorsAndCompareThem(74837585, 512, 512, 2048);
        }

        private static void ConstructIndexesViaDifferentConstructorsAndCompareThem(int indexValue, int firstIndexCapacity, int secondIndexCapacity, int thirdIndexCapacity)
        {
            var index = new DoubleIndirectionCompositeIndex(indexValue, firstIndexCapacity, secondIndexCapacity, thirdIndexCapacity);

            var index2 = new DoubleIndirectionCompositeIndex(
                index.First, index.FirstIndexCapacity, index.Second, index.SecondIndexCapacity, index.Third, index.ThirdIndexCapacity);

            Assert.AreEqual(index.Value, index2.Value);
            Assert.AreEqual(index.Capacity, index2.Capacity);
            Assert.AreEqual(index.MaxValue, index2.MaxValue);
        }
    }
}