using System;
using System.Collections.Generic;
using System.Threading;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel
{
    internal class ArtifactImporter : IArtifactImporter
    {
        private readonly VirtualFileSystem _fileSystem;
        private readonly IVirtualFileSystemInstanceManager _virtualFileSystemInstanceManager;
        private readonly TaskCounter _taskCounter;

        public ArtifactImporter(VirtualFileSystem fileSystem, IVirtualFileSystemInstanceManager virtualFileSystemInstanceManager, TaskCounter taskCounter)
        {
            if (fileSystem == null) throw new ArgumentNullException("fileSystem");
            if (virtualFileSystemInstanceManager == null)
                throw new ArgumentNullException("virtualFileSystemInstanceManager");
            if (taskCounter == null) throw new ArgumentNullException("taskCounter");

            _fileSystem = fileSystem;
            _virtualFileSystemInstanceManager = virtualFileSystemInstanceManager;
            _taskCounter = taskCounter;
        }

        public void KickOffVirtualSystemImport(string fileSystemContainer, string destinationPath, TaskViewModel taskViewModel, FileSystemCancellableTaskToken taskToken)
        {
            _taskCounter.IncreaseNumberOfOutstandingTasks();

            ThreadPool.QueueUserWorkItem(
                delegate
                    {
                        VirtualFileSystem sourceFileSystem = null;

                        try
                        {
                            string fileSystemId;
                            sourceFileSystem = _virtualFileSystemInstanceManager.CreateFromFile(fileSystemContainer, out fileSystemId);

                            if (Object.ReferenceEquals(_fileSystem, sourceFileSystem))
                            {
                                taskViewModel.SetResult(new List<TaskResultViewModel> { new TaskResultViewModel(VirtualFileSystem.Root, null, false, "Рекурсивный импорт (из системы в себя) запрещен.") });

                                MarkTaskViewModelAsCompleted(taskViewModel);
                                return;
                            }

                            try
                            {
                                var results =
                                    _fileSystem.ImportFolderFromVirtualFileSystem(
                                        sourceFileSystem, VirtualFileSystem.Root, destinationPath,
                                        taskToken);

                                var viewModels = TaskViewModelConverter.CreateViewModelsFromResults(results);

                                taskViewModel.SetResult(viewModels);
                            }
                            catch(FolderNotFoundException exception)
                            {
                                SetTaskModelFromError(taskViewModel, exception);
                            }
                            catch (InsufficientSpaceException exception)
                            {
                                SetTaskModelFromError(taskViewModel, exception);
                            }
                            catch (CannotGetImportedFolderStructureException exception)
                            {
                                SetTaskModelFromError(taskViewModel, exception);
                            }
                            finally
                            {
                                MarkTaskViewModelAsCompleted(taskViewModel);
                            }
                        }
                        catch (FileSystemCreationFailedException exception)
                        {
                            taskViewModel.SetResult(new List<TaskResultViewModel> { new TaskResultViewModel(VirtualFileSystem.Root, null, false, exception.Message) });

                            MarkTaskViewModelAsCompleted(taskViewModel);
                        }
                        finally
                        {
                            if (sourceFileSystem != null)
                            {
                                _virtualFileSystemInstanceManager.ReportThatSystemIsNoLongerNeeded(sourceFileSystem);
                            }

                            _taskCounter.DecreaseNumberOfOutstandingTasks();
                        }
                    });
        }

        private static void SetTaskModelFromError(TaskViewModel taskViewModel, Exception exception)
        {
            taskViewModel.SetResult(new List<TaskResultViewModel> { new TaskResultViewModel(VirtualFileSystem.Root, null, false, exception.Message) });
        }

        public void KickOffRealFileSystemImport(TaskViewModel taskViewModel, string sourceFolder, string destinationFolderPath, FileSystemCancellableTaskToken taskToken)
        {
            _taskCounter.IncreaseNumberOfOutstandingTasks();

            ThreadPool.QueueUserWorkItem(
                delegate
                {
                    try
                    {
                        var results = _fileSystem.ImportFolderFromRealFileSystem(
                            sourceFolder,
                            destinationFolderPath,
                            taskToken);

                        var viewModels = TaskViewModelConverter.CreateViewModelsFromResults(results);
                        
                        taskViewModel.SetResult(viewModels);
                    }
                    catch(FolderNotFoundException exception)
                    {
                        SetTaskModelFromError(sourceFolder, taskViewModel, exception);
                    }
                    catch (InsufficientSpaceException exception)
                    {
                        SetTaskModelFromError(sourceFolder, taskViewModel, exception);
                    }
                    catch (CannotGetImportedFolderStructureException exception)
                    {
                        SetTaskModelFromError(sourceFolder, taskViewModel, exception);
                    }
                    finally
                    {
                        MarkTaskViewModelAsCompleted(taskViewModel);
                        _taskCounter.DecreaseNumberOfOutstandingTasks();
                    }
                });
        }

        private static void SetTaskModelFromError(string sourceFolder, TaskViewModel taskViewModel, Exception exception)
        {
            taskViewModel.SetResult(new List<TaskResultViewModel> { new TaskResultViewModel(sourceFolder, null, false, exception.Message) });
        }

        private static void MarkTaskViewModelAsCompleted(TaskViewModel taskViewModel)
        {
            taskViewModel.Completed = true;
            taskViewModel.ProgressPercentage = 100;
        }
    }
}