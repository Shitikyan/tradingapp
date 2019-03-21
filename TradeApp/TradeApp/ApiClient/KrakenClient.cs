using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using System.Net;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using TradeApp.DataAccess;
using log4net;
using System.Threading;
using TradeApp.ConnectorService;
using TradeApp.Model;
using System.ComponentModel.Composition;
using PennedObjects.RateLimiting;

namespace TradeApp.ApiClient
{

    [Export(typeof(IExchangeClient))]
    public class KrakenClient : ExchangeClientBase,IExchangeClient
    {

        string _url;
        int _version;
        string _key;
        string _secret;
        RateGate _rateGate;
        ILog _logger;
        int _maxRetries;

        public KrakenClient()
        {
            _url = ConfigurationManager.AppSettings["KrakenBaseAddress"];
            _version = int.Parse(ConfigurationManager.AppSettings["KrakenApiVersion"]);
            _key = ConfigurationManager.AppSettings["KrakenKey"];
            _secret = ConfigurationManager.AppSettings["KrakenSecret"];
            _rateGate = new RateGate(1, TimeSpan.FromSeconds(5));
            _logger = LogManager.GetLogger("KrakenClient");
            _maxRetries = int.Parse(ConfigurationManager.AppSettings["KrakenClientMaxRetries"]) ;
            base.Name = "Kraken";

        }

        private JsonObject QueryPublic(string a_sMethod, string props = null)
        {

            try
            {
                int tries = 0;
                bool keepTrying = true;

                while (keepTrying)
                {

                    //Wait for RateGate
                    _rateGate.WaitToProceed();

                    string address = string.Format("{0}/{1}/public/{2}", _url, _version, a_sMethod);
                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(address);
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Method = "POST";


                    if (props != null)
                    {

                        using (var writer = new StreamWriter(webRequest.GetRequestStream()))
                        {
                            writer.Write(props);
                        }
                    }

                    //Make the request
                    try
                    {
                        using (WebResponse webResponse = webRequest.GetResponse())
                        {
                            using (Stream str = webResponse.GetResponseStream())
                            {
                                using (StreamReader sr = new StreamReader(str))
                                {
                                    return (JsonObject)JsonConvert.Import(sr);
                                }
                            }
                        }
                    }
                    catch (WebException wex)
                    {
                        #region Handle Exception

                        _logger.Debug(string.Format("Try {0} of {1} failed.", tries, _maxRetries));

                        if (wex.Response != null)
                        {
                            using (HttpWebResponse response = (HttpWebResponse)wex.Response)
                            {
                                _logger.Debug(string.Format("WebException. StatusCode: {0}, StatusDescription: {1}", response.StatusCode, response.StatusDescription));
                            }
                        }

                        _logger.Debug(string.Format("WebException. Message: {0}", wex.Message));


                        if (tries < _maxRetries)
                        {
                            _logger.Debug("Retrying...");
                            tries++;
                            keepTrying = true;
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            _logger.Error(string.Format("Maximum number of tries ({0}) reached. Throwing...", _maxRetries));
                            throw;
                        }

                        #endregion                                              
                    }
                }
                //not supposed to get here
                throw new Exception("Exited the try loop without anything happening.");
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error in KrakenClient.QueryPublic at line {0}: {1}", ex.LineNumber(), ex.Message));
            }
        }

        private JsonObject QueryPrivate(string a_sMethod, string props = null)
        {
            try
            {
                int tries = 1;
                bool keepTrying = true;
                
                while (keepTrying)
                {
                    //Wait for RateGate before creating the nonce otherwise one might get an Invalid-nonce exception if multiple threads are placing requests
                    _rateGate.WaitToProceed();

                    #region Prepare request

                    // generate a 64 bit nonce using a timestamp at tick resolution
                    Int64 nonce = DateTime.UtcNow.Ticks;
                    string nprops = "nonce=" + nonce + props;

                    string path = string.Format("/{0}/private/{1}", _version, a_sMethod);
                    string address = _url + path;
                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(address);
                    webRequest.ContentType = "application/x-www-form-urlencoded";
                    webRequest.Method = "POST";
                    webRequest.Headers.Add("API-Key", _key);


                    byte[] base64DecodedSecred = Convert.FromBase64String(_secret);

                    var np = nonce + Convert.ToChar(0) + nprops;

                    var pathBytes = Encoding.UTF8.GetBytes(path);
                    var hash256Bytes = sha256_hash(np);
                    var z = new byte[pathBytes.Count() + hash256Bytes.Count()];
                    pathBytes.CopyTo(z, 0);
                    hash256Bytes.CopyTo(z, pathBytes.Count());

                    var signature = getHash(base64DecodedSecred, z);

                    webRequest.Headers.Add("API-Sign", Convert.ToBase64String(signature));

                    if (nprops != null)
                    {

                        using (var writer = new StreamWriter(webRequest.GetRequestStream()))
                        {
                            writer.Write(nprops);
                        }
                    } 
                    #endregion

                    //Make the request
                    try
                    {
                        using (WebResponse webResponse = webRequest.GetResponse())
                        {
                            using (Stream str = webResponse.GetResponseStream())
                            {
                                using (StreamReader sr = new StreamReader(str))
                                {
                                    return (JsonObject)JsonConvert.Import(sr);
                                }
                            }
                        }
                    }
                    catch (WebException wex)
                    {
                        #region Handle Exception

                        _logger.Debug(string.Format("Try {0} of {1} failed.", tries, _maxRetries));

                        if (wex.Response != null)
                        {
                            using (HttpWebResponse response = (HttpWebResponse)wex.Response)
                            {
                                _logger.Debug(string.Format("WebException. StatusCode: {0}, StatusDescription: {1}", response.StatusCode, response.StatusDescription));
                            }
                        }

                        _logger.Debug(string.Format("WebException. Message: {0}",wex.Message));

                        
                        if (tries < _maxRetries)
                        {
                            _logger.Debug("Retrying...");
                            tries++;
                            keepTrying = true;
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            _logger.Error(string.Format("Maximum number of tries ({0}) reached. Throwing...", _maxRetries));
                            throw;
                        }
                      
                        #endregion                                              
                    }
                }
                
                //not supposed to get here
                throw new Exception("Exited the try loop without anything happening.");
            }
            catch(Exception ex) 
            {
                throw new Exception(string.Format("Error in KrakenClient.QueryPrivate at line {0}: {1}", ex.LineNumber(), ex.Message));
            }
        }

