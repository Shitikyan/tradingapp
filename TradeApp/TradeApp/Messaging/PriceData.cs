using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeApp.ConnectorService;
using TradeApp.DataAccess;
using TradeApp.Model;

namespace TradeApp.Messaging
{
    public class PriceData
    {
        public PriceData(CandleSticks candleStick, PricePoint wmaPoint,bool live)
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
