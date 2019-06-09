using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel
{
    internal interface INewArtifactCreator
    {
        void CreateNewArtifact(NewArtifactViewModel newArtifactViewModel);
    }
}