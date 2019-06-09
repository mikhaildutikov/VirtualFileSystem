using System;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.DiskStructuresManagement
{
    [Serializable]
    internal class FolderNode : Node
    {
        private readonly DataStreamDefinition _fileReferencesStreamDefinition;
        private readonly DataStreamDefinition _folderReferencesStreamDefinition;
        private readonly Int32 _parentFolderBlockReference;

        public FolderNode(string name, Guid id, int diskBlockIndex, int parentFolderBlockReference, DataStreamDefinition fileReferencesStreamDefinition, DataStreamDefinition folderReferencesStreamDefinition, DateTime creationTime, Guid version)
            : base(name, id,  diskBlockIndex, version, creationTime)
        {
            MethodArgumentValidator.ThrowIfNull(fileReferencesStreamDefinition, "fileReferencesStream");
            MethodArgumentValidator.ThrowIfNull(folderReferencesStreamDefinition, "folderReferencesStream");
            MethodArgumentValidator.ThrowIfNegative(parentFolderBlockReference, "parentFolderBlockReference");
            MethodArgumentValidator.ThrowIfNegative(diskBlockIndex, "diskBlockIndex");
            MethodArgumentValidator.ThrowIfIsDefault(id, "id");

            _fileReferencesStreamDefinition = fileReferencesStreamDefinition;
            _folderReferencesStreamDefinition = folderReferencesStreamDefinition;
            _parentFolderBlockReference = parentFolderBlockReference;
        }

        public DataStreamDefinition FileReferencesStreamDefinition
        {
            get
            {
                return _fileReferencesStreamDefinition;
            }
        }

        public DataStreamDefinition FolderReferencesStreamDefinition
        {
            get
            {
                return _folderReferencesStreamDefinition;
            }
        }

        public Int32 ParentFolderBlockReference
        {
            get
            {
                return _parentFolderBlockReference;
            }
        }
    }
}