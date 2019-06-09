namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal class NewArtifactViewModel : NewArtifactNameViewModel
    {
        public NewArtifactViewModel(IFileSystemArtifactNamesValidator namesValidator) : base(namesValidator)
        {
        }

        public string Location { get; set; }

        public ArtifactKind ArtifactKind { get; set; }
    }
}