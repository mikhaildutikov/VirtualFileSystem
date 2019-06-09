using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel.Visitors
{
    internal interface IFileSystemArtifactViewModelVisitor
    {
        void VisitFile(FileViewModel fileViewModel);
        void VisitFolder(FolderViewModel folderViewModel);
    }
}