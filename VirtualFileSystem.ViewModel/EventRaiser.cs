using System.ComponentModel;
using System.Threading;

namespace VirtualFileSystem.ViewModel
{
    internal static class EventRaiser
    {
        public static void RaisePropertyChangedEvent(PropertyChangedEventHandler handler, object eventSender, string propertyName)
        {
            PropertyChangedEventHandler eventHandler = Interlocked.CompareExchange(ref handler, null, null);

            if (eventHandler != null)
            {
                eventHandler.Invoke(eventSender, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}