        #region interface members

        public IList<PricePoint> CatchUp()
        { 
            //go back 30 days
            var res = GetPricePoints((DateTimeOffset.Now).Subtract(new TimeSpan(30, 0, 0, 0)), null);

            return res;
        }

        public IList<PricePoint> GetPricePoints(DateTimeOffset start, DateTimeOffset? end)
        {
            IList<PricePoint> pricePoints = new List<PricePoint>();

            using (var client = new ConnectorServiceClient())
            {
                DateTimeOffset endTime = end ?? DateTimeOffset.UtcNow;
                Log(LogEntryImportance.Info, "  Fetching recent price data...");
                pricePoints = client.GetPricePointsByDate(start.UtcDateTime, endTime.UtcDateTime); 
                Log(LogEntryImportance.Info, "  Done fetching price data.");
                if (pricePoints == null)
                {
                    throw new Exception("null returned by connector service");
                }
                else
                {
                    Log(LogEntryImportance.Info, string.Format("  {0} pricePoints retrieved", pricePoints.Count));
                }
            }

            return pricePoints;
        }

        public GetCandleStickResult GetCandleStick(long last)
        {
            GetCandleStickResult getCandleStickResult = new GetCandleStickResult();
            
            try
            {
                var result = GetRecentTrades("XXBTZEUR", last);

                JsonArray error = (JsonArray)result["error"];
                if (error.Count() > 0)
                {
                    getCandleStickResult.ResultType = GetCandleStickResultType.error;
                    List<string> errorList = new List<string>();
                    foreach (var item in error)
                    {
                        errorList.Add(item.ToString());
                    }
                    getCandleStickResult.Errors = errorList;
                    return getCandleStickResult;
                }
                else
                {

                    JsonObject trades = (JsonObject)result["result"];

                    var ts = (string[][])JsonConvert.Import(typeof(string[][]), trades["XXBTZEUR"].ToString());

                    _logger.Info(string.Format("{0} trades returned ", ts.Count()));

                    var pricePoints = new List<PricePoint>();

                    foreach (var item in ts)
                    {
                        pricePoints.Add(new PricePoint(item[2].ToString(), item[0].ToString(), item[1].ToString()));
                    }

                    CandleSticks candleStick = null;
                    if (pricePoints.Count > 0)
                    {
                        _logger.Info("Building candlestick...");
                        DateTimeOffset openTime = pricePoints.First()._Time;
                        DateTimeOffset closeTime = pricePoints.Last()._Time;
                        decimal openPrice = pricePoints.First()._Price;
                        decimal closePrice = pricePoints.Last()._Price;
                        decimal high = pricePoints.Max(p => p._Price);
                        decimal low = pricePoints.Min(p => p._Price);

                        candleStick = new CandleSticks(openTime, closeTime, openPrice, high, low, closePrice);
                        
                        _logger.Info(string.Format("OpenTime: {0}, CloseTime: {1}, Open: {2}, High: {3}, Low: {4}, Close: {5}", openTime, closeTime, openPrice, high, low, closeTime));

                    }

                    getCandleStickResult = new GetCandleStickResult()
                    {
                        ResultType = GetCandleStickResultType.success,
                        CandleStick = candleStick,
                        Last = long.Parse(trades["last"].ToString())
                    };

                    return getCandleStickResult;
                }
            }
            catch (Exception ex)
            {
                getCandleStickResult.ResultType = GetCandleStickResultType.exception;
                getCandleStickResult.Exception = ex;
                return getCandleStickResult;
            }
        }

        
        /// <summary>
        /// Submit an order to Kraken. 
        /// </summary>
        /// <param name="order">Order to submit.</param>
        /// <param name="wait">If set to true, the function will wait until the order is closed or canceled.</param>
        /// <returns>PlaceOrderResult containing info about eventual success or failure of the request</returns>
        public PlaceOrderResult PlaceOrder(Orders order, bool wait)
        {
            PlaceOrderResult placeOrderResult = new PlaceOrderResult();
            placeOrderResult.Order = order;

            try
            {
                JsonObject res = AddOrder(order);

                JsonArray error = (JsonArray)res["error"];
                if (error.Count() > 0)
                {
                    placeOrderResult.ResultType = PlaceOrderResultType.error;
                    List<string> errorList = new List<string>();
                    foreach (var item in error)
                    {
                        errorList.Add(item.ToString());
                    }
                    placeOrderResult.Errors = errorList;
                    return placeOrderResult;
                }
                else
                {

                    JsonObject result = (JsonObject)res["result"];
                    JsonObject descr = (JsonObject)result["descr"];
                    JsonArray txid = (JsonArray)result["txid"];

                    if (txid == null)
                    {
                        placeOrderResult.ResultType = PlaceOrderResultType.txid_null;
                        return placeOrderResult;
                    }
                    else
                    {
                        Orders newOrder = order;
                        
                        string transactionIds = "";

                        foreach (var item in txid)
                        {
                            transactionIds += item.ToString() + ",";
                        }
                        transactionIds = transactionIds.TrimEnd(',');

                        newOrder.TxId = transactionIds;

                        if (wait)
                        {
                            #region Repeatedly check order status by calling RefreshOrder
                            bool keepSpinning = true;
                            while (keepSpinning)
                            {
                                RefreshOrderResult refreshOrderResult = RefreshOrder(newOrder);
                                newOrder = refreshOrderResult.Order;

                                switch (refreshOrderResult.ResultType)
                                {
                                    case RefreshOrderResultType.success:
                                        placeOrderResult.Order = newOrder;   
                                        switch (newOrder.Status)
                                        {
                                            case "closed":
                                                placeOrderResult.ResultType = PlaceOrderResultType.success;                                               
                                                return placeOrderResult;
                                            case "pending":
                                                break;
                                            case "open":
                                                break;
                                            case "canceled":
                                                if (newOrder.VolumeExecuted > 0)
                                                {
                                                    placeOrderResult.ResultType = PlaceOrderResultType.partial;
                                                    return placeOrderResult;
                                                }
                                                else
                                                {
                                                    placeOrderResult.ResultType = PlaceOrderResultType.canceled_not_partial;
                                                    return placeOrderResult;
                                                }
                                            default:
                                                throw new Exception(string.Format("Unknown type of order status: {0}", newOrder.Status));
                                        }
                                        break;
                                    case RefreshOrderResultType.error:
                                        HandleErrors(refreshOrderResult.Errors);
                                        break;
                                    case RefreshOrderResultType.order_not_found:
                                        throw new Exception("An error occured while trying to refresh the order.\nOrder not found");

                                    case RefreshOrderResultType.exception:
                                        string exceptionMessage = string.Format("An unexpected exception occured while trying to refresh the order: {0}", refreshOrderResult.Exception.Message);
                                        if (refreshOrderResult.Exception.InnerException != null)
                                        {
                                            exceptionMessage += string.Format(" \n InnerException: ", refreshOrderResult.Exception.InnerException.Message);
                                        }
                                        throw new Exception(exceptionMessage, refreshOrderResult.Exception);

                                    default:
                                        keepSpinning = false;
                                        break;
                                }
                                Thread.Sleep(5000);
                            }
                            #endregion
                        }

                        placeOrderResult.ResultType = PlaceOrderResultType.success;
                        placeOrderResult.Order = newOrder;
                        return placeOrderResult;
                    }
                }
            }
            catch (Exception ex)
            {
                placeOrderResult.ResultType = PlaceOrderResultType.exception;
                placeOrderResult.Exception = ex;
                return placeOrderResult;
            }

        }

