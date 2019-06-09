using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace VirtualFileSystem.DiskStructuresManagement
{
    internal class NodeResolvingResult<TNode> where TNode : Node
    {
        public NodeResolvingResult(IEnumerable<FolderNode> nodesPassedWhileResolvingFromRoot, TNode resolvedNode)
        {
            if (nodesPassedWhileResolvingFromRoot == null)
                throw new ArgumentNullException("nodesPassedWhileResolvingFromRoot");

            FoldersPassedWhileResolving = nodesPassedWhileResolvingFromRoot.ToList().AsReadOnly();
            this.ResolvedNode = resolvedNode;
        }

        public ReadOnlyCollection<FolderNode> FoldersPassedWhileResolving
        {
            get; private set;
        }

        public TNode ResolvedNode { get; private set; }
    }

    internal class NodeWithSurroundingsResolvingResult<TNode> : NodeResolvingResult<TNode> where TNode: Node
    {
        public NodeWithSurroundingsResolvingResult(IEnumerable<FolderNode> nodesPassedWhileResolvingFromRoot, TNode resolvedNode, FolderNode immediateParent) : base(nodesPassedWhileResolvingFromRoot, resolvedNode)
        {
            ImmediateParent = immediateParent;
        }

        public FolderNode ImmediateParent { get; private set; }
    }
}