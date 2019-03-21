using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeApp.ConnectorService;
using TradeApp.DataAccess;
using TradeApp.DataAccess.Factories;
using TradeApp.DataAccess.Repositories;
using TradeApp.Infrastructure;
using TradeApp.Infrastructure.Model;
using TradeApp.Messaging;
using TradeApp.Model;

namespace TradeApp.Service
{
    public class StrategyService:ServiceBase
    {

        ICandleStickRepository _candleStickRepository;
        IConfirmationRepository _confirmationRepository;
        ISetupRepository _setupRepository;

        public StrategyService()
        {
            _name = "StrategyService";
            _logger = LogManager.GetLogger(this.GetType().Name);
            Mediator.Register(this);

            _candleStickRepository = _mefLoader.CandleStickRepository;
            _confirmationRepository = _mefLoader.ConfirmationRepository;
            _setupRepository = _mefLoader.SetupRepository;

        }

        #region Configuration Fields

        TimeIntervals TimeIntervalType;
        int TimeIntervalValue;
        int WmaPeriod;
        decimal NNInterval;
        decimal PositionOpeningCost;
        bool EnableOrders;
        bool ValidateOnly = false;
        

        #endregion

        #region Helper Fields

        CandleSticks OldCandle;
        CandleSticks CurrentCandle;
        PricePoint Wma;
        Setups Setup;

        decimal highBound;
        decimal HighBound
        {
            get 
            {
                return highBound;
            }
            set 
            {
                highBound = value;
                Mediator.NotifyColleagues<decimal>(MediatorMessages.UpdateHighBound, highBound);
            }
        }

        decimal lowBound;
        decimal LowBound
        {
            get
            {
                return lowBound;
            }
            set
            {
                lowBound = value;
                Mediator.NotifyColleagues<decimal>(MediatorMessages.UpdateLowBound, lowBound);
            }
        }
        
        decimal OngoingContracts;
        object OngoingContractsLock = new Object();

        decimal? _positionEpsilon;
        decimal PositionEpsilon 
        {
            get 
            {
                if (!_positionEpsilon.HasValue)
                {
                    string sPositionEpsilon = ConfigurationManager.AppSettings["PositionEpsilon"];
                    _positionEpsilon = decimal.Parse(sPositionEpsilon, CultureInfo.InvariantCulture);
                }
                return _positionEpsilon.Value;
            }
            set 
            {
                _positionEpsilon = value;
            }
        }

        #endregion

        #region Methods

