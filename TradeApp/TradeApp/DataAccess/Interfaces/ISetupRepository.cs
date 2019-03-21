using System;
namespace TradeApp.DataAccess
{
    public interface ISetupRepository
    {
        void Save(TradeApp.DataAccess.Setups setup);
    }
}
