using VirtualFileSystem.ContentsEnumerators;

namespace VirtualFileSystem
{
    internal interface IFolderEnumeratorUnregistrar
    {
        void Unregister(FolderContentsEnumerator folderContentsEnumerator);
    }
}