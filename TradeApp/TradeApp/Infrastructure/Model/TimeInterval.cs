using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.Infrastructure.Model
{
    public enum TimeIntervals
    {
        Minute,
        Hour,
        Day,
    }

    public class TimeIntervalTypeItem
    {
        public TimeIntervals ValueTimeIntervalTypeEnum { get; set; }
        public string ValueTimeIntervalTypeString { get; set; }
    }
}
