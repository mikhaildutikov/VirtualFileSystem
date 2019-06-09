using System;
using System.ComponentModel;

namespace VirtualFileSystem
{
    internal class NullFileSystemCancellableTaskToken : IFileSystemCancellableTaskToken
    {
        public void Cancel()
        {
        }

        public bool HasBeenCancelled
        {
            get { return false; }
        }

        public void ReportProgressChange(int percentage)
        {
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
    }
}