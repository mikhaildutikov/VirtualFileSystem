using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class FileLockableTests
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
        public void MakeSureYouCanGetSeveralReadersWithoutAnException()
        {
            var fileLock = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), FakeNodesFactory.CreateFakeFolderNodes(1));

            fileLock.LockForReading();
            fileLock.LockForReading();
            fileLock.LockForReading();
        }

        [TestMethod]
        public void MakeSureYouCannotGetAWriteLockWithReadLockInPlace()
        {
            var fileLock = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), FakeNodesFactory.CreateFakeFolderNodes(1));

            fileLock.LockForReading();
            
            ExceptionAssert.MakeSureExceptionIsRaisedBy<CannotAcquireLockException>(
                delegate
                    {
                        fileLock.LockForWriting();
                    });
        }

        [TestMethod]
        public void MakeSureYouCannotGetAReadLockWithWriteLockInPlace()
        {
            var fileLock = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), FakeNodesFactory.CreateFakeFolderNodes(1));

            fileLock.LockForWriting();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<CannotAcquireLockException>(
                delegate
                {
                    fileLock.LockForReading();
                });
        }

        [TestMethod]
        public void MakeSureYouCannotGiveNullAsFileBeingLockedAndGetAwayWithIt()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    new FileLockable(null, FakeNodesFactory.CreateFakeFolderNodes(1));
                });
        }

        [TestMethod]
        public void MakeSureYouCannotGetAWriteLockWithAnotherWriteLockInPlace()
        {
            var fileLock = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), FakeNodesFactory.CreateFakeFolderNodes(1));

            fileLock.LockForWriting();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<CannotAcquireLockException>(
                delegate
                {
                    fileLock.LockForWriting();
                });
        }

        [TestMethod]
        public void MakeSureYouCanGetAWriteLockWithAnotherWriteLockInPlaceIfYouReleaseThatOtherLockFirst()
        {
            var fileLock = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), FakeNodesFactory.CreateFakeFolderNodes(1));

            Guid lockId = fileLock.LockForWriting();
            fileLock.ReleaseLock(lockId);
            
            fileLock.LockForWriting();
        }

        [TestMethod]
        public void MakeSureYouCannotInitializeFileLockableWithoutAssociatedNodes()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                    {
                        new FileLockable(FakeNodesFactory.CreateFakeFileNode(), FakeNodesFactory.CreateFakeFolderNodes(0));
                    });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    new FileLockable(FakeNodesFactory.CreateFakeFileNode(), null);
                });
        }

        [TestMethod]
        public void MakeSureYouCanGetAWriteLockAfterReadLocksAreReleased()
        {
            var fileLock = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), FakeNodesFactory.CreateFakeFolderNodes(1));

            Guid lockId = fileLock.LockForReading();
            Guid lock2Id = fileLock.LockForReading();

            fileLock.ReleaseLock(lockId);
            fileLock.ReleaseLock(lock2Id);

            fileLock.LockForWriting();
        }
    }
}