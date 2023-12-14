using System;
using System.Collections.Generic;
using TradeApp.DataAccess;

namespace TradeApp.Model
{
    public class CancelOrderResult
    {
        public CancelOrderResultType ResultType { get; set; }

        //Set only if ResultType = error
        public List<string> Errors { get; set; }

        //Set only if ResultType = exception
        public Exception Exception { get; set; }

        public Orders Order { get; set; }

        //Comma delimited list of ids corresponding to orders which are pending cancelation
        public string OrdersPending { get; set; }

        public int OrdersCanceled { get; set; }
    }

    public enum CancelOrderResultType
    {
        success,
        error,
        exception
    }
}