        /// <summary>
        /// Cancels the order
        /// </summary>
        /// <param name="order">Order to cancel</param>
        /// <returns>CancelOrderResult containing info about eventual success or failure of the request</returns>
        public CancelOrderResult CancelOrder(Orders order)
        {
            CancelOrderResult cancelOrderResult = new CancelOrderResult();
            try
            {
                JsonObject res = CancelOrder(order.TxId);
                JsonArray error = (JsonArray)res["error"];
                if (error.Count() > 0)
                {
                    cancelOrderResult.ResultType = CancelOrderResultType.error;
                    List<string> errorList = new List<string>();
                    foreach (var item in error)
                    {
                        errorList.Add(item.ToString());
                    }
                    cancelOrderResult.Errors = errorList;
                    return cancelOrderResult;
                }
                else
                {
                    JsonObject result = (JsonObject)res["result"];
                    var count = int.Parse(result["count"].ToString());
                    var pending = (string)result["pending"];

                    //modif martin
                    //handle refresh order result
                    var newOrder = RefreshOrder(order).Order;

                    cancelOrderResult.ResultType = CancelOrderResultType.success;
                    cancelOrderResult.Order = newOrder;
                    cancelOrderResult.OrdersCanceled = count;
                    cancelOrderResult.OrdersPending = pending;

                    return cancelOrderResult;
                }
            }
            catch (Exception ex)
            {
                cancelOrderResult.ResultType = CancelOrderResultType.exception;
                cancelOrderResult.Exception = ex;
                return cancelOrderResult;
            }
        }

