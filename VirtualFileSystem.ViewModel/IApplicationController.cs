using System.Collections.Generic;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel
{
    internal interface IApplicationController
    {
        void DisplayNewBrowserWindow(BrowserWindowViewModel browserWindowViewModel);
        void QuitDisplaying(BrowserWindowViewModel browserWindowViewModel, bool closeDownTheWindow);
        void DisplayTaskResults(IEnumerable<TaskResultViewModel> results);
        void PresentListOfStrings(string title, IEnumerable<string> strings);
    }
}