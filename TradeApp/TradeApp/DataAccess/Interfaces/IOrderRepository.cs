using System;
namespace TradeApp.DataAccess
{
    public interface IOrderRepository
    {
        TradeApp.DataAccess.Orders Save(TradeApp.DataAccess.Orders order);
    }
}
