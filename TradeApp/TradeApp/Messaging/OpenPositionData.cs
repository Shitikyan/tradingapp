using TradeApp.DataAccess;

namespace TradeApp.Messaging
{
    public class OpenPositionData
    {
        public OpenPositionData(OrderType orderType, decimal enteringPrice, decimal exitingPrice, decimal volume, int candleStickId, int confirmationId, bool validateOnly = false)
        {
            Direction = orderType;
            EnteringPrice = enteringPrice;
            ExitingPrice = exitingPrice;
            Volume = volume;
            CandleStickId = candleStickId;
            ConfirmationId = confirmationId;
            ValidateOnly = validateOnly;
        }

        public OrderType Direction { get; set; }
        public decimal EnteringPrice { get; set; }
        public decimal ExitingPrice { get; set; }
        public decimal Volume { get; set; }
        public int CandleStickId { get; set; }
        public int ConfirmationId { get; set; }
        public bool ValidateOnly { get; set; }
    }
}
