using System;
using VirtualFileSystem.ContentsEnumerators;

namespace VirtualFileSystem
{
    internal interface IFolderEnumeratorRegistry : IFolderEnumeratorUnregistrar
    {
        void RegisterEnumerator(FolderContentsEnumerator folderContentsEnumerator);
        void InvalidateEnumeratorsForFolder(Guid folderId);
    }
}