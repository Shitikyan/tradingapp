using System;

namespace TradeApp.DataAccess.Factories
{
    public class OrderFactory
    {
        public Orders CreateOpeningOrder(OrderType type, KrakenOrderType orderType, decimal enteringPrice, decimal volume, int candleId, int confirmationId, string pair = "XXBTZEUR", bool viqc = false, bool validateOnly = false)
        {
            Orders order = new Orders();

            order.CreateDate = DateTimeOffset.UtcNow;
            order.Pair = pair;
            order.Type = type.ToString();
            order.OrderType = orderType.ToString().Replace("_", "-");
            order.Price = enteringPrice;
            order.Volume = volume;
            order.CandleStickId = candleId;
            order.ConfirmationId = confirmationId;
            if (viqc)
                order.OFlags = OFlag.viqc.ToString();
            order.Validate = validateOnly;
            order.Status = "not yet submitted";
            return order;
        }

        public Orders CreateStopLossOrder(Orders orderToClose, decimal limitPrice, bool validateOnly = false)
        {
            Orders order = new Orders();

            order.CreateDate = DateTimeOffset.UtcNow;
            order.Pair = orderToClose.Pair;
            order.Type = (orderToClose.Type == "buy") ? "sell" : "buy";
            order.OrderType = (KrakenOrderType.stop_loss).ToString().Replace("_", "-");
            order.Price = limitPrice;
            order.Volume = orderToClose.VolumeExecuted ?? orderToClose.Volume;
            order.OFlags = orderToClose.OFlags;
            order.Validate = validateOnly;
            order.CandleStickId = orderToClose.CandleStickId;
            order.ConfirmationId = orderToClose.ConfirmationId;
            order.Status = "not yet submitted";

            return order;
        }

        public Orders CreateEmergencyExitOrder(Orders orderToClose, bool validateOnly = false)
        {
            Orders order = new Orders();

            order.CreateDate = DateTimeOffset.UtcNow;
            order.Pair = orderToClose.Pair;
            order.Type = (orderToClose.Type == "buy") ? "sell" : "buy";
            order.OrderType = (KrakenOrderType.market).ToString().Replace("_", "-");
            order.Volume = orderToClose.VolumeExecuted ?? orderToClose.Volume;
            order.OFlags = orderToClose.OFlags;
            order.Validate = validateOnly;
            order.Status = "not yet submitted";
            order.IsEmergencyOrder = true;

            return order;
        }
    }
}
