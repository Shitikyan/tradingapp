using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeApp.DataAccess;
using TradeApp.Infrastructure;
using TradeApp.Messaging;

namespace TradeApp.ViewModel
{
    public class PositionViewViewModel:ViewModelBase
    {
        public PositionViewViewModel()
        {
            Mediator.Register(this);
        }

        decimal ongoingContracts;
        public decimal OngoingContracts
        {
            get 
            {
                return ongoingContracts;
            }
            set 
            {
                ongoingContracts = value;
                base.RaisePropertyChanged(() => this.OngoingContracts);
            }
        }

        Orders openOrder;
        public Orders OpenOrder
        {
            get
            {
                return openOrder;
            }
            set
            {
                openOrder = value;
                //notify view
                base.RaisePropertyChanged(() => this.OpenOrder);
            }
        }

        Orders emergencyExitOrder;
        public Orders EmergencyExitOrder
        {
            get
            {
                return emergencyExitOrder;
            }
            set
            {
                emergencyExitOrder = value;
                //notify view
                base.RaisePropertyChanged(() => this.EmergencyExitOrder);
            }
        }

        [MediatorMessageSink(MediatorMessages.UpdateOngoingContracts, ParameterType = typeof(decimal))]
        public void UpdateOngoingContracts(decimal ongoingContractsIncrement)
        {

              OngoingContracts += ongoingContractsIncrement;
            
        }

        [MediatorMessageSink(MediatorMessages.UpdateOrder, ParameterType = typeof(Orders))]
        public void UpdateOpenOrder(Orders order)
        {

            OpenOrder = order;

        }

        [MediatorMessageSink(MediatorMessages.UpdateEmergencyOrder, ParameterType = typeof(Orders))]
        public void UpdateEmergencyOrder(Orders order)
        {

            EmergencyExitOrder = order;

        }
    }
}
