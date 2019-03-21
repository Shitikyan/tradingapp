using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeApp.DataAccess;

namespace TradeApp.Model
{
    class RefreshOrderResult
    {
        public RefreshOrderResultType ResultType { get; set; }

        //Set only if ResultType = error
        public List<string> Errors { get; set; }

        //Set only if ResultType = exception
        public Exception Exception { get; set; }

        public Orders Order { get; set; }
    }

    public enum RefreshOrderResultType
    {
        error,
        exception,
        order_not_found,
        success,
    }
}
