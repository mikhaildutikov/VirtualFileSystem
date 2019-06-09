using System;
using System.Threading;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Toolbox.Extensions;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel.Visitors
{
    internal class ArtifactCopyingVisitor : IFileSystemArtifactViewModelVisitor
    {
        private readonly VirtualFileSystem _fileSystem;
        private readonly IUserInteractionService _userInteractionService;
        private readonly IPathValidator _pathValidator;
        private TaskViewModel _taskViewModel;
        private readonly IApplicationController _applicationController;
        private readonly TaskCounter _taskCounter;

        public ArtifactCopyingVisitor(VirtualFileSystem fileSystem, IUserInteractionService userInteractionService, IPathValidator pathValidator, IApplicationController applicationController, TaskCounter taskCounter)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (userInteractionService == null) throw new ArgumentNullException("userInteractionService");
            if (pathValidator == null) throw new ArgumentNullException("pathValidator");
            if (applicationController == null) throw new ArgumentNullException("applicationController");
            if (taskCounter == null) throw new ArgumentNullException("taskCounter");

            _fileSystem = fileSystem;
            _applicationController = applicationController;
            _taskCounter = taskCounter;
            _userInteractionService = userInteractionService;
            _pathValidator = pathValidator;
        }

        public TaskViewModel TaskViewModel
        {
            get { return _taskViewModel; }
        }

        public void VisitFile(FileViewModel fileViewModel)
        {
            if (fileViewModel == null) throw new ArgumentNullException("fileViewModel");

            var pathViewModel = new VirtualFolderPathViewModel(_pathValidator) { Path = VirtualFileSystem.Root };

            if (_userInteractionService.GetVirtualFolderPath(pathViewModel))
            {
                var taskToken = new FileSystemCancellableTaskToken();

                _taskViewModel = new TaskViewModel(
                    "Копирование файла \"{0}\" по следующему пути \"{1}\"".FormatWith(fileViewModel.FullPath, pathViewModel.Path),
                    _applicationController,
                    taskToken);

                _taskCounter.IncreaseNumberOfOutstandingTasks();

                ThreadPool.QueueUserWorkItem(
                    delegate
                     {
                         try
                         {
                             _fileSystem.CopyFile(fileViewModel.FullPath, pathViewModel.Path, taskToken);
                             _taskViewModel.SetResult(new[]{new TaskResultViewModel(fileViewModel.FullPath, pathViewModel.Path, true, String.Empty)});
                         }
                         catch(TaskCancelledException exception)
                         {
                             this.SetCopyFileError(fileViewModel.FullPath, pathViewModel.Path, exception);
                         }
                         catch (FileNotFoundException exception)
                         {
                             this.SetCopyFileError(fileViewModel.FullPath, pathViewModel.Path, exception);
                         }
                         catch (FileLockedException exception)
                         {
                             this.SetCopyFileError(fileViewModel.FullPath, pathViewModel.Path, exception);
                         }
                         catch (InvalidPathException exception)
                         {
                             this.SetCopyFileError(fileViewModel.FullPath, pathViewModel.Path, exception);
                         }
                         catch (FileAlreadyExistsException exception)
                         {
                             this.SetCopyFileError(fileViewModel.FullPath, pathViewModel.Path, exception);
                         }
                         catch (FolderNotFoundException exception)
                         {
                             this.SetCopyFileError(fileViewModel.FullPath, pathViewModel.Path, exception);
                         }
                         catch (MaximumFileCountReachedException exception)
                         {
                             this.SetCopyFileError(fileViewModel.FullPath, pathViewModel.Path, exception);
                         }
                         catch (InsufficientSpaceException exception)
                         {
                             this.SetCopyFileError(fileViewModel.FullPath, pathViewModel.Path, exception);
                         }
                         finally
                         {
                             MarkTaskViewModelAsCompleted(_taskViewModel);
                             _taskCounter.DecreaseNumberOfOutstandingTasks();
                         }
                     });
            }
        }

        private void SetCopyFileError(string source, string destination, Exception error)
        {
            _taskViewModel.SetResult(new[] { new TaskResultViewModel(source, destination, false, error.Message) });
        }

        private static void MarkTaskViewModelAsCompleted(TaskViewModel taskViewModel)
        {
            taskViewModel.Completed = true;
            taskViewModel.ProgressPercentage = 100;
        }

        public void VisitFolder(FolderViewModel folderViewModel)
        {
            if (folderViewModel == null) throw new ArgumentNullException("folderViewModel");

            var pathViewModel = new VirtualFolderPathViewModel(_pathValidator) { Path = VirtualFileSystem.Root };

            if (_userInteractionService.GetVirtualFolderPath(pathViewModel))
            {
                var taskToken = new FileSystemCancellableTaskToken();

                _taskViewModel = new TaskViewModel(
                    "Копирование папки \"{0}\" по следующему пути \"{1}\"".FormatWith(folderViewModel.FullPath, pathViewModel.Path),
                    _applicationController,
                    taskToken);

                _taskCounter.IncreaseNumberOfOutstandingTasks();

                ThreadPool.QueueUserWorkItem(
                    delegate
                        {
                            try
                            {
                                var results = _fileSystem.CopyFolder(folderViewModel.FullPath, pathViewModel.Path, taskToken);

                                var viewModels = TaskViewModelConverter.CreateViewModelsFromResults(results);

                                _taskViewModel.SetResult(viewModels);
                            }
                            catch (FolderNotFoundException exception)
                            {
                                this.SetCopyFileError(folderViewModel.FullPath, pathViewModel.Path, exception);
                            }
                            catch (InvalidPathException exception)
                            {
                                this.SetCopyFileError(folderViewModel.FullPath, pathViewModel.Path, exception);
                            }
                            catch (MaximumFolderCountReachedException exception)
                            {
                                this.SetCopyFileError(folderViewModel.FullPath, pathViewModel.Path, exception);
                            }
                            catch (InsufficientSpaceException exception)
                            {
                                this.SetCopyFileError(folderViewModel.FullPath, pathViewModel.Path, exception);
                            }
                            finally
                            {
                                MarkTaskViewModelAsCompleted(_taskViewModel);
                                _taskCounter.DecreaseNumberOfOutstandingTasks();
                            }
                        });
            }
        }
    }
}