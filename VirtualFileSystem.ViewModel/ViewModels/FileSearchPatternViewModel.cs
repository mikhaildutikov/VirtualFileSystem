using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    /// <summary>
    /// Note: надо бы устранить небольшое дублирование логики валидации.
    /// </summary>
    internal class FileSearchPatternViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private const string PatternPropertyName = "Pattern";
        private string _pattern;
        private bool _isValid;
        private readonly Dictionary<string, string> _namesOfPropertiesToErrors = new Dictionary<string, string>();
        private bool _isSearchEnabled;

        public FileSearchPatternViewModel()
        {
            this.IsSearchEnabled = true;
            this.Pattern = String.Empty;
        }

        public bool IsSearchEnabled
        {
            get { return _isSearchEnabled; }
            internal set
            {
                _isSearchEnabled = value;
                this.NotifyInterestedPartiesOfPropertyChange("IsSearchEnabled");
                this.NotifyInterestedPartiesOfPropertyChange("CanSearch");
            }
        }

        public string Pattern
        {
            get { return _pattern; }
            set
            {
                _pattern = value;

                bool isNewValueEmpty = String.IsNullOrEmpty(value);

                if (!isNewValueEmpty)
                {
                    _namesOfPropertiesToErrors[PatternPropertyName] = null;
                }
                else
                {
                    _namesOfPropertiesToErrors[PatternPropertyName] = "Требуется непустая строка";
                }

                this.IsValid = !isNewValueEmpty;
            }
        }

        private void NotifyInterestedPartiesOfPropertyChange(string propertyName)
        {
            EventRaiser.RaisePropertyChangedEvent(this.PropertyChanged, this, propertyName);
        }

        public bool IsValid
        {
            get { return _isValid; }

            private set
            {
                _isValid = value;
                this.NotifyInterestedPartiesOfPropertyChange("IsValid");
                this.NotifyInterestedPartiesOfPropertyChange("CanSearch");
            }
        }

        public bool CanSearch
        {
            get { return (this.IsValid && this.IsSearchEnabled); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (!_namesOfPropertiesToErrors.ContainsKey(columnName))
                {
                    return null;
                }
                else
                {
                    return _namesOfPropertiesToErrors[columnName];
                }
            }
        }

        string IDataErrorInfo.Error
        {
            get { return String.Empty; }
        }
    }
}