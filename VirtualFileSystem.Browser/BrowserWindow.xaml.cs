using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    public partial class BrowserWindow : Window
    {
        private readonly BrowserWindowViewModel _viewModel;

        internal BrowserWindow(BrowserWindowViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException("viewModel");

            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = viewModel;
        }

        private void QuitButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel.Quit(true);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // Note: никакой подобной логики здесь быть не должно. Место этому во ViewModel и коллабораторах.
            if (!_viewModel.TaskCounter.NoTasksRunning)
            {
                e.Cancel = true;

                MessageBox.Show("Нельзя закрыть окно пока выполняются поставленные вами в очередь задачи. Дождитесь, пожалуйста, завершения выполняющихся задач или завершите их.",
                                "Virtual File System Browser", MessageBoxButton.OK);
            }
            else
            {
                _viewModel.Quit(false);
            }
        }

        private void CreateNewFileMenuItemClick(object sender, RoutedEventArgs e)
        {
            _viewModel.CreateNewFile();
        }

        private void CreateFolderMenuItemClick(object sender, RoutedEventArgs e)
        {
            _viewModel.CreateNewFolder();
        }

        private void FilesAndFoldersListBoxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Note: да, думаю, InvalidCastException в этом месте должен повалить процесс
            _viewModel.CurrentlySelectedArtifact = (FileSystemArtifactViewModel)this.FilesAndFoldersListBox.SelectedItem;
        }

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdateFolderContentsChangingFolder();
        }

        private void DeleteSelectedMenuItemClick(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteSelectedArtifact();
        }

        private void RenameSelectedMenuItemClick(object sender, RoutedEventArgs e)
        {
            _viewModel.RenameSelectedArtifact();
        }

        private void FilesAndFoldersListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _viewModel.TryNavigatingToFolderIfFolderIsSelected();
        }

        private void ImportFilesFromLocalSystemMenuItemClick(object sender, RoutedEventArgs e)
        {
            _viewModel.StartImportingFilesFromLocalSystem();
        }

        private void CurrentLocationTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter) || (e.Key == Key.Return))
            {
                _viewModel.UpdateFolderContentsChangingFolder();
            }
        }

        private void CancelTaskButtonClick(object sender, RoutedEventArgs e)
        {
            TaskViewModel viewModel = ExtractTaskViewModel(sender);

            viewModel.CancelTask();
        }

        private void ViewResultButtonClick(object sender, RoutedEventArgs e)
        {
            TaskViewModel viewModel = ExtractTaskViewModel(sender);

            viewModel.ShowResult();
        }

        private TaskViewModel ExtractTaskViewModel(object viewModelContainer)
        {
            Control senderAsControl = (Control)viewModelContainer;

            return (TaskViewModel)senderAsControl.DataContext;
        }

        private void DeleteResultButtonClick(object sender, RoutedEventArgs e)
        {
            TaskViewModel viewModel = ExtractTaskViewModel(sender);

            _viewModel.DeleteTask(viewModel);
        }

        private void ImportFilesFromVirtualSystemMenuItemClick(object sender, RoutedEventArgs e)
        {
            _viewModel.StartImportingFilesFromVirtualSystem();
        }

        private void MoveSelectedMenuItemClick(object sender, RoutedEventArgs e)
        {
            _viewModel.MoveSelectedItem();
        }

        private void CopySelectedMenuItemClick(object sender, RoutedEventArgs e)
        {
            _viewModel.CopySelectedItem();
        }

        private void SearchFileButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel.KickOffFileSearch();
        }
    }
}