using System;

namespace VirtualFileSystem.Toolbox
{
    /// <summary>
    /// Generic-обертка для WeakReference. (Идея Дж. Рихтера. CLR via C#, 3-rd Edition.)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct WeakReference<T> : IDisposable
        where T: class
    {
        private WeakReference _weakReference;

        public WeakReference(T objectToReference)
        {
            _weakReference = new WeakReference(objectToReference);
        }

        public T ReferencedObject
        {
            get { return (T)_weakReference.Target; }
        }

        public void Dispose()
        {
            _weakReference = null;
        }
    }
}