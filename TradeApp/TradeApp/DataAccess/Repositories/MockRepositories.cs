using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.DataAccess.Repositories
{

    public class MockRepositoryBase
    {
        private object _counterLock = new Object();

        //auto-incrementing id
        private int IdCounter;

        protected int GetNextId()
        {
            lock (_counterLock)
            {
                return ++IdCounter;
            }
        }
    }
    
    [Export(typeof(ICandleStickRepository))]
    [ExportMetadata("Nature", "mock")]
    public class MockCandleStickRepository :MockRepositoryBase, TradeApp.DataAccess.ICandleStickRepository
    {
        public void Save(CandleSticks candleStick)
        {
            if (candleStick.Id == 0)
                candleStick.Id = base.GetNextId();
        }
    }

    [Export(typeof(IConfirmationRepository))]
    [ExportMetadata("Nature", "mock")]
    public class MockConfirmationRepository : MockRepositoryBase, TradeApp.DataAccess.IConfirmationRepository
    {
        public void Save(Confirmations confirmation)
        {
            if (confirmation.Id == 0)
                confirmation.Id = base.GetNextId();
        }
    }

    [Export(typeof(ISetupRepository))]
    [ExportMetadata("Nature", "mock")]
    public class MockSetupRepository :MockRepositoryBase, TradeApp.DataAccess.ISetupRepository
    {
        public void Save(Setups setup)
        {
            if(setup.Id==0)
                setup.Id = base.GetNextId();
        }
    }

    [Export(typeof(IOrderRepository))]
    [ExportMetadata("Nature", "mock")]
    public class MockOrderRepository : MockRepositoryBase,TradeApp.DataAccess.IOrderRepository
    {
        public Orders Save(Orders order)
        {
            if(order.Id == 0)//do something
                order.Id = base.GetNextId();
            return order;
        }
    }
}
