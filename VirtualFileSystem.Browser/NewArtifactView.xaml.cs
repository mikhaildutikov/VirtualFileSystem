using System;
using System.Windows;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    public partial class NewArtifactView : Window
    {
        private readonly NewArtifactViewModel _newArtifactViewModel;

        internal NewArtifactView(NewArtifactViewModel newArtifactViewModel)
        {
            if (newArtifactViewModel == null) throw new ArgumentNullException("newArtifactViewModel");

            _newArtifactViewModel = newArtifactViewModel;
            InitializeComponent();

            this.DataContext = newArtifactViewModel;
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