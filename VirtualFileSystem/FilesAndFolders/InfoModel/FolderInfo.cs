using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem
{
    public class FolderInfo : FileSystemArtifactInfo
    {
        internal FolderInfo(FolderNode folderNode, string fullPath)
            : base(fullPath, folderNode.Name, folderNode.Id, folderNode.Version, folderNode.CreationTimeUtc)
        {
        }
    }
}