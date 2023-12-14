using System;
using System.ComponentModel;

namespace TradeApp.DataAccess
{
    public partial class Confirmations : INotifyPropertyChanged
    {
        public Confirmations(DateTimeOffset date, decimal signal)
        {
            Date = date;
            Signal = signal;
        }

        public Confirmations(CandleSticks candleStick, decimal signal, int setupId)
        {
            Date = candleStick.CloseTime;
            CandleStickId = candleStick.Id;
            Signal = signal;
            SetupId = setupId;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
