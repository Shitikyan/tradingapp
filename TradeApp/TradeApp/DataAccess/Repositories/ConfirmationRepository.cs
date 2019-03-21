using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.DataAccess.Repositories
{
    [Export(typeof(IConfirmationRepository))]
    [ExportMetadata("Nature", "database")]
    public class ConfirmationRepository : TradeApp.DataAccess.IConfirmationRepository
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
