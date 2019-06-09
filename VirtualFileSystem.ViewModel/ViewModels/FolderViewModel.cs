using System;
using VirtualFileSystem.ViewModel.Visitors;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal class FolderViewModel : FileSystemArtifactViewModel
    {
        internal static FolderViewModel FromFolderInfo(FolderInfo folderInfo)
        {
            if (folderInfo == null) throw new ArgumentNullException("folderInfo");

            return new FolderViewModel(folderInfo.FullPath, folderInfo.Name, folderInfo.Id, folderInfo.CreationTimeUtc.ToLocalTime());
        }

        public FolderViewModel(string fullPath, string name, Guid id, DateTime creationTimeUtc) : base(fullPath, name, id, creationTimeUtc)
        {
        }

        public override void Accept(IFileSystemArtifactViewModelVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            visitor.VisitFolder(this);
        }
    }
}