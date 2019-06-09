using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace
namespace VirtualFileSystem
// ReSharper restore CheckNamespace
{
    /// <summary>
    /// Папка файловой системы (еще одна из моделей для этого - содержащая иерархии объектов; остальные - плоские).
    /// </summary>
    public class FolderAddressable : Addressable
    {
        /// <summary>
        /// Конструирует новый экземпляр <see cref="FolderAddressable"/>.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="name"></param>
        /// <param name="subfolders"></param>
        /// <param name="files"></param>
        public FolderAddressable(string fullPath, string name, IEnumerable<FolderAddressable> subfolders, IEnumerable<FileAddressable> files) : base(fullPath, name)
        {
            if (subfolders == null) throw new ArgumentNullException("subfolders");
            if (files == null) throw new ArgumentNullException("files");

            Subfolders = subfolders;
            Files = files;
        }

        /// <summary>
        /// Принять указанного посетителя (Visitor, Gof).
        /// </summary>
        /// <param name="visitor"></param>
        public override void Accept(IAddressableObjectVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            visitor.VisitFolder(this);

            foreach (FileAddressable file in Files)
            {
                file.Accept(visitor);
            }

            foreach (FolderAddressable subfolder in Subfolders)
            {
                subfolder.Accept(visitor);
            }
        }

        /// <summary>
        /// Подпапки текущей папки.
        /// </summary>
        public IEnumerable<FolderAddressable> Subfolders { get; private set; }

        /// <summary>
        /// Файлы, содержащиеся в текущей папке.
        /// </summary>
        public IEnumerable<FileAddressable> Files { get; private set; }
    }
}