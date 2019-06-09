using System;
using System.Collections.Generic;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal class TitledStringListViewModel
    {
        public TitledStringListViewModel(string title, IEnumerable<string> strings)
        {
            if (strings == null) throw new ArgumentNullException("strings");

            Title = title;
            Strings = strings;
        }

        public string Title { get; private set; }
        public IEnumerable<string> Strings { get; private set; }
    }
}