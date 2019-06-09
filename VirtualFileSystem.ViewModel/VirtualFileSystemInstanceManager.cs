using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using VirtualFileSystem.Disk;

namespace VirtualFileSystem.ViewModel
{
    internal class VirtualFileSystemInstanceManager : IVirtualFileSystemInstanceManager, INotifyPropertyChanged
    {
        private const int DefaultNumberOfBlocksForNewDisks = 150000;
        private readonly Dictionary<string, VirtualFileSystem> _filesToFileSystems = new Dictionary<string, VirtualFileSystem>();
        private readonly Dictionary<string, int> _filesToInstanceCounter = new Dictionary<string, int>();
        private readonly object _stateChangeCriticalSection = new object();
        private bool _isEmpty;

        public VirtualFileSystemInstanceManager()
        {
            this.IsEmpty = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <param name="fileSystemId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        public VirtualFileSystem CreateFromFile(string fullFilePath, out string fileSystemId)
        {
            lock (_stateChangeCriticalSection)
            {
                string fullFilePathUpperCased = fullFilePath.ToUpper();

                if (_filesToFileSystems.ContainsKey(fullFilePathUpperCased))
                {
                    _filesToInstanceCounter[fullFilePathUpperCased]++;

                    fileSystemId = fullFilePath + " #" + _filesToInstanceCounter[fullFilePathUpperCased];
                }
                else
                {
                    var fileSystem = VirtualFileSystem.OpenExisting(fullFilePath);

                    _filesToInstanceCounter[fullFilePathUpperCased] = 1;
                    _filesToFileSystems[fullFilePathUpperCased] = fileSystem;

                    fileSystemId = fullFilePath;
                }

                this.ReevaluateIsEmptyPropertyValue();

                return _filesToFileSystems[fullFilePathUpperCased];
            }
        }

        public void ReportThatSystemIsNoLongerNeeded(VirtualFileSystem virtualFileSystem)
        {
            lock(_stateChangeCriticalSection)
            {
                var keyValuePairWithSystemInQuestion =
                    _filesToFileSystems.Single(
                        keyValuePair => Object.ReferenceEquals(keyValuePair.Value, virtualFileSystem));

                _filesToInstanceCounter[keyValuePairWithSystemInQuestion.Key]--;

                if (_filesToInstanceCounter[keyValuePairWithSystemInQuestion.Key] == 0)
                {
                    _filesToInstanceCounter.Remove(keyValuePairWithSystemInQuestion.Key);
                    _filesToFileSystems.Remove(keyValuePairWithSystemInQuestion.Key);

                    virtualFileSystem.Dispose();
                }

                this.ReevaluateIsEmptyPropertyValue();
            }
        }

        public bool IsEmpty
        {
            get { return _isEmpty; }
            private set
            {
                _isEmpty = value;
                EventRaiser.RaisePropertyChangedEvent(PropertyChanged, this, "IsEmpty");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileSystemCreationFailedException"></exception>
        public VirtualFileSystem CreateNewFormattingTheFile(string fullFilePath)
        {
            lock (_stateChangeCriticalSection)
            {
                var fileSystem = VirtualFileSystem.CreateNew(fullFilePath, VirtualDisk.OnlySupportedBlockSize * DefaultNumberOfBlocksForNewDisks);

                string fullFilePathUpperCased = fullFilePath.ToUpper();

                _filesToFileSystems[fullFilePathUpperCased] = fileSystem;
                _filesToInstanceCounter[fullFilePathUpperCased] = 1;

                this.ReevaluateIsEmptyPropertyValue();

                return fileSystem;
            }
        }

        private void ReevaluateIsEmptyPropertyValue()
        {
            this.IsEmpty = (_filesToInstanceCounter.Keys.Count == 0);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}