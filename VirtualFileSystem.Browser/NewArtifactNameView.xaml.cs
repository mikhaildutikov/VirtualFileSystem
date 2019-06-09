using System;
using System.Windows;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    public partial class NewArtifactNameView : Window
    {
        private readonly NewArtifactNameViewModel _viewModel;

        internal NewArtifactNameView(NewArtifactNameViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException("viewModel");
            _viewModel = viewModel;

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