        public void HandleErrors(List<string> errors)
        {
            bool handled = false;
            string problematicErrorMessages = string.Empty;
            foreach (var errorMessage in errors)
            {
                if (errorMessage.Contains("Invalid nonce"))
                {
                    Log(LogEntryImportance.Info, "Invalid-nonce exception encountered. Keep spinning.");
                    handled = true;
                }
                else if (errorMessage.Contains("Rate limit exceeded"))
                {
                    Log(LogEntryImportance.Info, "API rate-limit exceeded.");
                    Log(LogEntryImportance.Info, "  Sleeping 10 extra seconds...");
                    Thread.Sleep(10000);
                    handled = true;
                }
                else
                {
                    Log(LogEntryImportance.Info, errorMessage);
                    problematicErrorMessages += string.Format(", {0}", errorMessage);
                    handled = false;
                }
            }
            if (!handled)
            {
                throw new Exception(string.Format("Unhandled API Error(s): {0}", problematicErrorMessages));
            }
        }

        public void HandleException(Exception ex)
        {
            bool handled = false;

            if (ex.Message.Contains("The request was aborted: The connection was closed unexpectedly"))
            {
                Log(LogEntryImportance.Error, string.Format("WebException of type 'request aborted'. Trying again..."));
                handled = true;
            }


            if (!handled)
            {
                Log(LogEntryImportance.Debug, "Exception not handled");
                throw ex;
            }

        }

        #endregion

        #region Helpers

        /// <summary>
        /// Call Kraken to update info about order execution.
        /// </summary>
        /// <param name="order">Order to update</param>
        /// <returns>RefreshOrderResult containing info about eventual success or failure of the request</returns>
        private RefreshOrderResult RefreshOrder(Orders order)
        {
            RefreshOrderResult refreshOrderResult = new RefreshOrderResult();
            refreshOrderResult.Order = order;

            try
            {
                if (order == null)
                {
                    Log(LogEntryImportance.Error, "'Argument order' is null");
                    throw new ArgumentNullException("order");
                }

                JsonObject res = QueryOrders(order.TxId);
                if (res == null)
                {
                    Log(LogEntryImportance.Error, "res is null in RefreshOrder");
                }

                JsonArray error = (JsonArray)res["error"];
                if (error.Count() > 0)
                {
                    refreshOrderResult.ResultType = RefreshOrderResultType.error;
                    List<string> errorList = new List<string>();
                    foreach (var item in error)
                    {
                        errorList.Add(item.ToString());
                    }
                    refreshOrderResult.Errors = errorList;
                    return refreshOrderResult;
                }
                else
                {
                    var newOrder = order;

                    JsonObject result = (JsonObject)res["result"];
                    if (result != null)
                    {

                        JsonObject orderDetails = (JsonObject)result[order.TxId];

                        if (orderDetails == null)
                        {
                            refreshOrderResult.ResultType = RefreshOrderResultType.order_not_found;
                            return refreshOrderResult;
                        }
                        else
                        {
                            string status = (orderDetails["status"] != null) ? orderDetails["status"].ToString() : null;
                            string reason = (orderDetails["reason"] != null) ? orderDetails["reason"].ToString() : null;
                            string openTime = (orderDetails["opentm"] != null) ? orderDetails["opentm"].ToString() : null;
                            string closeTime = (orderDetails["closetm"] != null) ? orderDetails["closetm"].ToString() : null;
                            string vol_exec = (orderDetails["vol_exec"] != null) ? orderDetails["vol_exec"].ToString() : null;
                            string cost = (orderDetails["cost"] != null) ? orderDetails["cost"].ToString() : null;
                            string fee = (orderDetails["fee"] != null) ? orderDetails["fee"].ToString() : null;
                            string price = (orderDetails["price"] != null) ? orderDetails["price"].ToString() : null;
                            string misc = (orderDetails["misc"] != null) ? orderDetails["misc"].ToString() : null;
                            string oflags = (orderDetails["oflags"] != null) ? orderDetails["oflags"].ToString() : null;
                            JsonArray tradesArray = (JsonArray)orderDetails["trades"];
                            string trades = null;
                            if (tradesArray != null)
                            {

                                foreach (var item in tradesArray)
                                {
                                    trades += item.ToString() + ",";
                                }
                                trades = trades.TrimEnd(',');
                            }

                            newOrder.Status = status;
                            newOrder.Reason = reason;
                            newOrder.OpenTime = openTime;
                            newOrder.CloseTime = closeTime;
                            newOrder.VolumeExecuted = decimal.Parse(vol_exec);
                            newOrder.Cost = decimal.Parse(cost);
                            newOrder.Fee = decimal.Parse(fee);
                            newOrder.AveragePrice = decimal.Parse(price);
                            order.Info = misc;
                            newOrder.OFlags = oflags;
                            newOrder.Trades = trades;

                        }
                    }
                    refreshOrderResult.ResultType = RefreshOrderResultType.success;
                    refreshOrderResult.Order = newOrder;
                    return refreshOrderResult;
                }
            }
            catch (Exception ex)
            {
                Log(LogEntryImportance.Error, string.Format("Exception at line {0}", ex.LineNumber()));
                refreshOrderResult.ResultType = RefreshOrderResultType.exception;
                refreshOrderResult.Exception = ex;
                return refreshOrderResult;
            }
        }

        #endregion

        #region Public queries


        //Get public server time
        //This is to aid in approximatig the skew time between the server and client
        public JsonObject GetServerTime()
        {
            return QueryPublic("Time") as JsonObject;
        }

