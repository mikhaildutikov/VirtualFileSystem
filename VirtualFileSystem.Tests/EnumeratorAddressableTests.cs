using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class EnumeratorAddressableTests
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

        private static List<int> CreateTestList()
        {
            return new List<int> {1, 2, 3};
        }

        [TestMethod]
        public void MakeSureEnumeratorGoesThruAllItemsInTheCollection()
        {
            List<int> integers = CreateTestList();

            EnumeratorAddressable<int> enumerator = new EnumeratorAddressable<int>(new ListTrackingChanges<int>(integers));

            List<int> integersAsTraversed = new List<int>();

            while (enumerator.MoveNext())
            {
                integersAsTraversed.Add(enumerator.Current);
            }

            CollectionAssert.AreEquivalent(integers, integersAsTraversed);
        }

        [TestMethod]
        public void MakeSureEnumeratorChangesPositionAndAllowsPartialTraversal()
        {
            List<int> integers = CreateTestList();

            EnumeratorAddressable<int> enumerator = new EnumeratorAddressable<int>(new ListTrackingChanges<int>(integers));

            List<int> integersAsTraversed = new List<int>();

            enumerator.SetPosition(1);

            while (enumerator.MoveNext())
            {
                integersAsTraversed.Add(enumerator.Current);
            }

            CollectionAssert.AreEquivalent(new List<int>{3}, integersAsTraversed);
        }

        [TestMethod]
        public void MakeSureResetMakesEnumeratorToStartOverEveryTime()
        {
            List<int> integers = CreateTestList();

            EnumeratorAddressable<int> enumerator = new EnumeratorAddressable<int>(new ListTrackingChanges<int>(integers));

            List<int> integersAsTraversed = new List<int>();

            while (enumerator.MoveNext())
            {
                integersAsTraversed.Add(enumerator.Current);
            }

            enumerator.Reset();

            while (enumerator.MoveNext())
            {
                integersAsTraversed.Add(enumerator.Current);
            }

            CollectionAssert.AreEquivalent(new List<int> { 1, 2, 3, 1, 2, 3 }, integersAsTraversed);
        }

        [TestMethod]
        public void MakeSureEnumeratorThrowsIfYouTryToSetAnInvalidPositionForIt()
        {
            List<int> integers = CreateTestList();

            EnumeratorAddressable<int> enumerator = new EnumeratorAddressable<int>(new ListTrackingChanges<int>(integers));

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                    {
                        enumerator.SetPosition(3);
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    enumerator.SetPosition(300);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentOutOfRangeException>(
                delegate
                {
                    enumerator.SetPosition(-111);
                });
        }
    }
}