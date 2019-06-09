using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class FolderNodeLockableTests
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
        public void MakeSureIsLockedPropertyRelfectsLockStateAdequately()
        {
            FolderNode nodeToLock = FakeNodesFactory.CreateFakeFolderNodes(1).First();
            var nodeLockable = new FolderNodeLockable(nodeToLock);

            List<FolderNode> nodes = new List<FolderNode> {nodeToLock};

            FileLockable lock1 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), nodes);
            FileLockable lock2 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), nodes);
            FileLockable lock3 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), nodes);
            FileLockable lock4 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), nodes);

            Assert.IsFalse(nodeLockable.IsLocked);

            Guid lock1Id = lock1.LockForReading();
            nodeLockable.AddLock(lock1, lock1Id);

            Assert.IsTrue(nodeLockable.IsLocked);

            Guid lock2Id = lock2.LockForReading();
            nodeLockable.AddLock(lock2, lock2Id);

            Guid lock3Id = lock3.LockForReading();
            nodeLockable.AddLock(lock3, lock3Id);

            Guid lock4Id = lock4.LockForReading();
            nodeLockable.AddLock(lock4, lock4Id);

            Assert.IsTrue(nodeLockable.IsLocked);

            nodeLockable.ReleaseLock(lock1Id);
            nodeLockable.ReleaseLock(lock2Id);
            nodeLockable.ReleaseLock(lock3Id);
            nodeLockable.ReleaseLock(lock4Id);

            Assert.IsFalse(nodeLockable.IsLocked);
        }

        [TestMethod]
        public void MakeSureYouCannotAcquireSameLockOnANodeTwice()
        {
            FolderNode nodeToLock = FakeNodesFactory.CreateFakeFolderNodes(1).First();
            var nodeLockable = new FolderNodeLockable(nodeToLock);

            List<FolderNode> nodes = new List<FolderNode> { nodeToLock };
            FileLockable lock1 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), nodes);

            Guid lockId = lock1.LockForReading();

            nodeLockable.AddLock(lock1, lockId);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<LockAlreadyHeldException>(
                delegate
                    {
                        nodeLockable.AddLock(lock1, lockId);
                    });
        }

        [TestMethod]
        public void MakeSureNodeLockableThrowsWhenBeingAcquiredIfFileLockDoesNotListItAsAssociatedNode()
        {
            FolderNode nodeToLock = FakeNodesFactory.CreateFakeFolderNodes(1).First();
            var nodeLockable = new FolderNodeLockable(nodeToLock);

            FileLockable lock1 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), FakeNodesFactory.CreateFakeFolderNodes(2));

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    nodeLockable.AddLock(lock1, lock1.LockForWriting());
                });
        }

        [TestMethod]
        public void MakeSureYouCannotReleaseALockWhichIsNotHeld()
        {
            FolderNode nodeToLock = FakeNodesFactory.CreateFakeFolderNodes(1).First();
            var nodeLockable = new FolderNodeLockable(nodeToLock);

            List<FolderNode> nodes = new List<FolderNode> { nodeToLock };
            FileLockable lock1 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), nodes);
            FileLockable lock2 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), nodes);

            Guid lockId = lock1.LockForReading();
            nodeLockable.AddLock(lock1, lockId);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<LockNotFoundException>(
                delegate
                {
                    nodeLockable.ReleaseLock(lock2.LockForWriting());
                });
        }

        [TestMethod]
        public void MakeSureYouCannotAcquireALockPassingNullOrEmptyGuidForIt()
        {
            FolderNode nodeToLock = FakeNodesFactory.CreateFakeFolderNodes(1).First();
            var nodeLockable = new FolderNodeLockable(nodeToLock);

            List<FolderNode> nodes = new List<FolderNode> { nodeToLock };
            FileLockable lock1 = new FileLockable(FakeNodesFactory.CreateFakeFileNode(), nodes);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                {
                    nodeLockable.AddLock(null, Guid.NewGuid());
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    nodeLockable.AddLock(lock1, Guid.Empty);
                });
        }
    }
}