using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class WildcardTests
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
        public void MakeSureYouCannotInitializeAWildcardWithoutAPatternSpecified()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                    {
                        new Wildcard(null);
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    new Wildcard(String.Empty);
                });
        }

        [TestMethod]
        public void TestWildcardWithDataSuite1()
        {
            Wildcard wildcard = new Wildcard("??");

            Assert.IsTrue(wildcard.CheckStringForMatch("AB"));
            Assert.IsTrue(wildcard.CheckStringForMatch("<>"));
            Assert.IsTrue(wildcard.CheckStringForMatch(";!"));

            Assert.IsFalse(wildcard.CheckStringForMatch("ABC"));
            Assert.IsFalse(wildcard.CheckStringForMatch("A"));
            Assert.IsFalse(wildcard.CheckStringForMatch("&&?"));

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                    {
                        wildcard.CheckStringForMatch(null);
                    });
        }

        [TestMethod]
        public void TestWildcardWithDataSuite2()
        {
            Wildcard wildcard = new Wildcard("*");

            Assert.IsTrue(wildcard.CheckStringForMatch("AB"));
            Assert.IsTrue(wildcard.CheckStringForMatch("<>"));
            Assert.IsTrue(wildcard.CheckStringForMatch(";!"));
            Assert.IsTrue(wildcard.CheckStringForMatch(""));
            Assert.IsTrue(wildcard.CheckStringForMatch(Guid.NewGuid().ToString("N")));
            Assert.IsTrue(wildcard.CheckStringForMatch("kesmfou~!@#$%^&*()_+"));
        }

        [TestMethod]
        public void TestWildcardWithDataSuite3()
        {
            Wildcard wildcard = new Wildcard("*.*");

            Assert.IsTrue(wildcard.CheckStringForMatch("A.B"));
            Assert.IsTrue(wildcard.CheckStringForMatch("<>.!!"));
            Assert.IsTrue(wildcard.CheckStringForMatch("readme.txt"));
            Assert.IsTrue(wildcard.CheckStringForMatch("io.sys"));
            
            Assert.IsFalse(wildcard.CheckStringForMatch(Guid.NewGuid().ToString("N")));
            Assert.IsFalse(wildcard.CheckStringForMatch("kesmfou~!@#$%^&*()_+"));
        }
    }
}