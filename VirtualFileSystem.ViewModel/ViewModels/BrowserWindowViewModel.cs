using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using VirtualFileSystem.Toolbox.Extensions;
using VirtualFileSystem.ViewModel.Visitors;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    /// <summary>
    /// Note: не реализует IDisposable в полной мере, как остальные Disposable-классы библиотеки VirtualFileSystem.
    /// Большой класс - дорефакторить бы.
    /// </summary>
    internal class BrowserWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private string _currentFolderPath;
        private readonly VirtualFileSystem _virtualSystemCurrentlyBrowsed; // Note: надо сделать адаптер и интерфейс/абстрактный класс для VirtualFileSystem - так проще будет тестировать. Сейчас я этого делать не стану.
        private readonly IApplicationController _applicationController;
        private readonly IComparer<FileSystemArtifactViewModel> _viewModelSortingComparer;
        private readonly IUserInteractionService _userInteractionService;
        private readonly string _diskBrowsedLocation;
        private readonly IEqualityComparer<string> _fileSystemArtifactNameComparer;
        private readonly IFileSystemArtifactViewModelVisitor _deletingVisitor;
        private string _desiredCurrentFolderPath;
        private bool _canRenameOrDeleteFileOrFolder;
        private FileSystemArtifactViewModel _currentlySelectedArtifact;
        private readonly IFileSystemArtifactViewModelVisitor _renamingVisitor;
        private readonly INewArtifactCreator _newArtifactCreator;
        private readonly IVirtualFileSystemInstanceManager _virtualFileSystemInstanceManager;
        private readonly IFileSystemArtifactNamesValidator _artifactNamesValidator;
        private readonly ObservableCollection<TaskViewModel> _tasks;
        private readonly IFileSystemArtifactViewModelVisitor _movingVisitor;
        private readonly IArtifactImporter _artifactImporter;
        private readonly TaskCounter _taskCounter;
        private readonly IDispatcher _dispatcher;
        private bool _isRefreshEnabled;

        public BrowserWindowViewModel(
            VirtualFileSystem virtualSystemCurrentlyBrowsed,
            IApplicationController applicationController,
            IComparer<FileSystemArtifactViewModel> viewModelSortingComparer,
            IUserInteractionService userInteractionService,
            string diskBrowsedLocation,
            IEqualityComparer<string> fileSystemArtifactNameComparer,
            IFileSystemArtifactViewModelVisitor deletingVisitor,
            IFileSystemArtifactNamesValidator artifactNamesValidator,
            IFileSystemArtifactViewModelVisitor renamingVisitor,
            INewArtifactCreator newArtifactCreator,
            IVirtualFileSystemInstanceManager virtualFileSystemInstanceManager,
            IFileSystemArtifactViewModelVisitor movingVisitor,
            IArtifactImporter artifactImporter,
            TaskCounter taskCounter,
            IDispatcher dispatcher)
        {
            if (virtualSystemCurrentlyBrowsed == null) throw new ArgumentNullException("virtualSystemCurrentlyBrowsed");
            if (applicationController == null) throw new ArgumentNullException("applicationController");
            if (viewModelSortingComparer == null) throw new ArgumentNullException("viewModelSortingComparer");
            if (userInteractionService == null) throw new ArgumentNullException("userInteractionService");
            if (diskBrowsedLocation == null) throw new ArgumentNullException("diskBrowsedLocation");
            if (fileSystemArtifactNameComparer == null)
                throw new ArgumentNullException("fileSystemArtifactNameComparer");
            if (deletingVisitor == null) throw new ArgumentNullException("deletingVisitor");
            if (artifactNamesValidator == null) throw new ArgumentNullException("artifactNamesValidator");
            if (renamingVisitor == null) throw new ArgumentNullException("renamingVisitor");
            if (newArtifactCreator == null) throw new ArgumentNullException("newArtifactCreator");
            if (virtualFileSystemInstanceManager == null)
                throw new ArgumentNullException("virtualFileSystemInstanceManager");
            if (movingVisitor == null) throw new ArgumentNullException("movingVisitor");
            if (artifactImporter == null) throw new ArgumentNullException("artifactImporter");
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            this.CurrentFolderContents = new ObservableCollection<FileSystemArtifactViewModel>();
            _virtualSystemCurrentlyBrowsed = virtualSystemCurrentlyBrowsed;
            _taskCounter = taskCounter;
            _dispatcher = dispatcher;
            _artifactImporter = artifactImporter;
            _movingVisitor = movingVisitor;
            _renamingVisitor = renamingVisitor;
            _newArtifactCreator = newArtifactCreator;
            _virtualFileSystemInstanceManager = virtualFileSystemInstanceManager;
            _userInteractionService = userInteractionService;
            _diskBrowsedLocation = diskBrowsedLocation;
            _fileSystemArtifactNameComparer = fileSystemArtifactNameComparer;
            _deletingVisitor = deletingVisitor;
            _artifactNamesValidator = artifactNamesValidator;
            _applicationController = applicationController;
            _viewModelSortingComparer = viewModelSortingComparer;

            this.CurrentFolderPath = VirtualFileSystem.Root;

            _tasks = new ObservableCollection<TaskViewModel>();

            this.Tasks = new ReadOnlyObservableCollection<TaskViewModel>(_tasks);

            this.RefreshCurrentFolderContentsFallingBackToRootIfNecessary();

            this.FileSearchPattern = new FileSearchPatternViewModel();

            this.IsRefreshEnabled = true;
        }

        public string DiskBrowsedLocation
        {
            get { return _diskBrowsedLocation; }
        }

        public string DesiredCurrentFolderPath
        {
            get { return _desiredCurrentFolderPath; }
            set
            {
                _desiredCurrentFolderPath = value;
                this.NotifyInterestedPartiesOfPropertyChange("DesiredCurrentFolderPath");
            }
        }

        public TaskCounter TaskCounter
        {
            get { return _taskCounter; }
        }

        public FileSearchPatternViewModel FileSearchPattern { get; private set; }

        public string CurrentFolderPath
        {
            get
            {
                return _currentFolderPath;
            }
            private set
            {
                _currentFolderPath = value;
                _desiredCurrentFolderPath = value;

                this.NotifyInterestedPartiesOfPropertyChange("CurrentFolderPath");
            }
        }

        public ReadOnlyObservableCollection<TaskViewModel> Tasks { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        private IEnumerable<FileSystemArtifactViewModel> GetContentsOfFolder(string folderPath)
        {
            var viewModels = new List<FileSystemArtifactViewModel>();

            ReadOnlyCollection<FolderInfo> folders = _virtualSystemCurrentlyBrowsed.GetAllFoldersFrom(folderPath);

            foreach (var folderInfo in folders)
            {
                viewModels.Add(FolderViewModel.FromFolderInfo(folderInfo));
            }

            var filesProvider = (IFilesAndFoldersProvider)_virtualSystemCurrentlyBrowsed;

            var files = filesProvider.GetAllFilesFrom(folderPath);

            foreach (var fileInfo in files)
            {
                viewModels.Add(FileViewModel.FromFileInfo(fileInfo));
            }

            viewModels.Sort(_viewModelSortingComparer); // Note: хотя интерфейсы (сортирующий collaborator указан через интерфейс) нам ничего не гарантируют, сортировка здесь case-sensitive. Так следует делать для строк, показываемых пользователю.

            return viewModels;
        }

        private void NotifyInterestedPartiesOfPropertyChange(string propertyName)
        {
            EventRaiser.RaisePropertyChangedEvent(this.PropertyChanged, this, propertyName);
        }

        public ObservableCollection<FileSystemArtifactViewModel> CurrentFolderContents { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public void Quit(bool closeDownTheWindow)
        {
            _applicationController.QuitDisplaying(this, closeDownTheWindow);
        }
       
        public void Dispose()
        {
            _virtualFileSystemInstanceManager.ReportThatSystemIsNoLongerNeeded(_virtualSystemCurrentlyBrowsed);
        }

        public void CreateNewFile()
        {
            var viewModel = new NewArtifactViewModel(_artifactNamesValidator) {Location = this.CurrentFolderPath, Name = "БезИмени", ArtifactKind = ArtifactKind.File};
            
            if (_userInteractionService.GetNewArtifactPropertiesFromUser(viewModel))
            {
                CreateNewArtifact(viewModel);
            }
        }

        public FileSystemArtifactViewModel CurrentlySelectedArtifact
        {
            get { return _currentlySelectedArtifact; }
            set
            {
                _currentlySelectedArtifact = value;

                this.CanRenameOrDeleteFileOrFolder = (_currentlySelectedArtifact != null);
            }
        }

        private void CreateNewArtifact(NewArtifactViewModel newArtifactViewModel)
        {
            _newArtifactCreator.CreateNewArtifact(newArtifactViewModel);
            this.RefreshCurrentFolderContentsFallingBackToRootIfNecessary();
        }

        public void CreateNewFolder()
        {
            var viewModel = new NewArtifactViewModel(_artifactNamesValidator) { Location = _currentFolderPath, Name = "БезИмени", ArtifactKind = ArtifactKind.Folder };

            if (_userInteractionService.GetNewArtifactPropertiesFromUser(viewModel))
            {
                CreateNewArtifact(viewModel);
            }
        }

        public bool CanRenameOrDeleteFileOrFolder
        {
            get { return _canRenameOrDeleteFileOrFolder; }
            private set
            {
                _canRenameOrDeleteFileOrFolder = value;
                this.NotifyInterestedPartiesOfPropertyChange("CanRenameOrDeleteFileOrFolder");
            }
        }

        public int FreeSpaceInBytes
        {
            get { return _virtualSystemCurrentlyBrowsed.FreeSpaceInBytes; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderPath"></param>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="InvalidPathException"></exception>
        private void GetAndSetAsCurrentContentsOfGivenFolder(string folderPath)
        {
            IEnumerable<FileSystemArtifactViewModel> viewModels = this.GetContentsOfFolder(folderPath);
            this.SetCurrentFolderContents(viewModels);
        }

        private void RefreshCurrentFolderContentsFallingBackToRootIfNecessary()
        {
            try
            {
                this.GetAndSetAsCurrentContentsOfGivenFolder(CurrentFolderPath);
            }
            catch (FolderNotFoundException)
            {
                this.DesiredCurrentFolderPath = VirtualFileSystem.Root;
                this.GetAndSetAsCurrentContentsOfGivenFolder(VirtualFileSystem.Root);
            }
            catch (InvalidPathException)
            {
                this.DesiredCurrentFolderPath = VirtualFileSystem.Root;
                this.GetAndSetAsCurrentContentsOfGivenFolder(VirtualFileSystem.Root);
            }
        }

        private void SetCurrentFolderContents(IEnumerable<FileSystemArtifactViewModel> viewModelsSorted)
        {
            _dispatcher.Invoke(new Action(delegate
                                              {
                                                  CurrentFolderContents.Clear();

                                                  foreach (FileSystemArtifactViewModel viewModel in viewModelsSorted)
                                                  {
                                                      CurrentFolderContents.Add(viewModel);
                                                  }

                                                  this.NotifyInterestedPartiesOfPropertyChange("FreeSpaceInBytes");
                                              }));
        }

        public bool IsRefreshEnabled
        {
            get { return _isRefreshEnabled; }
            private set
            {
                _isRefreshEnabled = value;
                this.NotifyInterestedPartiesOfPropertyChange("IsRefreshEnabled");
            }
        }

        public void UpdateFolderContentsChangingFolder()
        {
            this.IsRefreshEnabled = false;

            if (!_fileSystemArtifactNameComparer.Equals(CurrentFolderPath, DesiredCurrentFolderPath))
            {
                ThreadPool.QueueUserWorkItem(delegate
                                                 {
                                                     try
                                                     {
                                                         // сменить папку.
                                                         this.GetAndSetAsCurrentContentsOfGivenFolder(DesiredCurrentFolderPath);
                                                         this.CurrentFolderPath = DesiredCurrentFolderPath;
                                                     }
                                                     catch (FolderNotFoundException)
                                                     {
                                                         this.DesiredCurrentFolderPath = this.CurrentFolderPath;
                                                         this.RefreshCurrentFolderContentsFallingBackToRootIfNecessary();
                                                     }
                                                     catch (InvalidPathException)
                                                     {
                                                         this.DesiredCurrentFolderPath = this.CurrentFolderPath;
                                                         this.RefreshCurrentFolderContentsFallingBackToRootIfNecessary();
                                                     }
                                                     finally
                                                     {
                                                         IsRefreshEnabled = true;
                                                     }
                                                 });
            }
            else
            {
                ThreadPool.QueueUserWorkItem(delegate
                                                 {
                                                     try
                                                     {
                                                         this.RefreshCurrentFolderContentsFallingBackToRootIfNecessary();
                                                     }
                                                     finally
                                                     {
                                                         IsRefreshEnabled = true;
                                                     }
                                                 });
            }
        }

        public void DeleteSelectedArtifact()
        {
            this.CurrentlySelectedArtifact.Accept(_deletingVisitor);
            this.RefreshCurrentFolderContentsFallingBackToRootIfNecessary();
        }

        public void RenameSelectedArtifact()
        {
            this.CurrentlySelectedArtifact.Accept(_renamingVisitor);
            this.RefreshCurrentFolderContentsFallingBackToRootIfNecessary();
        }

        public void TryNavigatingToFolderIfFolderIsSelected()
        {
            if (this.CurrentlySelectedArtifact != null)
            {
                var selectedArtifactAsFolder = this.CurrentlySelectedArtifact as FolderViewModel;

                if (selectedArtifactAsFolder != null)
                {
                    this.DesiredCurrentFolderPath = selectedArtifactAsFolder.FullPath;
                    this.UpdateFolderContentsChangingFolder();
                }
            }
        }

        public void StartImportingFilesFromLocalSystem()
        {
            string folderPicked = _userInteractionService.PickAFolder("Выберите папку, все данные из которой следует проимпортировать");

            if (String.IsNullOrEmpty(folderPicked))
            {
                return;
            }
            
            var taskToken = new FileSystemCancellableTaskToken();

            var taskViewModel =
                new TaskViewModel("Импорт данных файловой системы компьютера из \"{0}\"".FormatWith(folderPicked),
                                  _applicationController, taskToken);

            _tasks.Insert(0, taskViewModel);

            string currentFolderPath = this.CurrentFolderPath;

            _artifactImporter.KickOffRealFileSystemImport(taskViewModel, folderPicked, currentFolderPath, taskToken);
        }

        public void DeleteTask(TaskViewModel modelToDelete)
        {
            _tasks.Remove(modelToDelete);
        }

        public void StartImportingFilesFromVirtualSystem()
        {
            var filePath =
                _userInteractionService.PickAFile(
                    "Выберите файл, содержащий файловую систему, из корня которой следует проимпортировать все данные");

            if (!String.IsNullOrEmpty(filePath))
            {
                var taskToken = new FileSystemCancellableTaskToken();

                var taskViewModel =
                    new TaskViewModel("Импорт данных из корня виртуальной файловой системы, работающей на файле \"{0}\"".FormatWith(filePath),
                                      _applicationController, taskToken);

                _tasks.Insert(0, taskViewModel);

                _artifactImporter.KickOffVirtualSystemImport(filePath, this.CurrentFolderPath, taskViewModel, taskToken);
            }
        }

        public void MoveSelectedItem()
        {
            this.CurrentlySelectedArtifact.Accept(_movingVisitor);
            this.RefreshCurrentFolderContentsFallingBackToRootIfNecessary();
        }

        public void CopySelectedItem()
        {
            var artifactCopyingVisitor =
                new ArtifactCopyingVisitor(_virtualSystemCurrentlyBrowsed, _userInteractionService, PathValidator.Default, _applicationController, _taskCounter);

            this.CurrentlySelectedArtifact.Accept(artifactCopyingVisitor);

            if (artifactCopyingVisitor.TaskViewModel != null)
            {
                _tasks.Insert(0, artifactCopyingVisitor.TaskViewModel);
            }
        }

        public void KickOffFileSearch()
        {
            string pattern = this.FileSearchPattern.Pattern;

            string folder = this.CurrentFolderPath;

            _taskCounter.IncreaseNumberOfOutstandingTasks();

            this.FileSearchPattern.IsSearchEnabled = false;

            ThreadPool.QueueUserWorkItem(
                delegate
                    {
                        try
                        {
                            bool gotEmAll = false;

                            var files = new List<FileInfo>();

                            while (!gotEmAll)
                            {
                                files.Clear();

                                using (IEnumerator<FileInfo> fileEnumerator = _virtualSystemCurrentlyBrowsed.EnumerateFilesUnderFolder(folder, pattern))
                                {
                                    try
                                    {
                                        while (fileEnumerator.MoveNext())
                                        {
                                            files.Add(fileEnumerator.Current);
                                        }

                                        gotEmAll = true;
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        gotEmAll = true;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // содержимое папки изменилось - не сдаемся.
                                        // Note: может привести и приводит к большой трате ресурсов, если, скажем, делать поиск по корню
                                        // и одновременно менять что-то в папках диска. Наверное, стоило делать итератор менее строгим, пусть бы он
                                        // сделался непохожим на другие итераторы (полный отрыв от изменений я сделал в копировании и импорте).
                                    }
                                }
                            }

                            _applicationController.PresentListOfStrings(
                                "Результаты рекурсивного поиска файлов по маске \"{0}\" в папке \"{1}\"".FormatWith(
                                    pattern, folder), files.Select(file => file.FullPath));
                        }
                        finally
                        {
                            this.FileSearchPattern.IsSearchEnabled = true;
                            _taskCounter.DecreaseNumberOfOutstandingTasks();
                        }
                    });
        }
    }
}