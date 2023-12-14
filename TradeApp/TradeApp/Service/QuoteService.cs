using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeApp.ApiClient;
using TradeApp.ConnectorService;
using TradeApp.DataAccess;
using TradeApp.Infrastructure;
using TradeApp.Infrastructure.Model;
using TradeApp.Messaging;
using TradeApp.Model;

namespace TradeApp.Service
{
    public class QuoteService : ServiceBase
    {

        private System.Timers.Timer _timer;

        public QuoteService(IExchangeClient exchangeClient) : base()
        {
            Client = exchangeClient;

            Mediator.Register(this);
        }


        #region Configuration Properties

        public IExchangeClient Client { get; set; }

        public TimeIntervals TimeIntervalType { get; set; }
        public int TimeIntervalValue { get; set; }
        public int WmaPeriod { get; set; }
        public decimal NNInterval { get; set; }
        public decimal PositionOpeningCost { get; set; }

        #endregion

        #region Helper Properties

        public bool IsBusy { get; set; }

        public long Last { get; set; }

        public TimeSpan TimeIncrement
        {
            get
            {
                TimeSpan timeIncrement = new TimeSpan();
                switch (TimeIntervalType)
                {
                    case TimeIntervals.Minute:
                        timeIncrement = new TimeSpan(0, TimeIntervalValue, 0);
                        break;
                    case TimeIntervals.Hour:
                        timeIncrement = new TimeSpan(TimeIntervalValue, 0, 0);
                        break;
                    case TimeIntervals.Day:
                        timeIncrement = new TimeSpan(TimeIntervalValue * 24, 0, 0);
                        break;
                    default:
                        break;
                }
                return timeIncrement;
            }
        }

        public long TimerMilliseconds
        {
            get
            {
                return (long)TimeIncrement.TotalMilliseconds;
            }

        }



        public FixedSizedQueue<CandleSticks> CandleSticks { get; set; }
        public FixedSizedQueue<PricePoint> WMAPoints { get; set; }

        #endregion

        #region Methods

        private void Run()
        {
            if (_timer != null && _timer.Enabled)
                return;

            try
            {
                _timer = new System.Timers.Timer(TimerMilliseconds);

                _timer.Elapsed += (s, e) =>
                {
                    Log(LogEntryImportance.Info, "QuoteBot timer elapsed", true);

                    _timer.Stop();

                    Log(LogEntryImportance.Info, "Loading price points...", true);
                    //LoadPricePoints();
                    var lastCandleStickResult = Client.GetCandleStick(Last);
                    Last = lastCandleStickResult.Last;
                    CandleSticks lastCandleStick = lastCandleStickResult.CandleStick;

                    if (lastCandleStick != null)
                    {
                        CandleSticks.Enqueue(lastCandleStick);

                        Log(LogEntryImportance.Info, "Calculating WMA...", true);
                        CalculateWMA();

                        //Modif 6/13/2014
                        //ajout l'info wma au dernier candlestick de la liste. On passe se candlestick au mediateur
                        var candleStick = CandleSticks.Last();
                        var wma = WMAPoints.Last();
                        candleStick.WMAValue = wma._Price;
                        candleStick.WMAPeriod = WmaPeriod;

                        Mediator.NotifyColleagues<PriceData>(MediatorMessages.NewPriceData, new PriceData(candleStick, WMAPoints.Last(), true));
                    }
                    else
                    {
                        Log(LogEntryImportance.Info, "No price points to process", true);
                    }

                    Log(LogEntryImportance.Info, "QuoteBot processing finished", true);

                    _timer.Start();
                    Log(LogEntryImportance.Info, "QuoteBot timer reset. Waiting...", true);

                };

                _timer.Start();

            }
            catch (Exception ex)
            {
                if (_timer != null)
                    _timer.Stop();
                Log(LogEntryImportance.Error, "There was an error. QuoteBot stopped. \n Details: " + ex.Message, true);
                throw;
            }
        }

        private void stop()
        {
            if (_timer != null)
                _timer.Stop();

            Log(LogEntryImportance.Info, "QuoteBot stopped.", true);
        }

