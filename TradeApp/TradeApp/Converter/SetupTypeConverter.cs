using System;
using System.Globalization;
using System.Windows.Data;
using TradeApp.DataAccess;

namespace TradeApp.Converter
{
    public class SetupTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (SetupType)value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
