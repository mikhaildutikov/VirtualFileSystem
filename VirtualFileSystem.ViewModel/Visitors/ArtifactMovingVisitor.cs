using System;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel.Visitors
{
    internal class ArtifactMovingVisitor : IFileSystemArtifactViewModelVisitor
    {
        private readonly VirtualFileSystem _fileSystem;
        private readonly IUserInteractionService _userInteractionService;
        private readonly IPathValidator _pathValidator;

        public ArtifactMovingVisitor(VirtualFileSystem fileSystem, IUserInteractionService userInteractionService, IPathValidator pathValidator)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (userInteractionService == null) throw new ArgumentNullException("userInteractionService");
            if (pathValidator == null) throw new ArgumentNullException("pathValidator");

            _fileSystem = fileSystem;
            _userInteractionService = userInteractionService;
            _pathValidator = pathValidator;
        }

        public void VisitFile(FileViewModel fileViewModel)
        {
            if (fileViewModel == null) throw new ArgumentNullException("fileViewModel");

            var pathViewModel = new VirtualFolderPathViewModel(_pathValidator) { Path = VirtualFileSystem.Root };

            if (_userInteractionService.GetVirtualFolderPath(pathViewModel))
            {
                try
                {
                    _fileSystem.MoveFile(fileViewModel.FullPath, pathViewModel.Path);
                }
                catch (FileNotFoundException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (FileLockedException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (InvalidPathException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (FileAlreadyExistsException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (FolderNotFoundException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (MaximumFileCountReachedException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (InsufficientSpaceException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
            }
        }

        public void VisitFolder(FolderViewModel folderViewModel)
        {
            if (folderViewModel == null) throw new ArgumentNullException("folderViewModel");

            var pathViewModel = new VirtualFolderPathViewModel(_pathValidator) { Path = VirtualFileSystem.Root };

            if (_userInteractionService.GetVirtualFolderPath(pathViewModel))
            {
                try
                {
                    _fileSystem.MoveFolder(folderViewModel.FullPath, pathViewModel.Path);
                }
                catch (FolderNotFoundException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (FolderLockedException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (InvalidPathException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (FolderAlreadyExistsException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (InvalidOperationException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (MaximumFolderCountReachedException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
                catch (InsufficientSpaceException exception)
                {
                    this.HandleErrorThatHasAUserFriendlyMessage(exception);
                }
            }
        }

        private void HandleErrorThatHasAUserFriendlyMessage(Exception exception)
        {
            _userInteractionService.ShowMessage(exception.Message);
        }
    }
}