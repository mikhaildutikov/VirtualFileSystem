using System;
using VirtualFileSystem.Toolbox.Extensions;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel
{
    internal class NewArtifactCreator : INewArtifactCreator
    {
        private readonly VirtualFileSystem _fileSystem;
        private readonly IUserInteractionService _userInteractionService;

        public NewArtifactCreator(VirtualFileSystem fileSystem, IUserInteractionService userInteractionService)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (userInteractionService == null) throw new ArgumentNullException("userInteractionService");

            _fileSystem = fileSystem;
            _userInteractionService = userInteractionService;
        }

        public void CreateNewArtifact(NewArtifactViewModel newArtifactViewModel)
        {
            var combinedPath =
                _fileSystem.PathBuilder.CombinePaths(newArtifactViewModel.Location, newArtifactViewModel.Name);

            if (newArtifactViewModel.ArtifactKind == ArtifactKind.File)
            {
                CreateNewFile(combinedPath);
            }
            else if (newArtifactViewModel.ArtifactKind == ArtifactKind.Folder)
            {
                CreateNewFolder(combinedPath);
            }
            else
            {
                throw new NotSupportedException("Тип артефакта {0} не поддерживается".FormatWith(newArtifactViewModel.ArtifactKind));
            }
        }

        private void CreateNewFolder(string combinedPath)
        {
            try
            {
                _fileSystem.CreateFolder(combinedPath);
            }
            catch (InvalidPathException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
            catch (FolderNotFoundException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
            catch (FolderAlreadyExistsException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
            catch (InsufficientSpaceException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
            catch (MaximumFolderCountReachedException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
        }

        private void CreateNewFile(string combinedPath)
        {
            try
            {
                _fileSystem.CreateFile(combinedPath);
            }
            catch (InvalidPathException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
            catch (FolderNotFoundException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
            catch (FileAlreadyExistsException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
            catch (InsufficientSpaceException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
            catch (MaximumFileCountReachedException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
        }
    }
}