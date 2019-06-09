using System;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.DiskStructuresManagement
{
    [Serializable]
    internal class FileNode : Node
    {
        private readonly DataStreamDefinition _fileContentsStreamDefinition;
        private DateTime _lastModificationTimeUtc;

        public FileNode(string name, Guid id, int diskBlockIndex, DataStreamDefinition fileContentsStreamDefinition, DateTime lastModificationTimeUtc, DateTime creationTimeUtc, Guid version)
            : base(name, id, diskBlockIndex, version, creationTimeUtc)
        {
            if (fileContentsStreamDefinition == null) throw new ArgumentNullException("fileContentsStreamDefinition");
            MethodArgumentValidator.ThrowIfIsDefault<DateTime>(lastModificationTimeUtc, "lastModificationTimeUtc");
            MethodArgumentValidator.ThrowIfDateIsNonUtc(lastModificationTimeUtc, "lastModificationTimeUtc");

            _fileContentsStreamDefinition = fileContentsStreamDefinition;
            _lastModificationTimeUtc = lastModificationTimeUtc;
        }

        public DataStreamDefinition FileContentsStreamDefinition
        {
            get
            {
                return _fileContentsStreamDefinition;
            }
        }

        public DateTime LastModificationTimeUtc
        {
            get
            {
                return _lastModificationTimeUtc;
            }
            set
            {
                // TODO: args;
                _lastModificationTimeUtc = value;
            }
        }
    }
}