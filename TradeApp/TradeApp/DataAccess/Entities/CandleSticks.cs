using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.DataAccess
{
    public partial class CandleSticks : INotifyPropertyChanged
    {
        public CandleSticks(DateTimeOffset openTime, DateTimeOffset closeTime, decimal open, decimal high, decimal low, decimal close)
        {
            OpenTime = openTime;
            CloseTime = closeTime;
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

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

        public bool ClosedUnder
        {
            get
            {
                return (Close < WMAValue);
            }
        }

        public bool ClosedAbove
        {
            get
            {
                return (Close > WMAValue);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
