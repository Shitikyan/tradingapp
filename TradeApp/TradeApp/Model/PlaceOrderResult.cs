using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeApp.DataAccess;

namespace TradeApp.Model
{
    public class PlaceOrderResult
    {
        public PlaceOrderResultType ResultType { get; set; }

        //Set only if ResultType = error
        public List<string> Errors { get; set; }

        //Set only if ResultType = exception
        public Exception Exception { get; set; }

        public Orders Order { get; set; }
 
    }

    public enum PlaceOrderResultType
    {
        error,
        txid_null,
        success,
        partial,
        canceled_not_partial,
        exception,
    }
    
}
