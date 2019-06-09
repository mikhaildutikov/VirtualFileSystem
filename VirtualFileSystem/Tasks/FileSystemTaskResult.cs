using System;

namespace VirtualFileSystem
{
    /// <summary>
    /// Результат исполнения задачи в файловой системе.
    /// </summary>
    public abstract class FileSystemTaskResult
    {
        protected FileSystemTaskResult(string error)
        {
            this.Error = error;
        }

        /// <summary>
        /// True, если задача завершилась успешно. False - в противном случае.
        /// </summary>
        public bool ExecutedSuccessfully
        {
            get { return (String.IsNullOrEmpty(Error)); }
        }

        /// <summary>
        /// Текстовое описание ошибки (если есть - может быть равно null).
        /// </summary>
        public string Error { get; private set; }
    }
}