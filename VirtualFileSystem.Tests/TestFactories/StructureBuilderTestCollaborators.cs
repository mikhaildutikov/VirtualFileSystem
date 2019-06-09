using System;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Tests.TestFactories
{
    internal class StructureBuilderTestCollaborators
    {
        public StructureBuilderTestCollaborators(NodeWithSurroundingsResolvingResult<FileNode> fileNode, IVirtualDisk disk)
        {
            if (fileNode == null) throw new ArgumentNullException("fileNode");
            if (disk == null) throw new ArgumentNullException("disk");

            FileNode = fileNode;
            Disk = disk;            
        }

        public NodeWithSurroundingsResolvingResult<FileNode> FileNode { get; private set; }
        public IVirtualDisk Disk { get; private set; }
    }
}