using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    public partial class TaskResultView : Window
    {
        internal TaskResultView(IEnumerable<TaskResultViewModel> resultsOfTasks)
        {
            if (resultsOfTasks == null) throw new ArgumentNullException("resultsOfTasks");

            InitializeComponent();

            this.DataContext =
                new ReadOnlyObservableCollection<TaskResultViewModel>(
                    new ObservableCollection<TaskResultViewModel>(resultsOfTasks));
        }
    }
}