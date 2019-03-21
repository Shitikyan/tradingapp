using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.Messaging
{
    public class ShiftPositionLimitsData
    {
        public ShiftPositionLimitsData(decimal newLimitPrice, bool validateOnly) 
        {
            NewLimitPrice = newLimitPrice;
            ValidateOnly = validateOnly;
        }
        
        public decimal NewLimitPrice { get; set; }
        public bool ValidateOnly { get; set; }
    }
}
