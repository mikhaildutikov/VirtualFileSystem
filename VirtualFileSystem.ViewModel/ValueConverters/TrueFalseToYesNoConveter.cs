using System;
using System.Globalization;
using System.Windows.Data;

namespace VirtualFileSystem.ViewModel.ValueConverters
{
    public class TrueFalseToYesNoConveter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool valueAsBool = (bool)value;

            if (valueAsBool)
            {
                return "Да";
            }
            else
            {
                return "Нет";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}