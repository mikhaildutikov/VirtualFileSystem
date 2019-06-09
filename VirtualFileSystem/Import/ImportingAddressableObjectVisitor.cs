using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Toolbox.Extensions;
using VirtualFileSystem.Visitors;

namespace VirtualFileSystem.Import
{
    internal class ImportingAddressableObjectVisitor : IAddressableObjectVisitor
    {
        private readonly VirtualFileSystem _targetFileSystem;
        private readonly string _sourceFolderPath;
        private readonly string _destinationFolder;
        private readonly IFileContentsBufferFactory _fileContentsBufferFactory;
        private readonly IFileSystemCancellableTaskToken _taskToken;
        private readonly int _totalNumberOfFilesToVisit;
        private int _numberOfFilesVisited;
        private readonly List<FileSystemTaskResult> _fileTaskResults = new List<FileSystemTaskResult>();
        private int _percentageOfWorkCompleted;

        public ImportingAddressableObjectVisitor(
            VirtualFileSystem targetFileSystem,
            string sourceFolderPath,
            string destinationFolder,
            IFileContentsBufferFactory fileContentsBufferFactory,
            IFileSystemCancellableTaskToken taskToken,
            int totalNumberOfFilesToVisit)
        {
            if (targetFileSystem == null) throw new ArgumentNullException("targetFileSystem");
            if (sourceFolderPath == null) throw new ArgumentNullException("sourceFolderPath");
            if (destinationFolder == null) throw new ArgumentNullException("destinationFolder");
            if (fileContentsBufferFactory == null) throw new ArgumentNullException("fileContentsBufferFactory");
            if (taskToken == null) throw new ArgumentNullException("taskToken");

            _targetFileSystem = targetFileSystem;
            _sourceFolderPath = sourceFolderPath;
            _destinationFolder = destinationFolder;
            _fileContentsBufferFactory = fileContentsBufferFactory;
            _taskToken = taskToken;
            _totalNumberOfFilesToVisit = totalNumberOfFilesToVisit;
        }

