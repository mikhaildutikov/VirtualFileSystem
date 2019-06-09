using System;
using System.Globalization;
using System.Windows.Data;

namespace VirtualFileSystem.ViewModel.ValueConverters
{
    public class EnumToCheckBoxIsCheckedConverter : IValueConverter
    {
        public object Convert(object enumValueForCheckBoxToBeChecked, Type targetType, object actualEnumValue, CultureInfo culture)
        {
            //Note: аргументы не проверяю, хотя должен

            bool shouldCheckBoxBeChecked = enumValueForCheckBoxToBeChecked.Equals(actualEnumValue);

            return shouldCheckBoxBeChecked;
        }

        public object ConvertBack(object isCheckBoxChecked, Type targetType, object newEnumValue, CultureInfo culture)
        {
            //Note: аргументы не проверяю, хотя должен

            return newEnumValue;
        }
    }
}