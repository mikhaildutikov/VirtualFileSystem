using System;
using System.Collections.Generic;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal static class TaskViewModelConverter
    {
        public static IEnumerable<TaskResultViewModel> CreateViewModelsFromResults(IEnumerable<FileSystemTaskResult> results)
        {
            if (results == null) throw new ArgumentNullException("results");

            var viewModels = new List<TaskResultViewModel>();

            foreach (FileSystemTaskResult result in results) // Note: Visitor бы
            {
                FolderTaskResult resultAsFolderResult = result as FolderTaskResult;

                if (resultAsFolderResult != null)
                {
                    viewModels.Add(TaskResultViewModel.FromFolderTaskResult(resultAsFolderResult));
                }
                else
                {
                    FileTaskResult resultAsFileResult = result as FileTaskResult;

                    if (resultAsFileResult != null)
                    {
                        viewModels.Add(TaskResultViewModel.FromFileTaskResult(resultAsFileResult));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }

            return viewModels;
        }
    }   
}