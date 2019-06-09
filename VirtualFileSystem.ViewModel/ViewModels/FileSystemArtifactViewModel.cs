using System;
using VirtualFileSystem.ViewModel.Visitors;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal abstract class FileSystemArtifactViewModel : IAcceptorForFileSystemArtifactVisitor
    {
        protected internal FileSystemArtifactViewModel(string fullPath, string name, Guid id, DateTime creationTime)
        {
            if (String.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentNullException("fullPath");
            }

            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            FullPath = fullPath;
            Name = name;
            Id = id;
            CreationTime = creationTime;
        }

        public string FullPath { get; private set; }
        public string Name { get; private set; }
        public Guid Id { get; private set; }
        public DateTime CreationTime { get; private set; }
        public abstract void Accept(IFileSystemArtifactViewModelVisitor visitor);
    }
}