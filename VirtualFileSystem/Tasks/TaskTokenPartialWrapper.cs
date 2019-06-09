using System;
using System.ComponentModel;

namespace VirtualFileSystem
{
    internal class TaskTokenPartialWrapper : IFileSystemCancellableTaskToken
    {
        private readonly IFileSystemCancellableTaskToken _token;

        public TaskTokenPartialWrapper(IFileSystemCancellableTaskToken token)
        {
            _token = token;
        }

        public void Cancel()
        {
            _token.Cancel();
        }

        public bool HasBeenCancelled
        {
            get { return _token.HasBeenCancelled; }
        }

        public void ReportProgressChange(int percentage)
        {
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
    }
}