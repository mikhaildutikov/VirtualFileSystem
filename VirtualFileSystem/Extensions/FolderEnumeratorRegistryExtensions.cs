using System;
using System.Collections.Generic;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Extensions
{
    internal static class FolderEnumeratorRegistryExtensions
    {
        public static void InvalidateEnumeratorsFor(this IFolderEnumeratorRegistry enumeratorRegistry, IEnumerable<FolderNode> folders)
        {
            if (enumeratorRegistry == null) throw new ArgumentNullException("enumeratorRegistry");
            if (folders == null) throw new ArgumentNullException("folders");

            foreach (FolderNode folderNode in folders)
            {
                enumeratorRegistry.InvalidateEnumeratorsForFolder(folderNode.Id);
            }
        }

        public static void InvalidateEnumeratorsFor(this IFolderEnumeratorRegistry enumeratorRegistry, IEnumerable<Guid> idsOfFolders)
        {
            if (enumeratorRegistry == null) throw new ArgumentNullException("enumeratorRegistry");
            if (idsOfFolders == null) throw new ArgumentNullException("idsOfFolders");

            foreach (Guid id in idsOfFolders)
            {
                enumeratorRegistry.InvalidateEnumeratorsForFolder(id);
            }
        }
    }
}