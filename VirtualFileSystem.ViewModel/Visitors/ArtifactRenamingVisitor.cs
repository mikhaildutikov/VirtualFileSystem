using System;
using System.IO;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel.Visitors
{
    internal class ArtifactRenamingVisitor : IFileSystemArtifactViewModelVisitor
    {
        private readonly VirtualFileSystem _fileSystem;
        private readonly IUserInteractionService _userInteractionService;
        private readonly IFileSystemArtifactNamesValidator _artifactNamesValidator;

        public ArtifactRenamingVisitor(VirtualFileSystem fileSystem, IUserInteractionService userInteractionService, IFileSystemArtifactNamesValidator artifactNamesValidator)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (userInteractionService == null) throw new ArgumentNullException("userInteractionService");
            if (artifactNamesValidator == null) throw new ArgumentNullException("artifactNamesValidator");

            _fileSystem = fileSystem;
            _artifactNamesValidator = artifactNamesValidator;
            _userInteractionService = userInteractionService;
        }

        public void VisitFile(FileViewModel fileViewModel)
        {
            if (fileViewModel == null) throw new ArgumentNullException("fileViewModel");

            var newArtifactNameViewModel = new NewArtifactNameViewModel(_artifactNamesValidator){Name = "НовоеИмя"}; // Note: правила хорошего тона говорят нам, что все значения по умолчанию должны быть валидными. Я здесь не использую это правило на 100%.

            if (_userInteractionService.GetNewArtifactNamesFromUser(newArtifactNameViewModel))
            {
                try
                {
                    _fileSystem.RenameFile(fileViewModel.FullPath, newArtifactNameViewModel.Name);
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
            }
        }

        public void VisitFolder(FolderViewModel folderViewModel)
        {
            if (folderViewModel == null) throw new ArgumentNullException("folderViewModel");

            var newArtifactNameViewModel = new NewArtifactNameViewModel(_artifactNamesValidator) { Name = "НовоеИмя" }; // Note: правила хорошего тона говорят нам, что все значения по умолчанию должны быть валидными. Я здесь не использую это правило на 100%.

            if (_userInteractionService.GetNewArtifactNamesFromUser(newArtifactNameViewModel))
            {
                try
                {
                    _fileSystem.RenameFolder(folderViewModel.FullPath, newArtifactNameViewModel.Name);
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
                catch (InvalidNameException exception)
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