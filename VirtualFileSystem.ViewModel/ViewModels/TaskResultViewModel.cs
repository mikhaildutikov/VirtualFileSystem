using System;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    /// <summary>
    /// TODO: сделать более generic. Здесь слишком сильная привязка к семантике файловых операций.
    /// </summary>
    internal class TaskResultViewModel
    {
        public static TaskResultViewModel FromFolderTaskResult(FolderTaskResult folderTaskResult)
        {
            if (folderTaskResult == null) throw new ArgumentNullException("folderTaskResult");

            return new TaskResultViewModel(
                folderTaskResult.SourceFolder.FullPath,
                folderTaskResult.ExecutedSuccessfully ? folderTaskResult.DestinationFolder.FullPath : String.Empty,
                folderTaskResult.ExecutedSuccessfully,
                folderTaskResult.Error);
        }

        public static TaskResultViewModel FromFileTaskResult(FileTaskResult folderTaskResult)
        {
            if (folderTaskResult == null) throw new ArgumentNullException("folderTaskResult");

            return new TaskResultViewModel(
                folderTaskResult.SourceFile.FullPath,
                folderTaskResult.ExecutedSuccessfully ? folderTaskResult.DestinationFile.FullPath : String.Empty,
                folderTaskResult.ExecutedSuccessfully,
                folderTaskResult.Error);
        }

        public TaskResultViewModel(string source, string destination, bool executedSuccesfully, string error)
        {
            Source = source;
            Destination = destination;
            CompletedSuccessfully = executedSuccesfully;
            Error = error;
        }

        public string Source { get; private set; }
        public string Destination { get; private set; }
        public bool CompletedSuccessfully { get; private set; }
        public string Error { get; private set; }
    }
}