        // Get a public list of active assets and their properties:
        /** 
        * Returned assets are keyed by their ISO-4217-A3-X names, example output:
        * 
        * Array
        * (
        *     [error] => Array
        *         (
        *         )
        * 
        *     [result] => Array
        *         (
        *             [XBTC] => Array
        *                 (
        *                     [aclass] => currency
        *                     [altname] => BTC
        *                     [decimals] => 10
        *                     [display_decimals] => 5
        *                 )
        * 
        *             [XLTC] => Array
        *                 (
        *                     [aclass] => currency
        *                     [altname] => LTC
        *                     [decimals] => 10
        *                     [display_decimals] => 5
        *                 )
        * 
        *             [XXRP] => Array
        *                 (
        *                     [aclass] => currency
        *                     [altname] => XRP
        *                     [decimals] => 8
        *                     [display_decimals] => 5
        *                 )
        * 
        *             [ZEUR] => Array
        *                 (
        *                     [aclass] => currency
        *                     [altname] => EUR
        *                     [decimals] => 4
        *                     [display_decimals] => 2
        *                 )
        *             ...
        * )
        */
        public JsonObject GetActiveAssets()
        {
            return QueryPublic("Assets") as JsonObject;
        }

        //Get a public list of tradable asset pairs
        public JsonObject GetAssetPairs(List<string> pairs)
        {
            StringBuilder pairString = new StringBuilder();

            if (pairs == null)
            {
                //do nothing, had to pass an empty string.

            }
            else if (pairs.Count() == 0)
            {
                return null;
            }
            else
            {
                pairString.Append("pair=");

                foreach (var item in pairs)
                {
                    pairString.Append(item + ",");
                }
                pairString.Length--; //disregard trailing comma    
            }

            return QueryPublic("AssetPairs", pairString.ToString()) as JsonObject;
        }

        // Get public ticker info for BTC/USD pair:
        /**
         * Example output:
         *
         * Array
         * (
         *     [error] => Array
         *         (
         *         )
         * 
         *     [result] => Array
         *         (
         *             [XBTCZUSD] => Array
         *                 (
         *                     [a] => Array
         *                         (
         *                             [0] => 106.09583
         *                             [1] => 111
         *                         )
         * 
         *                     [b] => Array
         *                         (
         *                             [0] => 105.53966
         *                             [1] => 4
         *                         )
         * 
         *                     [c] => Array
         *                         (
         *                             [0] => 105.98984
         *                             [1] => 0.13910102
         *                         )
         * 
         *                     ...
         *         )
         * )
         */
        public JsonObject GetTicker(List<string> pairs)
        {

            if (pairs == null)
            {
                return null;
            }
            if (pairs.Count() == 0)
            {
                return null;
            }

            StringBuilder pairString = new StringBuilder("pair=");
            foreach (var item in pairs)
            {
                pairString.Append(item + ",");
            }
            pairString.Length--; //disregard trailing comma


            return QueryPublic("Ticker", pairString.ToString()) as JsonObject;
        }


        //Get public order book
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pairs">asset pair to get market depth for</param>
        /// <param name="count">maximum number of ask/bids (optional)</param>
        /// <returns>
        /// pair_name = pair name
        ///             asks = ask side array of array entries([price], [volume], [timestamp])
        ///             bids = bid side array of array entries([price], [volume], [timestamp])
        /// </returns>
        public JsonObject GetOrderBook(string pair, int? count = null)
        {

            string reqs = string.Format("pair={0}", pair);

            if (count.HasValue)
            {

                reqs += string.Format("&count={0}", count.Value.ToString());
            }

            return QueryPublic("Depth", reqs) as JsonObject;

        }

        /// <summary>
        /// Get recent trades
        /// </summary>
        /// <param name="pair">asset pair to get trade data for</param>
        /// <param name="since">return trade data since given id (exclusive). Unix Time</param>
        /// <remarks> NOTE: the 'since' parameter is subject to change in the future: it's precision may be modified,
        ///             and it may no longer be representative of a timestamp. The best practice is to base it
        ///             on the 'last' value returned in the result set. 
        ///  </remarks>
        /// <returns></returns>
        public JsonObject GetRecentTrades(string pair, long since)
        {
            string reqs = string.Format("pair={0}", pair);


            reqs += string.Format("&since={0}", since.ToString());


            return QueryPublic("Trades", reqs) as JsonObject;
        }


        /// <summary>
        /// Get recent spread data
        /// </summary>
        /// <param name="pair">asset pair to get trade data for</param>
        /// <param name="since">return trade data since given id (inclusive). Unix Time</param>
        /// <remarks> NOTE: Note: "since" is inclusive so any returned data with the same time as the previous
        /// set should overwrite all of the previous set's entries at that time
        ///  </remarks>
        /// <returns></returns>
        public JsonObject GetRecentSpreadData(string pair, long since)
        {
            string reqs = string.Format("pair={0}", pair);


            reqs += string.Format("&since={0}", since.ToString());


            return QueryPublic("Spread", reqs) as JsonObject;
        }


        #endregion//public queries

