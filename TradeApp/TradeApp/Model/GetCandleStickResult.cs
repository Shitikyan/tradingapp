using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeApp.DataAccess;

namespace TradeApp.Model
{
    public class GetCandleStickResult
    {
        public GetCandleStickResultType ResultType { get; set; }

        public List<string> Errors { get; set; }

        public Exception Exception { get; set; }
        
        public CandleSticks CandleStick { get; set; }

        public long Last { get; set; }
    }

    public enum GetCandleStickResultType
    { 
        success,
        error,
        exception
    }
}
