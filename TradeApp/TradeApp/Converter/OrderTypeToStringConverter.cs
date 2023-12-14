using System;
using System.Globalization;
using System.Windows.Data;
using TradeApp.DataAccess;

namespace TradeApp.Converter
{
    public class OrderTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (OrderType)value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
