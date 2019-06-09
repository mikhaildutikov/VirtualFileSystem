using System;
using System.Windows;
using VirtualFileSystem.ViewModel;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.Browser
{
    internal static class EntryPoint
    {
        [STAThread]
        static void Main()
        {
            var application = new Application();

            var browserApplicationController = new BrowserApplicationController(application.Dispatcher);
            var viewModelNameAndTypeSortingComparer = new ViewModelNameAndTypeSortingComparer();
            var userInteractionSystem = new UserInteractionService();

            var virtualFileSystemInstanceManager = new VirtualFileSystemInstanceManager();

            var hubViewModel = new BrowserHubWindowViewModel(browserApplicationController, viewModelNameAndTypeSortingComparer, userInteractionSystem, virtualFileSystemInstanceManager, new DispatcherAdapted(application.Dispatcher));

            var window = new BrowserHubWindowView(hubViewModel);

            application.Run(window);
        }
    }
}