using System;
using System.Runtime.Serialization;

namespace VirtualFileSystem
{
    [Serializable]
    public class CannotGetImportedFolderStructureException : Exception
    {
        public CannotGetImportedFolderStructureException()
        {
        }

        public CannotGetImportedFolderStructureException(string message) : base(message)
        {
        }

        public CannotGetImportedFolderStructureException(string message, Exception inner) : base(message, inner)
        {
        }

        protected CannotGetImportedFolderStructureException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}