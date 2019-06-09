using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class PathBuilderTests
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
        public void TestPathBuilderWithDataSuite1()
        {
            PathBuilder pathBuilder = PathBuilder.Default;

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                    {
                        pathBuilder.CombinePaths(null, "newFile");
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    pathBuilder.CombinePaths(VirtualFileSystem.Root, VirtualFileSystem.Root);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    pathBuilder.CombinePaths("folder1", "newFile.txt");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    pathBuilder.CombinePaths(@"   jkfkjdnvkjklvblks\folder", "newFile.txt");
                });

            Assert.AreEqual(VirtualFileSystem.Root, pathBuilder.CombinePaths(@"\", ""));
            Assert.AreEqual(VirtualFileSystem.Root, pathBuilder.CombinePaths(VirtualFileSystem.Root, null));
            Assert.AreEqual(VirtualFileSystem.Root + "newFile.txt", pathBuilder.CombinePaths(VirtualFileSystem.Root, "newFile.txt"));
            Assert.AreEqual(@"\folder1\newFile.txt", pathBuilder.CombinePaths(@"\folder1", "newFile.txt"));
            Assert.AreEqual(VirtualFileSystem.Root + @"folder1\newFile.txt", pathBuilder.CombinePaths(VirtualFileSystem.Root, @"folder1\newFile.txt"));           
        }
    }
}