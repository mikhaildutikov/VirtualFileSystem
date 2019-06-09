using System;
using System.Threading;
using System.Windows;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    public partial class BrowserHubWindowView : Window
    {
        private readonly BrowserHubWindowViewModel _viewModel;

        internal BrowserHubWindowView(BrowserHubWindowViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException("viewModel");

            _viewModel = viewModel;
            InitializeComponent();

            this.DataContext = viewModel;
        }

        private void BrowseNewDiskButtonClick(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(
                delegate
                    {
                        _viewModel.BrowseNewSystemCreatedFormattingTheFile();
                    }
                );
        }

        private void BrowseExistingDiskButtonClick(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(
                delegate
                {
                    _viewModel.BrowseExistingSystem();
                }
            );
        }

        private void ShutdownButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel.CloseDown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!_viewModel.VirtualFileSystemInstanceManager.IsEmpty)
            {
                e.Cancel = true;

                MessageBox.Show(
                    "В текущей версии для завершения работы необходимо вручную закрыть все окна приложения.\r\nПриношу извинения за неудобства.",
                    "Virtual File System Browser");
            }
        }
    }
}