        void TreatNewPriceData(PriceData priceData)
        {

            bool logToConsole = priceData.Live;

            Log(LogEntryImportance.Info, "New PriceData received", logToConsole);

            //Update candles and WMA
            if (CurrentCandle != null)
            {
                OldCandle = CurrentCandle;
            }
            CurrentCandle = priceData.CandleStick;
            Wma = priceData.WmaPoint;

            Immortalize(CurrentCandle);

            //If setup is not null
            if (Setup != null)
            {
                Log(LogEntryImportance.Info, "Setup not null", logToConsole);
                
                //If setup is confirmed
                if (Setup.Confirmation != null)
                {
                    Log(LogEntryImportance.Info, "Confirmation not null", logToConsole);
                    switch (Setup.Type)
                    {
                        //Long Position
                        case (int)SetupType.up:
                            #region UP
                            //If the price closed above the upper limit
                            if (CurrentCandle.Close >= HighBound)
                            {
                                //shift limits up
                                UpdateLimits(CurrentCandle.Close);
                                Log(LogEntryImportance.Info,"Shifted limits up",logToConsole);

                                //if we re not trading yet, open a position
                                if (OngoingContracts==0) 
                                {
                                    OpenPosition();
                                }
                                //If we are in a long position
                                else if (OngoingContracts > 0)
                                {
                                    //Shift the stoploss order
                                    ShiftStopLoss();
                                }
                                //Negative position in long trend, BAD
                                else 
                                {                                    
                                    Log(LogEntryImportance.Error,string.Format("negative position of {0} contracts during up-trend. THIS IS BAD",OngoingContracts),logToConsole);
                                    Log(LogEntryImportance.Info, "Closing position", logToConsole);
                                    //Close the position
                                    ClosePosition();
                                }
                            }
                            //If the price closed below the lower bound
                            else if (CurrentCandle.Close <= LowBound)
                            { 
                                //If OngoingContracts is not 0
                                if (OngoingContracts != 0)
                                {
                                    //The stop loss was not executed. BAD
                                    Log(LogEntryImportance.Error, string.Format("position is {0} instead of 0. THIS IS BAD", OngoingContracts), logToConsole);
                                }

                                
                                //Check if trend is broken
                                //If there is a setup in the other direction
                                Setups tempSetup = DetermineSetup();
                                if (tempSetup != null)
                                {
                                    //Trend is broken
                                    Log(LogEntryImportance.Info, "Trend is broken", logToConsole);

                                    //if there is a pending opening order, close it
                                    Log(LogEntryImportance.Info, "Closing position", logToConsole);
                                    ClosePosition();

                                    //Set target
                                    Setup = tempSetup;
                                    Log(LogEntryImportance.Info, "New Setup detected", logToConsole);
                                    LowBound = HighBound = Setup.Target;

                                    
                                }
                                else 
                                {
                                    //Trend was not broken. Just update limits
                                    UpdateLimits(CurrentCandle.Close);
                                    Log(LogEntryImportance.Info, "Shifted limits down", logToConsole);
                                    //we still get ready for a long trend
                                    if (OngoingContracts == 0)
                                    {
                                        Log(LogEntryImportance.Info, "Opening long position", logToConsole);
                                        OpenPosition();
                                    }
                                }

                            }
                            else // price stayed strictly between bounds
                            {
                                //Check if trend was broken (possible if wma is between the two bounds and price closed between wma and low bound)
                                Setups tempSetup = DetermineSetup();
                                if (tempSetup != null)
                                {
                                    Log(LogEntryImportance.Info, "Trend is broken", logToConsole);
                                    
                                    //Close position or cancel current opening order                                   
                                     Log(LogEntryImportance.Info, "Closing position", logToConsole);
                                     ClosePosition();
                                                                        
                                    Setup = tempSetup;
                                    Log(LogEntryImportance.Info, "New Setup detected", logToConsole);
                                    LowBound = HighBound = Setup.Target;
                                }
                                else
                                {
                                    Log(LogEntryImportance.Info, "Price is contained. Waiting.", logToConsole);                                   
                                }
                            }

                            break;
                            #endregion

                        //Short Position
                        case (int)SetupType.down:
                            #region DOWN
                            if (CurrentCandle.Close <= LowBound)
                            {
                                //shift limits down
                                UpdateLimits(CurrentCandle.Close);
                                Log(LogEntryImportance.Info, "Shifted limits down", logToConsole);

                                //if we re not trading yet for some reason, it's still time to open a position
                                if (OngoingContracts==0)
                                {
                                    OpenPosition();
                                }
                                else if (OngoingContracts < 0)
                                {
                                    //create new stop loss order
                                    ShiftStopLoss();
                                }
                                else
                                {
                                    Log(LogEntryImportance.Error, string.Format("positive position of {0} contracts during down-trend. THIS IS BAD", OngoingContracts), logToConsole);
                                }
                            }
                            else if (CurrentCandle.Close >= HighBound)
                            {
                                //stop loss should have been executed
                                if (OngoingContracts != 0)
                                {
                                    Log(LogEntryImportance.Error, string.Format("position is {0} instead of 0. THIS IS BAD", OngoingContracts), logToConsole);
                                }

                                
                                //Check if trend is broken
                                //If there is a setup in the other direction, we abort the previous one
                                Setups tempSetup = DetermineSetup();
                                if (tempSetup != null)
                                {
                                    Log(LogEntryImportance.Info, "Trend is broken", logToConsole);

                                    //close position in case the opening order is still open
                                    Log(LogEntryImportance.Info, "Closing position", logToConsole);
                                    ClosePosition();

                                    Setup = tempSetup;
                                    Log(LogEntryImportance.Info, "New Setup detected", logToConsole);
                                    LowBound = HighBound = Setup.Target;
                                }
                                else
                                {
                                    //Trend was not broken. Just update limits
                                    UpdateLimits(CurrentCandle.Close);
                                    Log(LogEntryImportance.Info, "Shifted limits up", logToConsole);
                                    //we still get ready for a long trend
                                    if (OngoingContracts == 0)
                                    {
                                        Log(LogEntryImportance.Info, "Opening short position", logToConsole);
                                        OpenPosition();
                                    }
                                }
                            }
                            else // price stayed strictly between bounds
                            {
                                //Check if trend was broken (possible if wma is between the two bounds and price closed between wma and high bound)
                                Setups tempSetup = DetermineSetup();
                                if (tempSetup != null)
                                {
                                    Log(LogEntryImportance.Info, "Trend is broken", logToConsole);
                                    
                                    //Close position or cancel opening order
                                    
                                    Log(LogEntryImportance.Info, "Closing position", logToConsole);
                                    ClosePosition();
                                    

                                    Setup = tempSetup;
                                    Log(LogEntryImportance.Info, "New Setup detected", logToConsole);
                                    LowBound = HighBound = Setup.Target;
                                }
                                else
                                {
                                    Log(LogEntryImportance.Info, "Price is contained. Waiting.", logToConsole);
                                }
                            }

                            break;
                            #endregion

                        default:
                            break;
                    }

                }
                //Setup is not confirmed
                else
                {
                    Log(LogEntryImportance.Info, "Looking for confirmation", logToConsole);
                    //Check for confirmation
                    ValidateSetup();
                    //if setup is confirmed, initialize the bounds and open a long position
                    if (Setup.Confirmation != null)
                    {
                        Log(LogEntryImportance.Info, "The setup was confirmed", logToConsole);
                        InitializeLimits();
                        OpenPosition();
                    }
                    //Setup is not confirmed
                    else
                    {
                        Log(LogEntryImportance.Info, "The setup was not confirmed", logToConsole);
                        Setups tempSetup = DetermineSetup();
                        //If there is a new setup
                        if (tempSetup != null)
                        {
                            Setup = tempSetup;
                            Log(LogEntryImportance.Info, "New Setup detected", logToConsole);
                            //Set the target
                            LowBound = HighBound = Setup.Target;
                        }
                        //otherwise we keep it
                    }


                }

            }
            //There is no setup
            else
            {
                Log(LogEntryImportance.Info, "Looking for setup", logToConsole);
                Setup = DetermineSetup();
                //If there is a new setup
                if (Setup != null)
                {
                    Log(LogEntryImportance.Info, "New Setup detected", logToConsole);
                    //Set the target
                    LowBound = HighBound = Setup.Target;
                }
            }

        }

