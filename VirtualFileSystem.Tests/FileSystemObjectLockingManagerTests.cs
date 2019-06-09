using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.DiskStructuresManagement;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Tests.Helpers;
using VirtualFileSystem.Tests.TestFactories;

namespace VirtualFileSystem.Tests
{
    [TestClass()]
    public class FileSystemObjectLockingManagerTests
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

        [TestMethod()]
        public void MakeSureYouCanAcquireSeveralReadLocksInARow()
        {
            var manager = new FileSystemObjectLockingManager();

            var folderNodes = FakeNodesFactory.CreateFakeFolderNodes(2);

            var fileNode = FakeNodesFactory.CreateFakeFileNode();

            var nodeResolvingResult = new NodeWithSurroundingsResolvingResult<FileNode>(folderNodes, fileNode, FakeNodesFactory.CreateFakeFolderNodes(1).First());

            manager.AcquireLock(nodeResolvingResult, LockKind.Read);
            manager.AcquireLock(nodeResolvingResult, LockKind.Read);
            manager.AcquireLock(nodeResolvingResult, LockKind.Read);
        }

        [TestMethod()]
        public void MakeSureAcquiringALockDoesLockAppropriateFolders()
        {
            var manager = new FileSystemObjectLockingManager();

            var folderNodes = FakeNodesFactory.CreateFakeFolderNodes(2);

            var fileNode = FakeNodesFactory.CreateFakeFileNode();

            var nodeResolvingResult = new NodeWithSurroundingsResolvingResult<FileNode>(folderNodes, fileNode, FakeNodesFactory.CreateFakeFolderNodes(1).First());

            Guid lockId = manager.AcquireLock(nodeResolvingResult, LockKind.Read);

            Assert.IsTrue(manager.IsNodeLockedForRenamesAndMoves(folderNodes[0]));
            Assert.IsTrue(manager.IsNodeLockedForRenamesAndMoves(folderNodes[1]));

            manager.ReleaseLock(lockId);

            Assert.IsFalse(manager.IsNodeLockedForRenamesAndMoves(folderNodes[0]));
            Assert.IsFalse(manager.IsNodeLockedForRenamesAndMoves(folderNodes[1]));
        }

        [TestMethod()]
        public void MakeSurePuttingSeveralLocksOnSameThingCleanUpNicelyWhenYouReleaseTheLocks()
        {
            var manager = new FileSystemObjectLockingManager();

            var folderNodes = FakeNodesFactory.CreateFakeFolderNodes(2);

            var fileNode = FakeNodesFactory.CreateFakeFileNode();

            var nodeResolvingResult = new NodeWithSurroundingsResolvingResult<FileNode>(folderNodes, fileNode, FakeNodesFactory.CreateFakeFolderNodes(1).First());

            Guid lockId = manager.AcquireLock(nodeResolvingResult, LockKind.Read);
            Guid lock2Id = manager.AcquireLock(nodeResolvingResult, LockKind.Read);
            Guid lock3Id = manager.AcquireLock(nodeResolvingResult, LockKind.Read);

            Assert.IsTrue(manager.IsNodeLockedForRenamesAndMoves(folderNodes[0]));
            Assert.IsTrue(manager.IsNodeLockedForRenamesAndMoves(folderNodes[1]));

            manager.ReleaseLock(lockId);
            manager.ReleaseLock(lock3Id);

            Assert.IsTrue(manager.IsNodeLockedForRenamesAndMoves(folderNodes[0]));
            Assert.IsTrue(manager.IsNodeLockedForRenamesAndMoves(folderNodes[1]));

            manager.ReleaseLock(lock2Id);

            Assert.IsFalse(manager.IsNodeLockedForRenamesAndMoves(folderNodes[0]));
            Assert.IsFalse(manager.IsNodeLockedForRenamesAndMoves(folderNodes[1]));
        }

        [TestMethod()]
        public void MakeSureYouCannotAcquireWriteLockOnTheSameThingThatAlreadyHasAReadLock()
        {
            var manager = new FileSystemObjectLockingManager();

            var folderNodes = FakeNodesFactory.CreateFakeFolderNodes(2);

            var fileNode = FakeNodesFactory.CreateFakeFileNode();

            var nodeResolvingResult = new NodeWithSurroundingsResolvingResult<FileNode>(folderNodes, fileNode, FakeNodesFactory.CreateFakeFolderNodes(1).First());

            manager.AcquireLock(nodeResolvingResult, LockKind.Read);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<CannotAcquireLockException>(
                delegate
                    {
                        manager.AcquireLock(nodeResolvingResult, LockKind.Write);
                    });
        }

        [TestMethod()]
        public void MakeSureYouCannotAcquireWriteOrReadLockOnTheThingThatAlreadyHasAWriteLock()
        {
            var manager = new FileSystemObjectLockingManager();

            var folderNodes = FakeNodesFactory.CreateFakeFolderNodes(2);

            var fileNode = FakeNodesFactory.CreateFakeFileNode();

            var nodeResolvingResult = new NodeWithSurroundingsResolvingResult<FileNode>(folderNodes, fileNode, FakeNodesFactory.CreateFakeFolderNodes(1).First());

            manager.AcquireLock(nodeResolvingResult, LockKind.Write);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<CannotAcquireLockException>(
                delegate
                {
                    manager.AcquireLock(nodeResolvingResult, LockKind.Write);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<CannotAcquireLockException>(
                delegate
                {
                    manager.AcquireLock(nodeResolvingResult, LockKind.Read);
                });
        }

        [TestMethod()]
        public void MakeSureYouCannotProvideNullAsFileResolvingResultWhenTryingToLock()
        {
            var manager = new FileSystemObjectLockingManager();

            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                    {
                        manager.AcquireLock(null, LockKind.Write);
                    });
        }

        [TestMethod()]
        public void MakeSureNoFolderIsLockedAfterManagerCreation()
        {
            var manager = new FileSystemObjectLockingManager();

            Assert.IsFalse(manager.IsNodeLockedForRenamesAndMoves(FakeNodesFactory.CreateFakeFolderNodes(1).First()));
            Assert.IsFalse(manager.IsNodeLockedForRenamesAndMoves(FakeNodesFactory.CreateFakeFolderNodes(1).First()));
            Assert.IsFalse(manager.IsNodeLockedForRenamesAndMoves(FakeNodesFactory.CreateFakeFolderNodes(1).First()));
        }
    }
}