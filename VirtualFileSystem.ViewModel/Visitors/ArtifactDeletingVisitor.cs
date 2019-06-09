using System;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel.Visitors
{
    internal class ArtifactDeletingVisitor : IFileSystemArtifactViewModelVisitor
    {
        private readonly VirtualFileSystem _fileSystem;
        private readonly IUserInteractionService _userInteractionService;

        public ArtifactDeletingVisitor(VirtualFileSystem fileSystem, IUserInteractionService userInteractionService)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (userInteractionService == null) throw new ArgumentNullException("userInteractionService");

            _fileSystem = fileSystem;
            _userInteractionService = userInteractionService;
        }

        public void VisitFile(FileViewModel fileViewModel)
        {
            if (fileViewModel == null) throw new ArgumentNullException("fileViewModel");

            try
            {
                _fileSystem.DeleteFile(fileViewModel.FullPath);
            }
            catch (FileNotFoundException exception)
            {
                this.HandleErrorThatHasAUserFriendlyMessage(exception);
            }
            catch (FileLockedException exception)
            {
                this.HandleErrorThatHasAUserFriendlyMessage(exception);
            }
            catch(InvalidPathException exception)
            {
                this.HandleErrorThatHasAUserFriendlyMessage(exception);
            }
        }

        public void VisitFolder(FolderViewModel folderViewModel)
        {
            if (folderViewModel == null) throw new ArgumentNullException("folderViewModel");

            try
            {
                _fileSystem.DeleteFolder(folderViewModel.FullPath);
            }
            catch (FolderNotFoundException exception)
            {
                this.HandleErrorThatHasAUserFriendlyMessage(exception);
            }
            catch(FolderNotEmptyException exception)
            {
                this.HandleErrorThatHasAUserFriendlyMessage(exception);
            }
        }

        private void HandleErrorThatHasAUserFriendlyMessage(Exception exception)
        {
            _userInteractionService.ShowMessage(exception.Message);
        }
    }
}