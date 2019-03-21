using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TradeApp.DataAccess;

namespace TradeApp.Converter
{
    public class UnixTimeConverter : IValueConverter
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 
                                                      DateTimeKind.Utc);
        
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {

            return UnixTimeToDateTime((string)value);
        
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static DateTimeOffset? UnixTimeToDateTime(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                double seconds = double.Parse(text, CultureInfo.InvariantCulture);
                return Epoch.AddSeconds(seconds);
            }
            return null;
        }
    }
}
