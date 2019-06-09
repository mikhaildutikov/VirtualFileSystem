using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel
{
    internal interface IUserInteractionService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pickerTitle"></param>
        /// <returns></returns>
        /// <returns>Путь, указывающий выбранный файл или, увы, null - если ничего не выбрано.</returns>
        string PickAFile(string pickerTitle);

        bool GetNewArtifactPropertiesFromUser(NewArtifactViewModel viewModeToUpdate);
        bool GetNewArtifactNamesFromUser(NewArtifactNameViewModel newNameViewModel);
        void ShowMessage(string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pickedTitle"></param>
        /// <returns>Путь, указывающий выбранную папку или, увы, null - если ничего не выбрано.</returns>
        string PickAFolder(string pickedTitle);

        bool GetVirtualFolderPath(VirtualFolderPathViewModel viewModel);
    }
}