        Setups DetermineSetup()
        {
            Setups result = null;

            if (OldCandle == null)  OldCandle = CurrentCandle;

            //if the price crossed the wma upward, it's an up setup
            if (OldCandle.ClosedUnder && CurrentCandle.ClosedAbove)
            {
                result = new Setups(CurrentCandle.CloseTime, (int)SetupType.up, CurrentCandle.High, CurrentCandle.Id);
                Immortalize(result);
            }
            //if it crossed downward, it's a down setup
            if (OldCandle.ClosedAbove && CurrentCandle.ClosedUnder)
            {
                result = new Setups(CurrentCandle.CloseTime, (int)SetupType.down, CurrentCandle.Low,CurrentCandle.Id);
                Immortalize(result);
            }
            //otherwise the setup is null
            return result;

        }

        void  ValidateSetup()
        {
            switch (Setup.Type)
            {
                case (int)SetupType.up:
                    //If the market trades higher than the high of the setup bar
                    if (CurrentCandle.High > Setup.Target)
                    {
                        Confirmations confirmation = new Confirmations(CurrentCandle, GetUpSignal(CurrentCandle.High), Setup.Id);

                        Immortalize(confirmation);

                        Setup.Confirmation = confirmation;
                        return;
                    }
                    break;
                case (int)SetupType.down:
                    if (CurrentCandle.Low < Setup.Target)
                    {
                        Confirmations confirmation = new Confirmations(CurrentCandle, GetDownSignal(CurrentCandle.Low), Setup.Id);

                        Immortalize(confirmation);

                        Setup.Confirmation = confirmation;
                        return;
                    }
                    break;
                default:
                    Setup = null;
                    break;
            }
        }

        void InitializeLimits()
        {
            if (Setup.Confirmation == null) return;

            switch (Setup.Type)
            {
                case (int)SetupType.up:
                    HighBound = Setup.Confirmation.Signal;
                    LowBound = Math.Max(HighBound - NNInterval,Wma._Price);
                    break;
                case (int)SetupType.down:
                    LowBound = Setup.Confirmation.Signal;
                    HighBound = Math.Min(LowBound + NNInterval,Wma._Price);
                    break;
                default:
                    break;
            }
        }

        void UpdateLimits(decimal price)
        {
            HighBound = GetUpSignal(price);
            LowBound = GetDownSignal(price);
        }

        decimal GetUpSignal(decimal price)
        {
            decimal incr = 0;

            if ((Setup.Type == (int)SetupType.down) && (price % NNInterval == 0))
            {
                incr = NNInterval;
            }
            return Math.Ceiling(price / NNInterval) * NNInterval + incr;
        }

        decimal GetDownSignal(decimal price)
        {
            decimal incr = 0;

            if ((Setup.Type == (int)SetupType.up) && (price % NNInterval == 0))
            {
                incr = -NNInterval;
            }
            return Math.Floor(price / NNInterval) * NNInterval + incr;
        }

        void Reset()
        {
            OldCandle = null;
            CurrentCandle = null;
            Wma = null;
            Setup = null;
            HighBound = 0;
            LowBound = 0;

            Log(LogEntryImportance.Info, "Reset",true);
        }

