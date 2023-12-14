using System.ComponentModel.Composition;
using System.Linq;

namespace TradeApp.DataAccess.Repositories
{
    [Export(typeof(ICandleStickRepository))]
    [ExportMetadata("Nature", "database")]
    public class CandleStickRepository : ICandleStickRepository
    {
        public void Save(CandleSticks candleStick)
        {
            using (TradeAppDBEntities dc = new TradeAppDBEntities())
            {
                if (candleStick.Id > 0)
                {
                    CandleSticks candleStickToToUpdate = dc.CandleSticks.Where(cs => cs.Id == candleStick.Id).FirstOrDefault();

                    if (candleStickToToUpdate != null)
                    {
                        dc.Entry(candleStickToToUpdate).CurrentValues.SetValues(candleStick);
                    }
                }
                else
                {
                    dc.CandleSticks.Add(candleStick);
                }
                dc.SaveChanges();
            }
        }
    }
}
