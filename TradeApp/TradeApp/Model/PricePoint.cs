using System;
using System.Globalization;

namespace TradeApp.ConnectorService
{
    public partial class PricePoint
    {
        public PricePoint(string unixTime, string price, string amount)
        {
            _Time = UnixTimeSecondsToDateTime(unixTime);
            _Price = decimal.Parse(price);
            _Amount = double.Parse(amount);
        }

        public PricePoint(DateTimeOffset time, decimal price, double amount)
        {
            _Time = time;
            _Price = price;
            _Amount = amount;
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0,
                                                      DateTimeKind.Utc);

        public static DateTimeOffset UnixTimeSecondsToDateTime(string text)
        {
            double seconds = double.Parse(text, CultureInfo.InvariantCulture);
            return Epoch.AddSeconds(seconds);
        }

        public static DateTimeOffset UnixTimeNanoSecondsToDateTime(string text)
        {
            long nanoseconds = long.Parse(text, CultureInfo.InvariantCulture);
            return Epoch.AddSeconds(nanoseconds / 1000000000);
        }

        public static long DateTimeToUnixTimeNonoseconds(DateTimeOffset date)
        {
            //create Timespan by subtracting the value provided from
            //the Unix Epoch

            //TEMP MODIF
            TimeSpan span = (date - Epoch);

            //return the total nono seconds (which is a UNIX timestamp for kraken)
            return (long)span.TotalSeconds * 1000000000;
        }
    }
}
