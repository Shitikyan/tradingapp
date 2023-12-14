namespace TradeApp.DataAccess
{
    public interface IOrderRepository
    {
        Orders Save(Orders order);
    }
}
