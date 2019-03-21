using System;
namespace TradeApp.DataAccess
{
    public interface IConfirmationRepository
    {
        void Save(TradeApp.DataAccess.Confirmations confirmation);
    }
}
