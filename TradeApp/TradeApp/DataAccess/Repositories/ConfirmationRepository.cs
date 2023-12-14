using System.ComponentModel.Composition;
using System.Linq;

namespace TradeApp.DataAccess.Repositories
{
    [Export(typeof(IConfirmationRepository))]
    [ExportMetadata("Nature", "database")]
    public class ConfirmationRepository : IConfirmationRepository
    {
        public void Save(Confirmations confirmation)
        {
            using (TradeAppDBEntities dc = new TradeAppDBEntities())
            {
                if (confirmation.Id > 0)
                {
                    Confirmations itemToUpdate = dc.Confirmations.Where(cs => cs.Id == confirmation.Id).FirstOrDefault();

                    if (itemToUpdate != null)
                    {
                        dc.Entry(itemToUpdate).CurrentValues.SetValues(confirmation);
                    }
                }
                else
                {
                    dc.Confirmations.Add(confirmation);
                }
                dc.SaveChanges();
            }
        }
    }
}
