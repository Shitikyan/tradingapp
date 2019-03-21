using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TradeApp.ApiClient;
using TradeApp.Infrastructure;
using TradeApp.Infrastructure.Model;
using TradeApp.Messaging;
using TradeApp.Service;

namespace TradeApp.ViewModel
{
    public class ControlViewViewModel : ViewModelBase
    {
        QuoteService _quoteService;
        StrategyService _strategyService;
        BrokerageService _brokerageService;


        public ControlViewViewModel()
        {
            Mediator.Register(this);
            

            MEFLoader loader = new MEFLoader();

            ExchangeClients = loader.ExchangeClients;
            SelectedExchangeClient = ExchangeClients.SingleOrDefault(c => ((ExchangeClientBase)c).Name == "Kraken Mock"); ;

            InitializeServices();

            IntervalList = new List<TimeIntervalTypeItem>()
            {
                new TimeIntervalTypeItem(){ValueTimeIntervalTypeEnum=TimeIntervals.Minute,ValueTimeIntervalTypeString="Minute"},
                new TimeIntervalTypeItem(){ValueTimeIntervalTypeEnum=TimeIntervals.Hour,ValueTimeIntervalTypeString="Hour"},
                new TimeIntervalTypeItem(){ValueTimeIntervalTypeEnum=TimeIntervals.Day,ValueTimeIntervalTypeString="Day"},
            };
            
            CurrentTimeIntervalType = IntervalList.Where(i => i.ValueTimeIntervalTypeEnum == TimeIntervals.Minute).FirstOrDefault();
            TimeIntervalValue = 5;
            WmaPeriod = 180;
            NNInterval = 1;
            PositionOpeningCost = 10;

            BroadcastConfiguration();
            
        }

        #region Properties

        public IEnumerable<IExchangeClient> ExchangeClients {get;set;}
       
        IExchangeClient _selectedExchangeClient;
        public IExchangeClient SelectedExchangeClient
        {
            get
            {
                if (_selectedExchangeClient == null)
                    _selectedExchangeClient = ExchangeClients.FirstOrDefault();
                return _selectedExchangeClient;
            }
            set
            {
                _selectedExchangeClient = value;

                base.RaisePropertyChanged(() => this.SelectedExchangeClient);

                InitializeServices();
            }
        }

        public List<TimeIntervalTypeItem> IntervalList { get; set; }

        public TimeIntervalTypeItem OldTimeIntervalType { get; set; }
        TimeIntervalTypeItem currentTimeIntervalType;
        public TimeIntervalTypeItem CurrentTimeIntervalType
        {
            get
            {
                return currentTimeIntervalType;
            }
            set
            {
                OldTimeIntervalType = currentTimeIntervalType;                
                currentTimeIntervalType = value;

                //notify colleagues
                Mediator.NotifyColleagues<TimeIntervals>(MediatorMessages.TimeIntervalTypeChanged, currentTimeIntervalType.ValueTimeIntervalTypeEnum);
                //notify view
                base.RaisePropertyChanged(() => this.CurrentTimeIntervalType);
            }
        }
        
        public int OldTimeIntervalValue { get; set; }
        int timeIntervalValue;
        public int TimeIntervalValue
        {
            get
            {
                return timeIntervalValue;
            }
            set
            {
                OldTimeIntervalValue = timeIntervalValue;
                timeIntervalValue = value;

                Mediator.NotifyColleagues<int>(MediatorMessages.TimeIntervalValueChanged, timeIntervalValue);

                base.RaisePropertyChanged(() => this.TimeIntervalValue);
            }
        }

        public int OldPeriod { get; set; }
        int wmaPeriod;
        public int WmaPeriod
        {
            get
            {
                return wmaPeriod;
            }
            set
            {
                OldPeriod = wmaPeriod;
                wmaPeriod = value;

                Mediator.NotifyColleagues<int>(MediatorMessages.WmaPeriodChanged, wmaPeriod);

                base.RaisePropertyChanged(() => this.WmaPeriod);
            }
        }

        public decimal OldNNInterval { get; set; }
        decimal nnInterval;
        public decimal NNInterval
        {
            get
            {
                return nnInterval;
            }
            set
            {
                OldNNInterval = nnInterval;
                nnInterval = value;

                Mediator.NotifyColleagues<decimal>(MediatorMessages.NNIntervalChanged, nnInterval);

                base.RaisePropertyChanged(() => this.NNInterval);
            }
        }

        public decimal OldPositionOpeningCost { get; set; }
        decimal posSize;
        public decimal PositionOpeningCost
        {
            get
            {
                return posSize;
            }
            set
            {
                OldPositionOpeningCost = posSize;
                posSize = value;

                Mediator.NotifyColleagues<decimal>(MediatorMessages.PositionOpeningCostChanged, posSize);

                base.RaisePropertyChanged(() => this.PositionOpeningCost);
            }

        }
        
