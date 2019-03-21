using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.DataAccess.Repositories
{
    [Export(typeof(IOrderRepository))]
    [ExportMetadata("Nature", "database")]
    public class OrderRepository : TradeApp.DataAccess.IOrderRepository
    {
        public Orders Save(Orders order)
        {
            using (TradeAppDBEntities dc = new TradeAppDBEntities())
            {
                if (order.Id > 0)
                {
                    Orders itemToUpdate = dc.Orders.Where(cs => cs.Id == order.Id).FirstOrDefault();

                    if (itemToUpdate != null)
                    {
                        dc.Entry(itemToUpdate).CurrentValues.SetValues(order);
                    }
                }
                else
                {
                    order = dc.Orders.Add(order);
                }
                dc.SaveChanges();
            }
            return order;
        }
    }
}
