using System;
namespace TradeApp.ApiClient
{
    /// <summary>
    /// Defines the functionality that exchange classes have to implement to be pluggable 
    /// </summary>
    public interface IExchangeClient
    {        
        System.Collections.Generic.IList<TradeApp.ConnectorService.PricePoint> CatchUp();
        System.Collections.Generic.IList<TradeApp.ConnectorService.PricePoint> GetPricePoints(DateTimeOffset start, DateTimeOffset? end);
        TradeApp.Model.GetCandleStickResult GetCandleStick(long last);
        TradeApp.Model.PlaceOrderResult PlaceOrder(TradeApp.DataAccess.Orders order, bool wait);
        TradeApp.Model.CancelOrderResult CancelOrder(TradeApp.DataAccess.Orders order);
        void HandleErrors(System.Collections.Generic.List<string> errors);
        void HandleException(Exception exception);
    }
}
