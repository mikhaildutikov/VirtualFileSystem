using System;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Tests.TestFactories
{
    internal class DataStreamTestCollaboratorSet
    {
        public DataStreamTestCollaboratorSet(IVirtualDisk disk, IFileSystemNodeStorage fileSystemNodeStorage, FileInfo fileInfo, VirtualFileSystem virtualFileSystem)
        {
            if (disk == null) throw new ArgumentNullException("disk");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");
            if (fileInfo == null) throw new ArgumentNullException("fileInfo");
            if (virtualFileSystem == null) throw new ArgumentNullException("virtualFileSystem");

            Disk = disk;
            FileSystemNodeStorage = fileSystemNodeStorage;
            FileInfo = fileInfo;
            VirtualFileSystem = virtualFileSystem;
        }

        public IVirtualDisk Disk { get; private set; }
        public IFileSystemNodeStorage FileSystemNodeStorage { get; private set; }
        public FileInfo FileInfo { get; private set; }
        public VirtualFileSystem VirtualFileSystem { get; private set; }
    }
}