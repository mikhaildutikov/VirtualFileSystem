using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using VirtualFileSystem.ViewModel.Visitors;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal class BrowserHubWindowViewModel : INotifyPropertyChanged
    {
        private readonly IApplicationController _applicationController;
        private readonly IComparer<FileSystemArtifactViewModel> _viewModelComparer;
        private readonly IUserInteractionService _userInteractionService;
        private readonly IVirtualFileSystemInstanceManager _virtualFileSystemInstanceManager;
        private readonly IDispatcher _dispatcher;

        public BrowserHubWindowViewModel(
            IApplicationController applicationController,
            IComparer<FileSystemArtifactViewModel> viewModelComparer,
            IUserInteractionService userInteractionService,
            IVirtualFileSystemInstanceManager virtualFileSystemInstanceManager,
            IDispatcher dispatcher)
        {
            if (applicationController == null) throw new ArgumentNullException("applicationController");
            if (viewModelComparer == null) throw new ArgumentNullException("viewModelComparer");
            if (userInteractionService == null) throw new ArgumentNullException("userInteractionService");
            if (virtualFileSystemInstanceManager == null)
                throw new ArgumentNullException("virtualFileSystemInstanceManager");
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            _applicationController = applicationController;
            _userInteractionService = userInteractionService;
            _virtualFileSystemInstanceManager = virtualFileSystemInstanceManager;
            _dispatcher = dispatcher;
            _viewModelComparer = viewModelComparer;
        }

        public IVirtualFileSystemInstanceManager VirtualFileSystemInstanceManager
        {
            get { return _virtualFileSystemInstanceManager; }
        }

        public void BrowseNewSystemCreatedFormattingTheFile()
        {
            string fullPathForFile =
                _userInteractionService.PickAFile("Укажите имя файла, в котором нужно инициализировать диск и файловую систему");

            if (String.IsNullOrEmpty(fullPathForFile))
            {
                return;
            }

            try
            {
                VirtualFileSystem newVirtualFileSystem = VirtualFileSystemInstanceManager.CreateNewFormattingTheFile(fullPathForFile);

                DisplayBrowserViewModel(fullPathForFile, newVirtualFileSystem);
            }
            catch (FileSystemCreationFailedException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
        }

        private void DisplayBrowserViewModel(string fullPathForFile, VirtualFileSystem newVirtualFileSystem)
        {
            var nameValidator = FileSystemArtifactNamesValidator.Default;

            var stuffDeletingVisitor = new ArtifactDeletingVisitor(newVirtualFileSystem, _userInteractionService);
            var stuffRenamingVisitor = new ArtifactRenamingVisitor(newVirtualFileSystem, _userInteractionService,
                                                                   nameValidator);

            var stuffMovingVisitor = new ArtifactMovingVisitor(newVirtualFileSystem, _userInteractionService,
                                                               PathValidator.Default);

            var newArtifactCreator = new NewArtifactCreator(newVirtualFileSystem, _userInteractionService);

            TaskCounter taskCounter = new TaskCounter();

            var importer = new ArtifactImporter(newVirtualFileSystem, VirtualFileSystemInstanceManager, taskCounter);

            _applicationController.DisplayNewBrowserWindow(
                new BrowserWindowViewModel(newVirtualFileSystem, _applicationController, _viewModelComparer, _userInteractionService, fullPathForFile, StringComparer.OrdinalIgnoreCase, stuffDeletingVisitor, nameValidator, stuffRenamingVisitor, newArtifactCreator, VirtualFileSystemInstanceManager, stuffMovingVisitor, importer, taskCounter, _dispatcher));
        }

        public void BrowseExistingSystem()
        {
            string fullPathForSystemContainer =
                _userInteractionService.PickAFile("Выберите файл, содержащий виртуальную файловую систему");

            if (String.IsNullOrEmpty(fullPathForSystemContainer))
            {
                return;
            }

            try
            {
                string fileSystemId;
                VirtualFileSystem fileSystem = VirtualFileSystemInstanceManager.CreateFromFile(fullPathForSystemContainer, out fileSystemId);

                DisplayBrowserViewModel(fileSystemId, fileSystem);
            }
            catch (FileSystemCreationFailedException exception)
            {
                _userInteractionService.ShowMessage(exception.Message);
            }
        }

        public void CloseDown()
        {
            // Note: не закрываемся, пока есть задачки в окнах.

            Application.Current.Shutdown();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}