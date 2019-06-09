using System;
using System.ComponentModel;

namespace VirtualFileSystem
{
    internal interface IFileSystemCancellableTaskToken
    {
        void Cancel();
        bool HasBeenCancelled { get; }
        void ReportProgressChange(int percentage);
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
    }
}