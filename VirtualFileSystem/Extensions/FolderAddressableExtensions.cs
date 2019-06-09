using System;
using System.Linq;

namespace VirtualFileSystem.Extensions
{
    internal static class FolderAddressableExtensions
    {
        internal static int GetTotalFileCount(this FolderAddressable folder)
        {
            if (folder == null) throw new ArgumentNullException("folder");

            int countOfItems = folder.Files.Count();

            foreach (FolderAddressable subfolder in folder.Subfolders)
            {
                countOfItems += subfolder.GetTotalFileCount();
            }

            return countOfItems;
        }
    }
}