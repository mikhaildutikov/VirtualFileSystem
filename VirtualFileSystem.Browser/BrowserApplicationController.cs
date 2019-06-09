using System;
using System.Collections.Generic;
using System.Windows.Threading;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    internal class BrowserApplicationController : IApplicationController
    {
        private readonly Dispatcher _dispatcher;
        private readonly Dictionary<BrowserWindowViewModel, BrowserWindow> _browserViewModelsToWindows = new Dictionary<BrowserWindowViewModel, BrowserWindow>();
        private readonly object _controllerStateChangeCriticalSection = new object();

        public BrowserApplicationController(Dispatcher dispatcher)
        {
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            _dispatcher = dispatcher;
        }

        public void DisplayNewBrowserWindow(BrowserWindowViewModel browserWindowViewModel)
        {
            lock (_controllerStateChangeCriticalSection)
            {
                if (browserWindowViewModel == null) throw new ArgumentNullException("browserWindowViewModel");

                _dispatcher.Invoke(new Action(delegate
                                                  {
                                                      var browserWindow = new BrowserWindow(browserWindowViewModel);

                                                      _browserViewModelsToWindows.Add(browserWindowViewModel,
                                                                                      browserWindow);

                                                      browserWindow.Show();
                                                  }));
            }
        }

        public void QuitDisplaying(BrowserWindowViewModel browserWindowViewModel, bool closeDownTheWindow)
        {
            lock (_controllerStateChangeCriticalSection)
            {
                if (_browserViewModelsToWindows.ContainsKey(browserWindowViewModel))
                {
                    var window = _browserViewModelsToWindows[browserWindowViewModel];

                    _browserViewModelsToWindows.Remove(browserWindowViewModel);

                    browserWindowViewModel.Dispose();

                    if (closeDownTheWindow)
                    {
                        window.Close();
                    }
                }
            }
        }

        public void DisplayTaskResults(IEnumerable<TaskResultViewModel> results)
        {
            lock (_controllerStateChangeCriticalSection)
            {
                var taskResultView = new TaskResultView(results);

                taskResultView.Show();
            }
        }

        public void PresentListOfStrings(string title, IEnumerable<string> strings)
        {
            if (strings == null) throw new ArgumentNullException("strings");

            _dispatcher.Invoke(new Action(delegate
                                   {
                                       var titledStringListView = new TitledStringListView(new TitledStringListViewModel(title, strings));
                                       titledStringListView.Show();
                                   }));
        }
    }
}