using System;
using System.ComponentModel;

namespace TradeApp.DataAccess
{
    public partial class Orders : INotifyPropertyChanged
    {
        //for UI purposes
        bool isSelected;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        string displayStatus;
        public string DisplayStatus
        {
            get
            {
                return displayStatus;
            }
            set
            {
                displayStatus = value;
                OnPropertyChanged("DisplayStatus");
            }
        }

        string displayAvgPrice;
        public string DisplayAvgPrice
        {
            get
            {
                return displayAvgPrice;
            }
            set
            {
                displayAvgPrice = value;
                OnPropertyChanged("DisplayAvgPrice");
            }
        }

        public bool IsEmergencyOrder { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum OrderType
    {
        buy = 1,
        sell = 2
    }

    public enum KrakenOrderType
    {
        market = 1,
        limit = 2,// (price = limit price)
        stop_loss = 3, // (price = stop loss price)
        take_profit = 4, // (price = take profit price)
        stop_loss_profit = 5, // (price = stop loss price, price2 = take profit price)
        stop_loss_profit_limit = 6, // (price = stop loss price, price2 = take profit price)
        stop_loss_limit = 7,// (price = stop loss trigger price, price2 = triggered limit price)
        take_profit_limit = 8, // (price = take profit trigger price, price2 = triggered limit price)
        trailing_stop = 9, //(price = trailing stop offset)
        trailing_stop_limit = 10,// (price = trailing stop offset, price2 = triggered limit offset)
        stop_loss_and_limit = 11,// (price = stop loss price, price2 = limit price)
    }

    public enum KrakenOrderStatus
    {
        pending = 1, // order pending book entry
        open = 2, // open order
        closed = 3, //cosed order
        canceled = 4, // order canceled
        expired = 5 // order expired
    }

    public enum OFlag
    {
        viqc = 1, //volume in quote currency
        plbc = 2, //prefer profit/los in base currency
        nompp = 3 //no market price protection
    }
}
