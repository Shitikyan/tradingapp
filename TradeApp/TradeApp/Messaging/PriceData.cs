using TradeApp.ConnectorService;
using TradeApp.DataAccess;

namespace TradeApp.Messaging
{
    public class PriceData
    {
        public PriceData(CandleSticks candleStick, PricePoint wmaPoint, bool live)
        {
            CandleStick = candleStick;
            WmaPoint = wmaPoint;
            Live = live;
        }

        public CandleSticks CandleStick { get; set; }
        public PricePoint WmaPoint { get; set; }
        public bool Live { get; set; }
    }
}
