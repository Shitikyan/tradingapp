using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeApp.DataAccess
{
    public class Positions:INotifyPropertyChanged
    {
        public OrderType Direction { get; set; }
        public Orders OpeningOrder { get; set; }
        public Orders ClosingOrder { get; set; }
        public Orders EmergencyExitOrder { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
