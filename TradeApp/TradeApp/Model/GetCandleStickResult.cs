using System;
using System.Collections.Generic;
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
