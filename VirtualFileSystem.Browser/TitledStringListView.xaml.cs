using System;
using System.Windows;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    public partial class TitledStringListView : Window
    {
        internal TitledStringListView(TitledStringListViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException("viewModel");

            InitializeComponent();

            this.DataContext = viewModel;
        }
    }
}