using System;
namespace TradeApp.DataAccess
{
    public interface ICandleStickRepository
    {
        void Save(TradeApp.DataAccess.CandleSticks candleStick);
    }
}
