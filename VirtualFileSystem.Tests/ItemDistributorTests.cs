using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class ItemDistributorTests
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
        public void EnsureDistributorThrowsIfFirstBucketIsSaidToContainMoreThanBucketCapacity()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                    {
                        ItemDistributor.Distribute(100, 100000, 1000);
                    });
        }

        [TestMethod]
        public void EnsureDistributorThrowsIfAnyNumberPassedToItAsDistributionArgumentIsNegative()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    ItemDistributor.Distribute(-100, 10, 1000);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    ItemDistributor.Distribute(100, -10, 1000);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    ItemDistributor.Distribute(100, 10, -1000);
                });
        }

        [TestMethod]
        public void TestDataSuite1()
        {
            ReadOnlyCollection<BucketDistribution> distributions = ItemDistributor.Distribute(100, 1000, 1000);

            List<BucketDistribution> expectedList = new List<BucketDistribution> {new BucketDistribution(0, 0, 100)};

            CollectionAssert.AreEqual(expectedList, distributions);
        }

        [TestMethod]
        public void TestDataSuite2()
        {
            ReadOnlyCollection<BucketDistribution> distributions = ItemDistributor.Distribute(1000, 0, 1000);

            List<BucketDistribution> expectedList = new List<BucketDistribution> { new BucketDistribution(1, 0, 1000) };

            CollectionAssert.AreEqual(expectedList, distributions);
        }

        [TestMethod]
        public void TestDataSuite3()
        {
            ReadOnlyCollection<BucketDistribution> distributions = ItemDistributor.Distribute(50, 5, 10);

            List<BucketDistribution> expectedList = new List<BucketDistribution>
                                                        {
                                                            new BucketDistribution(0, 5, 5),
                                                            new BucketDistribution(1, 0, 10),
                                                            new BucketDistribution(2, 0, 10),
                                                            new BucketDistribution(3, 0, 10),
                                                            new BucketDistribution(4, 0, 10),
                                                            new BucketDistribution(5, 0, 5),
                                                        };

            CollectionAssert.AreEqual(expectedList, distributions);
        }
    }
}