using System;
namespace TradeApp.ApiClient
{
    /// <summary>
    /// Defines the functionality that exchange classes have to implement to be pluggable 
    /// </summary>
    public interface IExchangeClient
    {
        System.Collections.Generic.IList<ConnectorService.PricePoint> CatchUp();
        System.Collections.Generic.IList<ConnectorService.PricePoint> GetPricePoints(DateTimeOffset start, DateTimeOffset? end);
        Model.GetCandleStickResult GetCandleStick(long last);
        Model.PlaceOrderResult PlaceOrder(DataAccess.Orders order, bool wait);
        Model.CancelOrderResult CancelOrder(DataAccess.Orders order);
        void HandleErrors(System.Collections.Generic.List<string> errors);
        void HandleException(Exception exception);
    }
}
