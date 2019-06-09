using System;
using System.ComponentModel;
using System.Threading;

namespace VirtualFileSystem
{
    internal class FileSystemCancellableTaskToken : IFileSystemCancellableTaskToken
    {
        private volatile bool _cancelled;

        public void Cancel()
        {
            _cancelled = true;
        }

        public bool HasBeenCancelled
        {
            get { return _cancelled; }
        }

        public void ReportProgressChange(int percentage)
        {
            if ((percentage < 0) || (percentage > 100))
            {
                throw new ArgumentOutOfRangeException("percentage");
            }

            EventHandler<ProgressChangedEventArgs> eventHandler = Interlocked.CompareExchange(ref ProgressChanged, null, null);

            if (eventHandler != null)
            {
                eventHandler.Invoke(this, new ProgressChangedEventArgs(percentage, null));
            }
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
    }
}