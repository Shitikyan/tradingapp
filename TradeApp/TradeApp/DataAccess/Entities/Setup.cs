using System;
using System.ComponentModel;

namespace TradeApp.DataAccess
{

    public enum SetupType
    {
        up = 1,
        down = 2,
    }

    public partial class Setups : INotifyPropertyChanged
    {
        public Setups(DateTimeOffset date, int type, decimal target)
        {
            Date = date;
            Type = type;
            Target = target;
        }

        public Setups(DateTimeOffset date, int type, decimal target, int candleStickId)
            : this(date, type, target)
        {
            CandleStickId = candleStickId;
        }

        public Confirmations Confirmation { get; set; }

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
