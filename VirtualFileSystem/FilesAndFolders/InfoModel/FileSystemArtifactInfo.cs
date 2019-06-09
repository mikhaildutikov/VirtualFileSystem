using System;

namespace VirtualFileSystem
{
    /// <summary>
    /// Сведения об артефакте файловой системы - файле или папке.
    /// </summary>
    public abstract class FileSystemArtifactInfo
    {
        protected internal FileSystemArtifactInfo(string fullPath, string name, Guid id, Guid version, DateTime creationTimeUtc)
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
            Version = version;
            CreationTimeUtc = creationTimeUtc;
        }

        /// <summary>
        /// Путь, ведущий к артефакту.
        /// </summary>
        public string FullPath { get; private set; }

        /// <summary>
        /// Название артефакта - имя файла или папки.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Уникальный идентификатор артефакта.
        /// </summary>
        public Guid Id { get; private set; }
        public Guid Version { get; private set; }

        /// <summary>
        /// Время создания артефакта (файла или папки) в UTC.
        /// </summary>
        public DateTime CreationTimeUtc { get; private set; }
    }
}