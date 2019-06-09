using System;

namespace VirtualFileSystem.DiskStructuresManagement
{
    [Serializable]
    internal class FileSystemHeader
    {
        private readonly Version _fileSystemVersion;
        private readonly string _magicNumber = "5fd0d75fa8c0450d85ea19b82b35fac9";
        private readonly int _rootBlockOffset;

        public FileSystemHeader(int rootBlockOffset, Version version)
        {
            // args, stuff.

            _rootBlockOffset = rootBlockOffset;
            _fileSystemVersion = version;
        }

        public int RootBlockOffset
        {
            get
            {
                return _rootBlockOffset;
            }
        }

        public Version Version
        {
            get
            {
                return _fileSystemVersion;
            }
        }
    }
}