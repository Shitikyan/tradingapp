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
