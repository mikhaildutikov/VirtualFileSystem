using System;
using System.Collections.Generic;
using System.Threading;
using VirtualFileSystem.ContentsEnumerators;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem
{
    internal class FolderEnumeratorRegistry : IFolderEnumeratorRegistry
    {
        private readonly Dictionary<Guid, List<WeakReference<FolderContentsEnumerator>>> _foldersToIterators = new Dictionary<Guid, List<WeakReference<FolderContentsEnumerator>>>();
        private readonly object _stateChangeCriticalSection = new object();

        public void InvalidateEnumeratorsForFolder(Guid folderId)
        {
            lock (_stateChangeCriticalSection)
            {
                if (_foldersToIterators.ContainsKey(folderId))
                {
                    List<WeakReference<FolderContentsEnumerator>> allIteratorsForGivenFolder =
                        _foldersToIterators[folderId];

                    for (int i = 0; i < allIteratorsForGivenFolder.Count; i++)
                    {
                        WeakReference<FolderContentsEnumerator> enumeratorReference = allIteratorsForGivenFolder[i];
                        FolderContentsEnumerator enumerator = enumeratorReference.ReferencedObject;

                        if (enumerator == null) //Note: по идее такого все же быть не должно - итератор нам просигналит о завершении своего существования в любом случае.
                        {
                            enumeratorReference.Dispose();
                            allIteratorsForGivenFolder.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            enumerator.MarkAsInvalid(); // все: ходить по элементам больше не получится.
                        }
                    }

                    if (allIteratorsForGivenFolder.Count == 0)
                    {
                        _foldersToIterators.Remove(folderId);
                    }
                }
            }
        }

        public void RegisterEnumerator(FolderContentsEnumerator folderContentsEnumerator)
        {
            if (folderContentsEnumerator == null) throw new ArgumentNullException("folderContentsEnumerator");

            lock (_stateChangeCriticalSection)
            {
                List<WeakReference<FolderContentsEnumerator>> allIteratorsForGivenFolder;

                if (_foldersToIterators.ContainsKey(folderContentsEnumerator.FolderBeingEnumerated.Id))
                {
                    allIteratorsForGivenFolder = _foldersToIterators[folderContentsEnumerator.FolderBeingEnumerated.Id];
                }
                else
                {
                    allIteratorsForGivenFolder = new List<WeakReference<FolderContentsEnumerator>>();
                    _foldersToIterators[folderContentsEnumerator.FolderBeingEnumerated.Id] = allIteratorsForGivenFolder;
                }

                allIteratorsForGivenFolder.Add(new WeakReference<FolderContentsEnumerator>(folderContentsEnumerator)); 
            }
        }

        public void Unregister(FolderContentsEnumerator folderContentsEnumerator)
        {
            if (folderContentsEnumerator == null) throw new ArgumentNullException("folderContentsEnumerator");

            Monitor.Enter(_stateChangeCriticalSection);

            try
            {
                List<WeakReference<FolderContentsEnumerator>> allIteratorsForGivenFolder =
                    _foldersToIterators[folderContentsEnumerator.FolderBeingEnumerated.Id];

                for (int i = 0; i < allIteratorsForGivenFolder.Count; i++)
                {
                    WeakReference<FolderContentsEnumerator> enumeratorReference = allIteratorsForGivenFolder[i];

                    FolderContentsEnumerator enumerator = enumeratorReference.ReferencedObject;

                    if (Object.ReferenceEquals(enumerator, folderContentsEnumerator))
                    {
                        enumeratorReference.Dispose();
                        allIteratorsForGivenFolder.RemoveAt(i);
                        i--;
                    }
                }

                RemoveFolderRecordIfItIsEmpty(folderContentsEnumerator, allIteratorsForGivenFolder);
            }
            catch (KeyNotFoundException)
            {
            }
            finally
            {
                Monitor.Exit(_stateChangeCriticalSection);
            }
        }

        private void RemoveFolderRecordIfItIsEmpty(FolderContentsEnumerator folderContentsEnumerator, List<WeakReference<FolderContentsEnumerator>> allIteratorsForGivenFolder)
        {
            if (allIteratorsForGivenFolder.Count == 0)
            {
                _foldersToIterators.Remove(folderContentsEnumerator.FolderBeingEnumerated.Id);
            }
        }
    }
}