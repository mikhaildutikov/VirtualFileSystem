using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal class TaskViewModel : INotifyPropertyChanged
    {
        private readonly IApplicationController _applicationController;
        private readonly FileSystemCancellableTaskToken _taskToken;
        private int _progressPercentage;
        private bool _completed;
        private IEnumerable<TaskResultViewModel> _results;
        private bool _canCancel = true;

        public TaskViewModel(string taskName, IApplicationController applicationController, FileSystemCancellableTaskToken taskToken)
        {
            if (applicationController == null) throw new ArgumentNullException("applicationController");
            if (taskToken == null) throw new ArgumentNullException("taskToken");

            _applicationController = applicationController;
            _taskToken = taskToken;
            this.TaskName = taskName;

            _taskToken.ProgressChanged += TaskTokenProgressChanged;
        }

        internal void SetResult(IEnumerable<TaskResultViewModel> result)
        {
            if (_results != null)
            {
                throw new InvalidOperationException("Установить результат операции можно только однажды.");
            }

            _results = result;
        }

        void TaskTokenProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.ProgressPercentage = e.ProgressPercentage;
        }

        public string TaskName { get; private set; }
       
        public int ProgressPercentage
        {
            get { return _progressPercentage; }
            internal set
            {
                if ((value > 100) || (value < 0))
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _progressPercentage = value;

                EventRaiser.RaisePropertyChangedEvent(this.PropertyChanged, this, "ProgressPercentage");
            }
        }

        public bool CanCancel
        {
            get { return _canCancel; }
            private set
            {
                _canCancel = value;
                EventRaiser.RaisePropertyChangedEvent(this.PropertyChanged, this, "CanCancel");
            }
        }

        public bool Completed
        {
            get { return _completed; }
            internal set
            {
                _completed = value;

                if (_completed)
                {
                    this.CanCancel = false;
                }

                EventRaiser.RaisePropertyChangedEvent(this.PropertyChanged, this, "Completed");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ShowResult()
        {
            _applicationController.DisplayTaskResults(_results);
        }

        public void CancelTask()
        {
            if (this.CanCancel)
            {
                _taskToken.Cancel();
                this.CanCancel = false;
            }
        }
    }
}