        #region Private user data queries

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// array of asset names and balance amount
        /// </returns>
        public JsonObject GetBalance()
        {
            return QueryPrivate("Balance") as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aclass">asset class (optional): currency (default)</param>
        /// <param name="asset">base asset used to determine balance (default = ZUSD)</param>
        /// <returns>
        /// tb = trade balance (combined balance of all currencies)
        /// m = initial margin amount of open positions
        /// n = unrealized net profit/loss of open positions
        /// c = cost basis of open positions
        /// v = current floating valuation of open positions
        /// e = equity = trade balance + unrealized net profit/loss
        /// mf = free margin = equity - initial margin (maximum margin available to open new positions)
        /// ml = margin level = (equity / initial margin) * 100
        /// </returns>
        public JsonObject GetTradeBalance(string aclass, string asset)
        {
            string reqs = "";
            if (string.IsNullOrEmpty(aclass))
            {
                reqs += string.Format("&aclass={0}", aclass);
            }
            if (string.IsNullOrEmpty(aclass))
            {
                reqs += string.Format("&asset={0}", asset);
            }

            return QueryPrivate("TradeBalance", reqs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trades">whether or not to include trades in output (optional.  default = false)</param>
        /// <param name="userref">restrict results to given user reference id (optional)</param>
        /// <returns>
        /// refid = Referral order transaction id that created this order
        ///userref = user reference id
        ///status = status of order:
        ///  pending = order pending book entry
        ///open = open order
        ///closed = closed order
        ///canceled = order canceled
        ///expired = order expired
        ///opentm = unix timestamp of when order was placed
        ///starttm = unix timestamp of order start time (or 0 if not set)
        ///expiretm = unix timestamp of order end time (or 0 if not set)
        ///descr = order description info
        /// pair = asset pair
        /// type = type of order (buy/sell)
        /// ordertype = order type (See Add standard order)
        /// price = primary price
        /// price2 = secondary price
        /// leverage = amount of leverage
        /// position = position tx id to close (if order is positional)
        /// order = order description
        /// close = conditional close order description (if conditional close set)
        ///vol = volume of order (base currency unless viqc set in oflags)
        ///vol_exec = volume executed (base currency unless viqc set in oflags)
        ///cost = total cost (quote currency unless unless viqc set in oflags)
        ///fee = total fee (quote currency)
        ///price = average price (quote currency unless viqc set in oflags)
        ///stopprice = stop price (quote currency, for trailing stops)
        ///limitprice = triggered limit price (quote currency, when limit based order type triggered)
        ///misc = comma delimited list of miscellaneous info
        ///  stopped = triggered by stop price
        ///  touched = triggered by touch price
        ///  liquidated = liquidation
        ///  partial = partial fill
        ///oflags = comma delimited list of order flags
        /// viqc = volume in quote currency
        ///  plbc = prefer profit/loss in base currency
        ///  nompp = no market price protection 
        ///trades = array of trade ids related to order (if trades info requested and data available)


        /// <summary>
        /// 
        /// </summary>
        /// <param name="trades"></param>
        /// <param name="userref"></param>
        /// <returns></returns>
        /// </returns>
        public JsonObject GetOpenOrders(bool trades = false, string userref = "")
        {
            string reqs = string.Format("&trades={0}", true);

            if (!string.IsNullOrEmpty(userref))
                reqs += string.Format("&userref={1}", userref);

            return QueryPrivate("OpenOrders", reqs) as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trades">whether or not to include trades in output (optional.  default = false)</param>
        /// <param name="userref">restrict results to given user reference id (optional)</param>
        /// <param name="start">starting unix timestamp or order tx id of results (optional.  exclusive)></param>
        /// <param name="end">ending unix timestamp or order tx id of results (optional.  inclusive)</param>
        /// <param name="ofs">result offset</param>
        /// <param name="closetime"> which time to use (optional) [open close both] (default)</param>
        /// <returns></returns>
        ///  /// <remarks>Note: Times given by order tx ids are more accurate than unix timestamps. If an order tx id is given for the time, the order's open time is used</remarks>
        public JsonObject GetClosedOrders(bool trades = false, string userref = "", string start = "", string end = "", string ofs = "", string closetime = "both")
        {
            string reqs = string.Format("&trades={0}&closetime={1}", trades, closetime);
            if (!string.IsNullOrEmpty(userref))
                reqs += string.Format("&useref={0}", userref);
            if (!string.IsNullOrEmpty(start))
                reqs += string.Format("&start={0}", start);
            if (!string.IsNullOrEmpty(end))
                reqs += string.Format("&end={0}", end);
            if (!string.IsNullOrEmpty(ofs))
                reqs += string.Format("&ofs={0}", ofs);


            return QueryPrivate("ClosedOrders", reqs) as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txid">comma delimited list of transaction ids to query info about (20 maximum)</param>
        /// <param name="trades">whether or not to include trades in output (optional.  default = false)</param>
        /// <param name="userref">restrict results to given user reference id (optional)</param>
        /// <returns></returns>
        public JsonObject QueryOrders(string txid, bool trades = false, string userref = "")
        {
            string reqs = string.Format("&txid={0}&trades={1}", txid, trades);
            if (!string.IsNullOrEmpty(userref))
                reqs += string.Format("&userref={0}", userref);

            return QueryPrivate("QueryOrders", reqs) as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ofs">result offset</param>
        /// <param name="type">type of trade (optional) [all = all types (default), any position = any position (open or closed), closed position = positions that have been closed, closing position = any trade closing all or part of a position, no position = non-positional trades]</param>
        /// <param name="trades">whether or not to include trades related to position in output (optional.  default = false)</param>
        /// <param name="start">starting unix timestamp or trade tx id of results (optional.  exclusive)</param>
        /// <param name="end">ending unix timestamp or trade tx id of results (optional.  inclusive)</param>
        /// <returns></returns>
        public JsonObject GetTradesHistory(string ofs = "", string type = "all", bool trades = false, string start = "", string end = "")
        {
            string reqs = string.Format("&ofs={0}&type={1}&trades={2}", ofs, type, trades);
            if (!string.IsNullOrEmpty(start))
                reqs += string.Format("&start={0}", start);
            if (!string.IsNullOrEmpty(end))
                reqs += string.Format("&end={0}", end);
            return QueryPrivate("TradesHistory", reqs) as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txid">comma delimited list of transaction ids to query info about (20 maximum)</param>
        /// <param name="trades">whether or not to include trades related to position in output (optional.  default = false)</param>
        /// <returns></returns>
        public JsonObject QueryTrades(string txid = "", bool trades = false)
        {
            string reqs = string.Format("&txid={0}&trades={1}", txid, trades);
            return QueryPrivate("QueryTrades", reqs) as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txid">comma delimited list of transaction ids to query info about (20 maximum)</param>
        /// <param name="docalcs">whether or not to include profit/loss calculations (optional.  default = false)</param>
        /// <returns>
        /// <position_txid> = open position info
        /// ordertxid = order responsible for execution of trade
        /// pair = asset pair
        /// time = unix timestamp of trade
        /// type = type of order used to open position (buy/sell)
        /// ordertype = order type used to open position
        /// cost = opening cost of position (quote currency unless viqc set in oflags)
        /// fee = opening fee of position (quote currency)
        /// vol = position volume (base currency unless viqc set in oflags)
        /// vol_closed = position volume closed (base currency unless viqc set in oflags)
        /// margin = initial margin (quote currency)
        /// value = current value of remaining position (if docalcs requested.  quote currency)
        /// net = unrealized profit/loss of remaining position (if docalcs requested.  quote currency, quote currency scale)
        /// misc = comma delimited list of miscellaneous info
        /// oflags = comma delimited list of order flags
        /// viqc = volume in quote currency
        /// </returns>
        public JsonObject GetOpenPositions(string txid = "", bool docalcs = false)
        {
            string reqs = string.Format("&txid={0}&docalcs={1}", txid, docalcs);
            return QueryPrivate("OpenPositions", reqs) as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aclass">asset class (optional): currency (default)</param>
        /// <param name="asset">comma delimited list of assets to restrict output to (optional.  default = all) </param>
        /// <param name="type">type of ledger to retrieve (optional):[all(default) deposit withdrawal trade margin]</param>
        /// <param name="start">starting unix timestamp or ledger id of results (optional.  exclusive)</param>
        /// <param name="end">ending unix timestamp or ledger id of results (optional.  inclusive)</param>
        /// <param name="ofs">result offset</param>
        /// <returns>
        /// <ledger_id> = ledger info
        ///refid = reference id
        ///time = unx timestamp of ledger
        /// type = type of ledger entry
        ///aclass = asset class
        ///asset = asset
        ///amount = transaction amount
        ///fee = transaction fee
        ///balance = resulting balance
        /// </returns>
        public JsonObject GetLedgers(string aclass = "currency", string asset = "all", string type = "all", string start = "", string end = "", string ofs = "")
        {
            string reqs = string.Format("&ofs={0}", ofs);
            if (!string.IsNullOrEmpty(aclass))
                reqs += string.Format("&aclass={0}", asset);
            if (!string.IsNullOrEmpty(type))
                reqs += string.Format("&type={0}", type);
            if (!string.IsNullOrEmpty(start))
                reqs += string.Format("&start={0}", start);
            if (!string.IsNullOrEmpty(end))
                reqs += string.Format("&end={0}", end);
            return QueryPrivate("Ledgers", reqs) as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">comma delimited list of ledger ids to query info about (20 maximum)</param>
        /// <returns><ledger_id> = ledger info.  See Get ledgers info</returns>
        public JsonObject QueryLedgers(string id = "")
        {
            string reqs = string.Format("&id={0}", id);
            return QueryPrivate("QueryLedgers", reqs) as JsonObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pair">comma delimited list of asset pairs to get fee info on (optional)</param>
        /// <returns>currency = volume currency
        ///volume = current discount volume
        ///fees = array of asset pairs and fee tier info (if requested)
        ///fee = current fee in percent
        ///minfee = minimum fee for pair (if not fixed fee)
        ///maxfee = maximum fee for pair (if not fixed fee)
        ///nextfee = next tier's fee for pair (if not fixed fee.  nil if at lowest fee tier)
        ///nextvolume = volume level of next tier (if not fixed fee.  nil if at lowest fee tier)
        ///tiervolume = volume level of current tier (if not fixed fee.  nil if at lowest fee tier)
        ///</returns>
        public JsonObject GetTradeVolume(string pair = "")
        {
            string reqs = string.Format("&pair={0}", pair);
            return QueryPrivate("TradeVolume", reqs) as JsonObject;
        }

        #endregion

        #region Private user trading

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pair">asset pair</param>
        /// <param name="type">type of order (buy/sell)</param>
        /// <param name="ordertype">ordertype = order type:
        ///market
        ///limit (price = limit price)
        ///stop-loss (price = stop loss price)
        ///take-profit (price = take profit price)
        ///stop-loss-profit (price = stop loss price, price2 = take profit price)
        ///stop-loss-profit-limit (price = stop loss price, price2 = take profit price)
        ///stop-loss-limit (price = stop loss trigger price, price2 = triggered limit price)
        ///take-profit-limit (price = take profit trigger price, price2 = triggered limit price)
        ///trailing-stop (price = trailing stop offset)
        ///trailing-stop-limit (price = trailing stop offset, price2 = triggered limit offset)
        ///stop-loss-and-limit (price = stop loss price, price2 = limit price)</param>
        /// <param name="volume">order volume in lots</param>
        /// <param name="price">price (optional.  dependent upon ordertype)</param>
        /// <param name="price2">secondary price (optional.  dependent upon ordertype)</param>
        /// <param name="leverage">amount of leverage desired (optional.  default = none)</param>
        /// <param name="position">position tx id to close (optional.  used to close positions)</param>
        /// <param name="oflags">oflags = comma delimited list of order flags (optional):
        ///viqc = volume in quote currency
        ///plbc = prefer profit/loss in base currency
        ///nompp = no market price protection</param>
        /// <param name="starttm">scheduled start time (optional):
        ///0 = now (default)
        ///+<n> = schedule start time <n> seconds from now
        ///<n> = unix timestamp of start time</param>
        /// <param name="expiretm">expiration time (optional):
        /// 0 = no expiration (default)
        ///+<n> = expire <n> seconds from now
        ///<n> = unix timestamp of expiration time</param>
        /// <param name="userref">user reference id.  32-bit signed number.  (optional)</param>
        /// <param name="validate">validate inputs only.  do not submit order (optional)</param>
        /// <param name="close">optional closing order to add to system when order gets filled:
        ///close[ordertype] = order type
        ///close[price] = price
        ///close[price2] = secondary price</param>
        /// <returns>
        /// descr = order description info
        ///order = order description
        ///close = conditional close order description (if conditional close set)
        ///txid = array of transaction ids for order (if order was added successfully)
        /// </returns>
        public JsonObject AddOrder(string pair,
            string type,
            string ordertype,
            decimal volume,
            decimal? price,
            decimal? price2,
            string leverage = "none",
            string position = "",
            string oflags = "",
            string starttm = "",
            string expiretm = "",
            string userref = "",
            bool validate = false,
            Dictionary<string, string> close = null)
        {
            string reqs = string.Format("&pair={0}&type={1}&ordertype={2}&volume={3}&leverage={4}", pair, type, ordertype, volume, leverage);
            if (price.HasValue)
                reqs += string.Format("&price={0}", price.Value);
            if (price2.HasValue)
                reqs += string.Format("&price2={0}", price2.Value);
            if (!string.IsNullOrEmpty(position))
                reqs += string.Format("&position={0}", position);
            if (!string.IsNullOrEmpty(starttm))
                reqs += string.Format("&starttm={0}", starttm);
            if (!string.IsNullOrEmpty(expiretm))
                reqs += string.Format("&expiretm={0}", expiretm);
            if (!string.IsNullOrEmpty(oflags))
                reqs += string.Format("&oflags={0}", oflags);
            if (!string.IsNullOrEmpty(userref))
                reqs += string.Format("&userref={0}", userref);
            if (validate)
                reqs += "&validate=true";
            if (close != null)
            {
                string closeString = string.Format("&close[ordertype]={0}&close[price]={1}&close[price2]={2}", close["ordertype"], close["price"], close["price2"]);
                reqs += closeString;
            }
            return QueryPrivate("AddOrder", reqs) as JsonObject;
        }

        public JsonObject AddOrder(Orders krakenOrder)
        {
            return AddOrder(pair: krakenOrder.Pair,
                            type: krakenOrder.Type,
                            ordertype: krakenOrder.OrderType,
                            volume: krakenOrder.Volume,
                            price: krakenOrder.Price,
                            price2: krakenOrder.Price2,
                            leverage: krakenOrder.Leverage ?? "none",
                            position: krakenOrder.Position ?? string.Empty,
                            oflags: krakenOrder.OFlags ?? string.Empty,
                            starttm: krakenOrder.Starttm ?? string.Empty,
                            expiretm: krakenOrder.Expiretm ?? string.Empty,
                            userref: krakenOrder.UserRef ?? string.Empty,
                            validate: krakenOrder.Validate,
                            close: null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txid">transaction id</param>
        /// <returns>
        /// count = number of orders canceled
        ///pending = if set, order(s) is/are pending cancellation
        /// </returns>
        public JsonObject CancelOrder(string txid)
        {
            string reqs = string.Format("&txid={0}", txid);
            return QueryPrivate("CancelOrder", reqs);
        }

        #endregion

        #region Helper methods

        private byte[] sha256_hash(String value)
        {
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;

                Byte[] result = hash.ComputeHash(enc.GetBytes(value));

                return result;
            }
        }

        private byte[] getHash(byte[] keyByte, byte[] messageBytes)
        {
            using (var hmacsha512 = new HMACSHA512(keyByte))
            {

                Byte[] result = hmacsha512.ComputeHash(messageBytes);

                return result;

            }
        }

        protected void Log(LogEntryImportance importance, string message)
        {

            //log to file
            switch (importance)
            {
                case LogEntryImportance.Info:
                    _logger.Info(message);
                    break;
                case LogEntryImportance.Debug:
                    _logger.Debug(message);
                    break;
                case LogEntryImportance.Error:
                    _logger.Error(message);
                    break;
            }

        }

        #endregion
    }
}
