using System;
using System.Windows.Threading;

namespace VirtualFileSystem.ViewModel
{
    internal class DispatcherAdapted : IDispatcher
    {
        private readonly Dispatcher _dispatcher;

        public DispatcherAdapted(Dispatcher dispatcher)
        {
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");
            _dispatcher = dispatcher;
        }

        public object Invoke(Delegate method, params object[] args)
        {
            return _dispatcher.Invoke(method, args);
        }
    }
}