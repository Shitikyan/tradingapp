﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.DataAccess.Repositories
{
    [Export(typeof(ISetupRepository))]
    [ExportMetadata("Nature", "database")]
    public class SetupRepository : TradeApp.DataAccess.ISetupRepository
    {
        public void Save(Setups setup)
        {
            using (TradeAppDBEntities dc = new TradeAppDBEntities())
            {
                if (setup.Id > 0)
                {
                    Setups itemToUpdate = dc.Setups.Where(cs => cs.Id == setup.Id).FirstOrDefault();

                    if (itemToUpdate != null)
                    {
                        dc.Entry(itemToUpdate).CurrentValues.SetValues(setup);
                    }
                }
                else
                {
                    dc.Setups.Add(setup);
                }
                dc.SaveChanges();
            }
        }
    }
}