        public void CatchUpWithConnector()
        {
            Log(LogEntryImportance.Info, "Catching up with connector...", true);

            List<PricePoint> pricePoints = new List<PricePoint>();


            try
            {
                #region Fetch Data

                Log(LogEntryImportance.Info, "  Fetching recent price data...", true);
                pricePoints = Client.CatchUp().ToList();
                Log(LogEntryImportance.Info, "  Done fetching price data.", true);
                if (pricePoints == null)
                {
                    throw new Exception("null returned by connector service");
                }
                else
                {
                    Log(LogEntryImportance.Info, string.Format("  {0} pricePoints retrieved", pricePoints.Count), true);
                }

                #endregion

                #region Build Candlesticks

                int lastPointIndex = pricePoints.Count - 1;
                int firstPointIndex = lastPointIndex;

                List<CandleSticks> data = new List<CandleSticks>();

                TimeSpan timeIncrement = TimeIncrement;
                while (data.Count() < 2 * WmaPeriod)
                {
                    DateTimeOffset firstTime = pricePoints[lastPointIndex]._Time.Subtract(timeIncrement);
                    int counter = 0;

                    while (pricePoints[firstPointIndex]._Time >= firstTime)
                    {
                        firstPointIndex--;
                        counter++;
                        if (firstPointIndex == 0) break;
                    }

                    DateTimeOffset openTime = pricePoints[firstPointIndex + 1]._Time;
                    DateTimeOffset closeTime = pricePoints[lastPointIndex]._Time;
                    decimal openPrice = pricePoints[firstPointIndex + 1]._Price;
                    decimal closePrice = pricePoints[lastPointIndex]._Price;
                    decimal high = pricePoints.GetRange(firstPointIndex + 1, counter).Max(p => p._Price);
                    decimal low = pricePoints.GetRange(firstPointIndex + 1, counter).Min(p => p._Price);

                    data.Add(new CandleSticks(openTime, closeTime, openPrice, high, low, closePrice));

                    lastPointIndex = firstPointIndex;

                }

                data.Reverse();

                #endregion

                #region Calculate WMA

                List<PricePoint> wma = new List<PricePoint>();

                int wmaFirstIndex = 1;
                int wmaLastIndex = WmaPeriod;

                while (wmaLastIndex < data.Count)
                {
                    var window = data.GetRange(wmaFirstIndex, WmaPeriod);

                    DateTimeOffset time = window.Last().CloseTime;

                    decimal wsum = 0;
                    int denominator = 0;
                    for (int i = 1; i <= WmaPeriod; i++)
                    {
                        denominator += i;
                        wsum += (i * window[i - 1].Close);
                    }
                    decimal wavg = wsum / denominator;

                    PricePoint temp = new PricePoint(time, wavg, 0.0);

                    wma.Add(temp);

                    //Modif 6/13/2014
                    //ajout l'info wma au dernier candlestick de la liste. On passe se candlestick au mediateur
                    var candleStick = data[wmaLastIndex];
                    candleStick.WMAValue = temp._Price;
                    candleStick.WMAPeriod = WmaPeriod;

                    Mediator.NotifyColleagues<PriceData>(MediatorMessages.NewPriceData, new PriceData(candleStick, temp, false));

                    wmaFirstIndex++;
                    wmaLastIndex++;

                }

                #endregion

                WMAPoints = new FixedSizedQueue<PricePoint>(wma);

                CandleSticks = new FixedSizedQueue<CandleSticks>(data);

                Last = PricePoint.DateTimeToUnixTimeNonoseconds(CandleSticks.Last().CloseTime);

                Log(LogEntryImportance.Info, "Done catching up with connector.", true);
            }
            catch (Exception ex)
            {
                Log(LogEntryImportance.Info, string.Format("An exception occured while retreiving data from connector. {0}", ex.Message), true);
                if (ex.InnerException != null)
                {
                    Log(LogEntryImportance.Info, string.Format("    InnerException: {0}", ex.InnerException.Message), true);
                }
                throw;
            }
        }

        private void LoadPricePoints()
        {

            var res = Client.GetCandleStick(Last);
            Last = res.Last;
            CandleSticks candleStick = res.CandleStick;

            if (candleStick != null)
                CandleSticks.Enqueue(candleStick);
        }

        private void CalculateWMA()
        {
            List<CandleSticks> candleStickList = CandleSticks.ToList().GetRange(WmaPeriod, WmaPeriod);
            decimal numerator = 0;
            decimal denominator = 0;
            for (int i = 1; i <= candleStickList.Count(); i++)
            {
                numerator += i * candleStickList[i - 1].Close;
                denominator += i;
            }
            PricePoint wma = new PricePoint(candleStickList.Last().CloseTime, numerator / denominator, 0);
            WMAPoints.Enqueue(wma);
        }

        #endregion 

        #region Message Handlers


        [MediatorMessageSink(MediatorMessages.TimeIntervalTypeChanged, ParameterType = typeof(TimeIntervals))]
        public void SetTimeIntervalType(TimeIntervals timeIntervalType)
        {
            TimeIntervalType = timeIntervalType;
        }

        [MediatorMessageSink(MediatorMessages.TimeIntervalValueChanged, ParameterType = typeof(int))]
        public void SetTimeIntervalValue(int timeIntervalValue)
        {
            TimeIntervalValue = timeIntervalValue;
        }

        [MediatorMessageSink(MediatorMessages.WmaPeriodChanged, ParameterType = typeof(int))]
        public void SetWmaPeriod(int wmaPeriod)
        {
            WmaPeriod = wmaPeriod;
        }

        [MediatorMessageSink(MediatorMessages.NNIntervalChanged, ParameterType = typeof(decimal))]
        public void SetNNInterval(decimal nnInterval)
        {
            NNInterval = nnInterval;
        }

        [MediatorMessageSink(MediatorMessages.PositionOpeningCostChanged, ParameterType = typeof(decimal))]
        public void SetPositionOpeningCost(decimal openingCost)
        {
            PositionOpeningCost = openingCost;
        }

        [MediatorMessageSink(MediatorMessages.CatchUpWithConnector, ParameterType = typeof(string))]
        public void CatchUpWithConnector(string message)
        {
            Task task = new Task(() =>
            {

                CatchUpWithConnector();

                Mediator.NotifyColleagues<string>(MediatorMessages.DoneCatchingUpWithConnector, "yo");

            });
            task.Start();
        }

        [MediatorMessageSink(MediatorMessages.StartQuoteBot, ParameterType = typeof(string))]
        public void Start(string message)
        {
            Task task = new Task(() =>
            {
                Run();

                Log(LogEntryImportance.Info, "QuoteBot timer set. Waiting...", true);
            });
            task.Start();
        }

        [MediatorMessageSink(MediatorMessages.StopQuoteBot, ParameterType = typeof(string))]
        public void Stop(string message)
        {
            stop();
            Mediator.NotifyColleagues<string>(MediatorMessages.StopStrategyService, "stop now!");
        }
        #endregion

        #region Dispose
        ~QuoteService()
        {
            // In case the client forgets to call
            // Dispose , destructor will be invoked for
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                CandleSticks = null;
                WMAPoints = null;
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
