using System;
using System.IO;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Tests.TestFactories
{
    class AllCollaborators
    {
        public AllCollaborators(IVirtualDisk disk, IFileSystemNodeStorage diskStructuresManager, IFileSystemArtifactNamesValidator nameValidator, IPathValidator pathValidator, NodeResolver nodeResolver, VirtualFileSystem virtualFileSystem, FileInfo fileNodeFake, Stream stream)
        {
            if (disk == null) throw new ArgumentNullException("disk");
            if (diskStructuresManager == null) throw new ArgumentNullException("diskStructuresManager");
            if (nameValidator == null) throw new ArgumentNullException("nameValidator");
            if (pathValidator == null) throw new ArgumentNullException("pathValidator");
            if (nodeResolver == null) throw new ArgumentNullException("nodeResolver");
            if (virtualFileSystem == null) throw new ArgumentNullException("virtualFileSystem");
            if (stream == null) throw new ArgumentNullException("stream");

            Disk = disk;
            DiskStructuresManager = diskStructuresManager;
            NameValidator = nameValidator;
            PathValidator = pathValidator;
            NodeResolver = nodeResolver;
            VirtualFileSystem = virtualFileSystem;
            FileNodeFake = fileNodeFake;
            Stream = stream;
        }

        public IVirtualDisk Disk { get; private set; }
        public IFileSystemNodeStorage DiskStructuresManager { get; private set; }
        public IFileSystemArtifactNamesValidator NameValidator { get; private set; }
        public IPathValidator PathValidator { get; private set; }
        public NodeResolver NodeResolver { get; private set; }
        public VirtualFileSystem VirtualFileSystem { get; private set; }
        public FileInfo FileNodeFake { get; private set; }
        public Stream Stream { get; set; }
    }
}