        //this is slow because it uses reflection. Think of something else
        void Immortalize(Object o)
        {
            if (o.GetType() == typeof(CandleSticks))
            {
                CandleSticks cs = (CandleSticks)o;
                _candleStickRepository.Save(cs);
                Mediator.NotifyColleagues<CandleSticks>(MediatorMessages.NewCandleStick, cs);
                return;
            }
            if (o.GetType() == typeof(Setups))
            {
                Setups setup = (Setups)o;
                _setupRepository.Save(setup);
                Mediator.NotifyColleagues<Setups>(MediatorMessages.NewSetup, setup);
                return;
            }
            if (o.GetType() == typeof(Confirmations))
            {
                Confirmations c = (Confirmations)o;
                _confirmationRepository.Save(c);
                Mediator.NotifyColleagues<Confirmations>(MediatorMessages.NewConfirmation, c);
                return;
            }
        }

        void OpenPosition()
        {
            if (Setup.Confirmation == null) return;

            if (EnableOrders)
            {
                OpenPositionData data = null;
                decimal enteringPrice;
                decimal numberOfContracts;
                switch (Setup.Type)
                {
                    case (int)SetupType.up:
                        enteringPrice = HighBound;
                        numberOfContracts = PositionOpeningCost / enteringPrice;
                        data = new OpenPositionData(OrderType.buy, enteringPrice, LowBound, numberOfContracts, CurrentCandle.Id, Setup.Confirmation.Id, ValidateOnly);
                        break;
                    case (int)SetupType.down:
                        enteringPrice = LowBound;
                        numberOfContracts = PositionOpeningCost / enteringPrice;
                        data = new OpenPositionData(OrderType.sell, enteringPrice, HighBound, numberOfContracts, CurrentCandle.Id, Setup.Confirmation.Id, ValidateOnly);
                        break;
                    default:
                        break;
                }

                Mediator.NotifyColleagues<OpenPositionData>(MediatorMessages.OpenPosition, data);
            }

        }

        void ShiftStopLoss()
        {
            if (EnableOrders)
            {
                decimal newLimitPrice = 0;
                switch (Setup.Type)
                {
                    case (int)SetupType.up:
                        newLimitPrice = LowBound;
                        break;
                    case (int)SetupType.down:
                        newLimitPrice = HighBound;
                        break;
                }

                ShiftPositionLimitsData shiftPositionLimitsData = new ShiftPositionLimitsData(newLimitPrice, ValidateOnly);
                Mediator.NotifyColleagues<ShiftPositionLimitsData>(MediatorMessages.ShiftPositionLimits, shiftPositionLimitsData);
            }
        }

        void ClosePosition()
        {
            if (EnableOrders)
            {
                Mediator.NotifyColleagues<string>(MediatorMessages.ClosePosition, "close position");
            }
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

        [MediatorMessageSink(MediatorMessages.EnableOrdersChanged, ParameterType = typeof(bool))]
        public void SetEnableOrders(bool enableOrders)
        {
            EnableOrders = enableOrders;
        }

        [MediatorMessageSink(MediatorMessages.PositionOpeningCostChanged, ParameterType = typeof(decimal))]
        public void SetPositionOpeningCost(decimal openingCost)
        {
            PositionOpeningCost = openingCost;
        }

        [MediatorMessageSink(MediatorMessages.NewPriceData, ParameterType = typeof(PriceData))]
        public void NewPriceDataReceived(PriceData priceData)
        {

            TreatNewPriceData(priceData);
        }
        
        [MediatorMessageSink(MediatorMessages.Reset, ParameterType = typeof(string))]
        public void ResetMessageReceived(string message)
        {
            Reset();
        }

        [MediatorMessageSink(MediatorMessages.UpdateOngoingContracts, ParameterType = typeof(decimal))]
        public void UpdateOngoingContracts(decimal ongoingContractsIncrement)
        {

            lock (OngoingContractsLock)
            {
                OngoingContracts += ongoingContractsIncrement;

                //Sometimes an order is considered to be executed even when a very small fraction of it is not filled. We allow for a small error Epsilon (~ 0.001 EUR at the time of writing)
                if (Math.Abs(OngoingContracts) < PositionEpsilon)
                {
                    OngoingContracts = 0;
                }
            }
        }

        [MediatorMessageSink(MediatorMessages.StopStrategyService, ParameterType = typeof(string))]
        public void StopStrategyService(string message)
        {
            ClosePosition();
        }

        #endregion

        #region Dispose

        #region Dispose

        ~StrategyService()
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
                
			}
			// free native resources
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

        #endregion

        #endregion
    }
}
