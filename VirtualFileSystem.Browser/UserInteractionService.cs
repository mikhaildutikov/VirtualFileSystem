using System;
using System.Windows;
using System.Windows.Forms;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace VirtualFileSystem.Browser
{
    internal class UserInteractionService : IUserInteractionService
    {
        private const string ApplicationName = "Virtual File System Browser";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pickedTitle"></param>
        /// <returns></returns>
        /// <returns>Путь, указывающий выбранную папку или, увы, null - если ничего не выбрано.</returns>
        public string PickAFolder(string pickedTitle)
        {
            var folderBrowserDialog = new FolderBrowserDialog() { ShowNewFolderButton = false, Description = pickedTitle };

            DialogResult dialogResult = folderBrowserDialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                return folderBrowserDialog.SelectedPath;
            }

            return null;
        }

        public bool GetVirtualFolderPath(VirtualFolderPathViewModel viewModel)
        {
            var view = new VirtualFolderSelectionView(viewModel);

            var gotIt = view.ShowDialog();

            return (gotIt == true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pickerTitle"></param>
        /// <returns></returns>
        /// <returns>Путь, указывающий выбранный файл или, увы, null - если ничего не выбрано.</returns>
        public string PickAFile(string pickerTitle)
        {
            var openFileDialog = new OpenFileDialog {CheckFileExists = false, Title = pickerTitle};

            Nullable<bool> gotTheFile = openFileDialog.ShowDialog();

            if ((!gotTheFile.HasValue) || (gotTheFile.Value == false))
            {
                return null;
            }

            return openFileDialog.FileName;
        }

        public bool GetNewArtifactPropertiesFromUser(NewArtifactViewModel viewModeToUpdate)
        {
            var newArtifactView = new NewArtifactView(viewModeToUpdate);

            return ShowWindowAsDialog(newArtifactView);
        }

        private static bool ShowWindowAsDialog(Window windowToShow)
        {
            bool? usedOkayedOutTheDialog = windowToShow.ShowDialog();

            bool gotIt = (usedOkayedOutTheDialog == true);

            return gotIt;
        }

        public bool GetNewArtifactNamesFromUser(NewArtifactNameViewModel newNameViewModel)
        {
            var newArtifactNameView = new NewArtifactNameView(newNameViewModel);

            return ShowWindowAsDialog(newArtifactNameView);
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message, ApplicationName, MessageBoxButton.OK);
        }
    }
}