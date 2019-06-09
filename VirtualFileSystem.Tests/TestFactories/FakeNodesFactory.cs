using System;
using System.Collections.Generic;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Tests.TestFactories
{
    internal static class FakeNodesFactory
    {
        public static FileNode CreateFakeFileNode()
        {
            return new FileNode("TestFile", Guid.NewGuid(), 20, new DataStreamDefinition(10, 15), DateTime.UtcNow, DateTime.UtcNow, Guid.NewGuid());
        }

        public static List<FolderNode> CreateFakeFolderNodes(uint numberOfNodesToCreate)
        {
            var nodes = new List<FolderNode>();

            for (int i = 0; i < numberOfNodesToCreate; i++)
            {
                Guid id = Guid.NewGuid();

                nodes.Add(
                    new FolderNode(id.ToString("N"), id, 11, 45, new DataStreamDefinition(30, 23), new DataStreamDefinition(11, 0), DateTime.UtcNow, Guid.NewGuid()));
            }

            return nodes;
        }
    }
}