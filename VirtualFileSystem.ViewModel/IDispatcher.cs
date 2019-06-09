using System;

namespace VirtualFileSystem.ViewModel
{
    internal interface IDispatcher
    {
        object Invoke(Delegate method, params object[] args);
    }
}