        public ReadOnlyCollection<FileSystemTaskResult> GetResult()
        {
            return _fileTaskResults.AsReadOnly();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void VisitFile(FileAddressable file)
        {
            if (file == null) throw new ArgumentNullException("file");

            try
            {
                if (_taskToken.HasBeenCancelled)
                {
                    _fileTaskResults.Add(new FileTaskResult(file, null, "Задача прервана"));
                    return;
                }

                // если бы DirectorySeparatorChar не был установлен в Path.DirectorySeparatorChar, здесь надо было бы использовать разные стратегии для виртуальной и реальной файловой системы
                string newFilePathRelative = _targetFileSystem.PathBuilder.GetRelativePath(_sourceFolderPath, file.FullPath);

                string fullPathForFile;

                try
                {
                    fullPathForFile = _targetFileSystem.PathBuilder.CombinePaths(_destinationFolder, newFilePathRelative);
                }
                catch (ArgumentException exception)
                {
                    _fileTaskResults.Add(new FileTaskResult(file, null, "Не удалось создать эквивалент файла \"{0}\" в виртуальной файловой системе.{1}{2}".FormatWith(file.FullPath, Environment.NewLine, exception.Message)));
                    return;
                }

                string fileName = _targetFileSystem.PathBuilder.GetFileOrFolderName(fullPathForFile);

                CopyFileContents(file, fullPathForFile);

                _fileTaskResults.Add(new FileTaskResult(file, new FileAddressable(fullPathForFile, fileName), String.Empty));
            }
            catch(TaskCancelledException)
            {
                _fileTaskResults.Add(new FileTaskResult(file, null, "Задача прервана"));
            }
            catch (CannotGetFileContentsException exception)
            {
                _fileTaskResults.Add(new FileTaskResult(file, null, "{0}{1}{2}".FormatWith(exception.Message, Environment.NewLine, exception.InnerException.Message)));
            }
            catch(InvalidPathException exception)
            {
                AddNewErrorToFileTaskResults(file, exception);
            }
            catch(FolderNotFoundException exception)
            {
                AddNewErrorToFileTaskResults(file, exception);
            }
            catch(FileAlreadyExistsException exception)
            {
                AddNewErrorToFileTaskResults(file, exception);
            }
            catch(InsufficientSpaceException exception)
            {
                AddNewErrorToFileTaskResults(file, exception);
                throw; // продолжать не будем.
            }
            catch(MaximumFileCountReachedException exception)
            {
                AddNewErrorToFileTaskResults(file, exception);
            }
            catch(ObjectDisposedException exception)
            {
                AddNewErrorToFileTaskResults(file, exception);
                throw; // продолжать не будем.
            }
            finally
            {
                CalculateAndReportProgress();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fullPathForFile"></param>
        /// <exception cref="TaskCancelledException"></exception>
        /// <exception cref="CannotGetFileContentsException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidPathException">Если указанный путь невалиден</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FolderNotFoundException"></exception>
        /// <exception cref="FileAlreadyExistsException"></exception>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="MaximumFileCountReachedException"></exception>
        private void CopyFileContents(FileAddressable file, string fullPathForFile)
        {
            using (var stream = _targetFileSystem.CreateAndOpenFileForWriting(fullPathForFile))
            {
                try
                {
                    IEnumerator<byte[]> contentsEnumerator = _fileContentsBufferFactory.GetBufferEnumeratorFor(file.FullPath);

                    while (contentsEnumerator.MoveNext())
                    {
                        if (_taskToken.HasBeenCancelled)
                        {
                            throw new TaskCancelledException();
                        }

                        stream.Write(contentsEnumerator.Current, 0, contentsEnumerator.Current.Length);
                    }
                }
                catch (Exception)
                {
                    stream.Dispose();

                    try
                    {
                        _targetFileSystem.DeleteFile(fullPathForFile);
                    }
                    catch (FileNotFoundException)
                    { }
                    catch (FileLockedException)
                    { }
                    catch (ObjectDisposedException)
                    { }

                    throw;
                }
            }
        }

        private void CalculateAndReportProgress()
        {
            _numberOfFilesVisited++;

            int percentage = _numberOfFilesVisited*100/_totalNumberOfFilesToVisit;
                
            if (percentage != _percentageOfWorkCompleted)
            {
                _percentageOfWorkCompleted = percentage;
                _taskToken.ReportProgressChange(percentage);
            }
        }

        private void AddNewErrorToFileTaskResults(FileAddressable file, Exception exception)
        {
            _fileTaskResults.Add(new FileTaskResult(file, null, exception.Message));
        }

        private void AddNewErrorToFolderTaskResults(FolderAddressable folder, Exception exception)
        {
            _fileTaskResults.Add(new FolderTaskResult(folder, null, exception.Message));
        }

        /// <summary>
        /// </summary>
        /// <param name="folder"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void VisitFolder(FolderAddressable folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            try
            {
                if (_taskToken.HasBeenCancelled)
                {
                    _fileTaskResults.Add(new FolderTaskResult(folder, null, "Задача прервана"));
                    return;
                }

                // если бы DirectorySeparatorChar не был установлен в Path.DirectorySeparatorChar, здесь надо было бы использовать разные стратегии для виртуальной и реальной файловой системы
                string newFolderPathRelative = _targetFileSystem.PathBuilder.GetRelativePath(_sourceFolderPath, folder.FullPath);

                string folderPathInDestinationSystem = _targetFileSystem.PathBuilder.CombinePaths(_destinationFolder, newFolderPathRelative);

                if (!_targetFileSystem.PathBuilder.PointsToRoot(folderPathInDestinationSystem))
                {
                    _targetFileSystem.CreateFolder(folderPathInDestinationSystem);
                }
            }
            catch (FolderAlreadyExistsException)
            {
            }
            catch (ArgumentException exception)
            {
                _fileTaskResults.Add(new FolderTaskResult(folder, null, "Не удалось создать эквивалент папки \"{0}\" в виртуальной файловой системе.{1}{2}".FormatWith(folder.FullPath, Environment.NewLine, exception.Message)));
            }
            catch(InvalidPathException exception)
            {
                this.AddNewErrorToFolderTaskResults(folder, exception);
            }
            catch(FolderNotFoundException exception)
            {
                this.AddNewErrorToFolderTaskResults(folder, exception);
            }
            catch(InsufficientSpaceException exception)
            {
                this.AddNewErrorToFolderTaskResults(folder, exception);
            }
            catch(MaximumFolderCountReachedException exception)
            {
                this.AddNewErrorToFolderTaskResults(folder, exception);
            }
            catch(ObjectDisposedException exception)
            {
                this.AddNewErrorToFolderTaskResults(folder, exception);
            }
        }
    }
}