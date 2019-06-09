using System;
using System.Globalization;
using System.Windows.Data;
using VirtualFileSystem.ViewModel.ViewModels;

namespace VirtualFileSystem.ViewModel.ValueConverters
{
    public class ViewModelToArtifactTypeConverter : IValueConverter
    {
        private const string Folder = "Папка";
        private const string File = "Файл";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var valueAsFolder = value as FolderViewModel;

            if (valueAsFolder != null)
            {
                return Folder;
            }
            else
            {
                var valueAsFile = value as FileViewModel;
                
                if (valueAsFile != null)
                {
                    return File;
                }
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}