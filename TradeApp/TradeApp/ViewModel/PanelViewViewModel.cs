using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using TradeApp.DataAccess;
using TradeApp.Infrastructure;
using TradeApp.Messaging;

namespace TradeApp.ViewModel
{
    public class PanelViewViewModel:ViewModelBase
    {
        public PanelViewViewModel()
        {
            CandleSticks = new ObservableCollection<CandleSticks>();
            Setups = new ObservableCollection<Setups>();
            Confirmations = new ObservableCollection<Confirmations>();
            Orders = new ObservableCollection<Orders>();

            Mediator.Register(this);
        }

        public ObservableCollection<CandleSticks> CandleSticks { get; set; }
        public ObservableCollection<Setups> Setups { get; set; }
        public ObservableCollection<Confirmations> Confirmations { get; set; }
        public ObservableCollection<Orders> Orders { get; set; }
        
        #region Methods

        void UnselectAll()
        {

            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                () =>
                {


                    foreach (var candleStick in CandleSticks.Where(cs => cs.IsSelected))
                        candleStick.IsSelected = false;
                    foreach (var setup in Setups.Where(s => s.IsSelected))
                        setup.IsSelected = false;
                    foreach (var confirmation in Confirmations.Where(c => c.IsSelected))
                        confirmation.IsSelected = false;
                    foreach (var order in Orders.Where(o => o.IsSelected))
                        order.IsSelected = false;
                }));

        }

        public void SetupSelectionChanged(Setups selectedSetup)
        {
            
            
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                ()=>
                {
                    //unselect all                    
                    foreach (var candleStick in CandleSticks.Where(cs => cs.IsSelected))
                        candleStick.IsSelected = false;                    
                    foreach (var setup in Setups.Where(s => s.IsSelected))
                        setup.IsSelected = false;
                    foreach (var confirmation in Confirmations.Where(c => c.IsSelected))
                        confirmation.IsSelected = false;
                    foreach (var order in Orders.Where(o => o.IsSelected))
                        order.IsSelected = false;
                    
                    //select setup
                    selectedSetup.IsSelected = true;

                    //select the Setup's candle stick
                    var setupCandleStick = CandleSticks.Where(cs => cs.Id == selectedSetup.CandleStickId).FirstOrDefault();
                    setupCandleStick.IsSelected = true;

                    //look for a confirmation
                    var setupConfirmation = Confirmations.Where(c => c.SetupId == selectedSetup.Id).FirstOrDefault();

                    //if confirmation is not null, select it and look for related trade orders
                    if (setupConfirmation != null)
                    {
                        setupConfirmation.IsSelected = true;

                        var tradeOrders = Orders.Where(o => o.ConfirmationId == setupConfirmation.Id);

                        foreach (var order in tradeOrders)
                        {
                            order.IsSelected = true;
                        }
                    }
                    
                    
                }));
        
        }

        void Reset()
        {
            CandleSticks.Clear();            
            Setups.Clear();
            Confirmations.Clear();
            Orders.Clear();
        }

        #endregion

        #region Message Handlers

        [MediatorMessageSink(MediatorMessages.NewCandleStick, ParameterType = typeof(CandleSticks))]
        public void AddCandleStick(CandleSticks candleStick)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                            () =>
                            {
                                CandleSticks.Add(candleStick);
                            }));
        }

        [MediatorMessageSink(MediatorMessages.NewSetup, ParameterType = typeof(Setups))]
        public void AddSetup(Setups setup)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                            () =>
                            {
                                Setups.Add(setup);
                            }));
        }

        [MediatorMessageSink(MediatorMessages.NewConfirmation, ParameterType = typeof(Confirmations))]
        public void AddConfirmation(Confirmations confirmation)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                            () =>
                            {
                                Confirmations.Add(confirmation);
                            }));
        }

        [MediatorMessageSink(MediatorMessages.UpdateOrder, ParameterType = typeof(Orders))]       
        public void UpdateOrder(Orders order)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                            () =>
                            {
                                var listOrder = Orders.Where(o => o.Id == order.Id).FirstOrDefault();
                                if (listOrder == null)
                                {
                                    Orders.Add(order);
                                }
                                else 
                                {
                                    listOrder = order;
                                    listOrder.DisplayStatus = string.Format("{0} {1}", listOrder.Status, listOrder.Reason);
                                    listOrder.DisplayAvgPrice = (listOrder.AveragePrice.HasValue) ? listOrder.AveragePrice.ToString() : "";
                                }
                            }));
        }

        [MediatorMessageSink(MediatorMessages.UpdateEmergencyOrder, ParameterType = typeof(Orders))]
        public void UpdateEmergncyOrder(Orders order)
        {
            
            if (order == null)
                return;
            
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                            () =>
                            {
                                var listOrder = Orders.Where(o => o.Id == order.Id).FirstOrDefault();
                                if (listOrder == null)
                                {
                                    Orders.Add(order);
                                }
                                else
                                {
                                    listOrder = order;
                                    listOrder.DisplayStatus = string.Format("{0} {1}", listOrder.Status, listOrder.Reason);
                                    listOrder.DisplayAvgPrice = (listOrder.AveragePrice.HasValue) ? listOrder.AveragePrice.ToString() : "";
                                }
                            }));
        }

        [MediatorMessageSink(MediatorMessages.Reset, ParameterType = typeof(string))]
        public void Reset(string message)
        {
            Reset();
        }

        #endregion
    }
}
