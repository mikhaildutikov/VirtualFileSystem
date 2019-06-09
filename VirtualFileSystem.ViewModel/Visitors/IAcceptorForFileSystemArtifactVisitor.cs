namespace VirtualFileSystem.ViewModel.Visitors
{
    internal interface IAcceptorForFileSystemArtifactVisitor
    {
        void Accept(IFileSystemArtifactViewModelVisitor visitor);
    }
}