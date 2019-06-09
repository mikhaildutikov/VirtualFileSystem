using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel
{
    internal interface IArtifactImporter
    {
        void KickOffVirtualSystemImport(string fileSystemContainer, string destinationPath, TaskViewModel taskViewModel,
                                        FileSystemCancellableTaskToken taskToken);

        void KickOffRealFileSystemImport(TaskViewModel taskViewModel, string sourceFolder, string destinationFolderPath,
                                         FileSystemCancellableTaskToken taskToken);
    }
}