using System.ComponentModel;

namespace VirtualFileSystem.ViewModel
{
    internal class TaskCounter : INotifyPropertyChanged
    {
        private readonly object _stateChangeCriticalSection = new object();
        private int _numberOfOutstandingTasks;
        private bool _noTasksRunning;

        public TaskCounter()
        {
            this.ReevaluateTheProperty();
        }

        public void IncreaseNumberOfOutstandingTasks()
        {
            lock (_stateChangeCriticalSection) //full fence
            {
                _numberOfOutstandingTasks++;

                ReevaluateTheProperty();
            }
        }

        private void ReevaluateTheProperty()
        {
            this.NoTasksRunning = (_numberOfOutstandingTasks == 0);
        }

        public void DecreaseNumberOfOutstandingTasks()
        {
            lock (_stateChangeCriticalSection) //full fence
            {
                _numberOfOutstandingTasks--;

                ReevaluateTheProperty();
            }
        }

        public bool NoTasksRunning
        {
            get { return _noTasksRunning; }
            private set
            {
                _noTasksRunning = value;
                EventRaiser.RaisePropertyChangedEvent(PropertyChanged, this, "NoTasksRunning");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}