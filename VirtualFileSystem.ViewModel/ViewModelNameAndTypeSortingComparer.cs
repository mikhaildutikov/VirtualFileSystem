using System;
using System.Collections.Generic;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel
{
    /// <summary>
    /// Особый вид Comparer-a. Папки всегда пропускает вперед.
    /// TODO: unit-test me!
    /// </summary>
    internal class ViewModelNameAndTypeSortingComparer : IComparer<FileSystemArtifactViewModel>
    {
        public int Compare(FileSystemArtifactViewModel artifact1, FileSystemArtifactViewModel artifact2)
        {
            if (artifact1 == null) throw new ArgumentNullException("artifact1");
            if (artifact2 == null) throw new ArgumentNullException("artifact2");

            FolderViewModel artifact1AsFolder = artifact1 as FolderViewModel;
            FolderViewModel artifact2AsFolder = artifact2 as FolderViewModel;

            bool artifact1IsAFolder = (artifact1AsFolder != null);
            bool artifact2IsAFolder = (artifact2AsFolder != null);

            if ((artifact2IsAFolder && artifact1IsAFolder) || ((!artifact2IsAFolder) && (!artifact1IsAFolder)))
            {
                return StringComparer.CurrentCulture.Compare(artifact1.Name, artifact2.Name);
            }
            else if (artifact1IsAFolder && (!artifact2IsAFolder))
            {
                return -1;
            }
            else if (!artifact1IsAFolder && (artifact2IsAFolder))
            {
                return 1;
            }

            throw new NotSupportedException();
        }
    }
}