        public bool OldEnableOrders { get; set; }
        bool enableOrders;
        public bool EnableOrders
        {
            get
            {
                return enableOrders;
            }
            set
            {
                OldEnableOrders = enableOrders;
                enableOrders = value;

                Mediator.NotifyColleagues<bool>(MediatorMessages.EnableOrdersChanged, enableOrders);

                base.RaisePropertyChanged(() => this.EnableOrders);

            }

        }

        bool canChangeEnableOrders;
        public bool CanChangeEnableOrders
        {
            get 
            {
                return canChangeEnableOrders;            
            }
            set
            {
                canChangeEnableOrders = value;

                base.RaisePropertyChanged(() => this.CanChangeEnableOrders);
            }       
        }

        bool canChangeExchange;
        public bool CanChangeExchange
        {
            get
            {
                return canChangeExchange;
            }
            set
            {
                canChangeExchange = value;

                base.RaisePropertyChanged(() => this.CanChangeExchange);
            }
        }

        bool busy;
        public bool Busy 
        {
            get
            {
                return busy;
            }
            set
            {
                busy = value;
                
                base.RaisePropertyChanged(() => this.Busy);
            }
        }

        bool CaughtUpWithConnector { get; set; }


        #endregion
        
        #region Commands

        RelayCommand _startCommand;
        public ICommand StartCommand
        {
            get
            {
                if (_startCommand == null)
                {
                    _startCommand = new RelayCommand(param => this.Start(), param => this.CanStart());
                }
                return _startCommand;
            }
        }

        RelayCommand _stopCommand;
        public ICommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                {
                    _stopCommand = new RelayCommand(param => this.StopQuoteBot(), param => this.CanStopQuoteBot());
                }
                return _stopCommand;
            }
        }
        
        #endregion
        
        #region Methods

        public void InitializeServices()
        {
            if (_quoteService == null) 
                _quoteService = new QuoteService(SelectedExchangeClient);
            else 
                _quoteService.Client = SelectedExchangeClient;
            
            if (_strategyService == null) 
                _strategyService = new StrategyService();
            
            if (_brokerageService == null) 
                _brokerageService = new BrokerageService(SelectedExchangeClient);
            else 
                _brokerageService.Client = SelectedExchangeClient;

        }

        /// <summary>
        /// Send a message to start the quotebot
        /// </summary>
        public void Start()
        {

            Busy = true;

            BroadcastConfiguration();

            Mediator.NotifyColleagues<string>(MediatorMessages.Reset, "yo");

            EnableOrders = false;
            CanChangeEnableOrders = false;
            
            Mediator.NotifyColleagues<string>(MediatorMessages.CatchUpWithConnector, "yo");
            

            
        }

        public void StartQuoteBot()
        {
            Mediator.NotifyColleagues<string>(MediatorMessages.StartQuoteBot, "yo");
            
            EnableOrders = OldEnableOrders;
            CanChangeEnableOrders = true;
            CanChangeExchange = false;
        }

        public void StopQuoteBot()
        {

            Mediator.NotifyColleagues<string>(MediatorMessages.StopQuoteBot, "yo");

            Busy = false;

           //when the brokerage service informs of position closed, we will set can change exchange
        }

        public void BroadcastConfiguration()
        {
            Mediator.NotifyColleagues<TimeIntervals>(MediatorMessages.TimeIntervalTypeChanged, currentTimeIntervalType.ValueTimeIntervalTypeEnum);
            Mediator.NotifyColleagues<int>(MediatorMessages.TimeIntervalValueChanged, timeIntervalValue);
            Mediator.NotifyColleagues<int>(MediatorMessages.WmaPeriodChanged, wmaPeriod);
            Mediator.NotifyColleagues<decimal>(MediatorMessages.NNIntervalChanged, nnInterval);
            Mediator.NotifyColleagues<decimal>(MediatorMessages.PositionOpeningCostChanged, posSize);
        } 
        
        #endregion

        #region Predicates

        public bool CanStart()
        {
            return !Busy;
        }

        public bool CanStopQuoteBot()
        {
            return Busy;
        }

              
        #endregion

        #region Message Handlers

        [MediatorMessageSink(MediatorMessages.DoneCatchingUpWithConnector, ParameterType = typeof(string))]
        public void CaughtUpWithConnectorMessageReceived(string message)
        {
            StartQuoteBot();
        }

        [MediatorMessageSink(MediatorMessages.BrokerPositionNull, ParameterType = typeof(string))]
        public void BrokerPositionNullMessageReceived(string message)
        {
            CanChangeExchange = !Busy;
        }

        #endregion

    }
}
