namespace VirtualFileSystem
{
    /// <summary>
    /// Посетитель для <see cref="Addressable"/>.
    /// </summary>
    public interface IAddressableObjectVisitor
    {
        /// <summary>
        /// Сходить в гости к файлу :).
        /// </summary>
        /// <param name="file"></param>
        void VisitFile(FileAddressable file);

        /// <summary>
        /// Сходить в гости к папке :).
        /// </summary>
        /// <param name="folder"></param>
        void VisitFolder(FolderAddressable folder);
    }
}