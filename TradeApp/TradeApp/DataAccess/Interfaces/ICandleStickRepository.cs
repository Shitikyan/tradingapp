namespace TradeApp.DataAccess
{
    public interface ICandleStickRepository
    {
        void Save(CandleSticks candleStick);
    }
}
