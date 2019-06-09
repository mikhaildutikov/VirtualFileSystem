using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    internal class NewArtifactNameViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private const string NewNamePropertyName = "Name";
        private readonly IFileSystemArtifactNamesValidator _namesValidator;
        private string _name;
        private bool _isValid = true;
        private readonly Dictionary<string, string> _namesOfPropertiesToErrors = new Dictionary<string, string>();

        public NewArtifactNameViewModel(IFileSystemArtifactNamesValidator namesValidator)
        {
            if (namesValidator == null) throw new ArgumentNullException("namesValidator");
            
            _namesValidator = namesValidator;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;

                try
                {
                    _namesValidator.Validate(_name);
                    _namesOfPropertiesToErrors[NewNamePropertyName] = null;
                    this.IsValid = true;
                }
                catch (InvalidNameException exception)
                {
                    this.IsValid = false;
                    _namesOfPropertiesToErrors[NewNamePropertyName] = exception.Message;
                }
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
            }
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