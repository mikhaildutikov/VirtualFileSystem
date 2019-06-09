using System;
using System.Windows;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    public partial class VirtualFolderSelectionView : Window
    {
        internal VirtualFolderSelectionView(VirtualFolderPathViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException("viewModel");
            InitializeComponent();

            this.DataContext = viewModel;
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}