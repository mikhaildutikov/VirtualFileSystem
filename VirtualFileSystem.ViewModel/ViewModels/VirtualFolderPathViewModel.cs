using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VirtualFileSystem.ViewModel.ViewModels
{
    /// <summary>
    /// Note: надо устранить небольшое дублирование, связанное с валидацией в трех ViewModels.
    /// </summary>
    internal class VirtualFolderPathViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private const string PathPropertyName = "Path";
        private readonly IPathValidator _pathValidator;
        private string _path;
        private bool _isValid = true;
        private readonly Dictionary<string, string> _namesOfPropertiesToErrors = new Dictionary<string, string>();

        public VirtualFolderPathViewModel(IPathValidator pathValidator)
        {
            if (pathValidator == null) throw new ArgumentNullException("pathValidator");

            _pathValidator = pathValidator;
        }

        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;

                try
                {
                    _pathValidator.Validate(_path);
                    _namesOfPropertiesToErrors[PathPropertyName] = null;
                    this.IsValid = true;
                }
                catch (InvalidPathException exception)
                {
                    this.IsValid = false;
                    _namesOfPropertiesToErrors[PathPropertyName] = exception.Message;
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