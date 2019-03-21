using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TradeApp.DataAccess;
using TradeApp.Infrastructure;
using TradeApp.ViewModel;

namespace TradeApp.View
{
    /// <summary>
    /// Interaction logic for PanelView.xaml
    /// </summary>
    public partial class PanelView
    {
        public PanelView()
        {
            InitializeComponent();
        }


        private void Setups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PanelViewViewModel vm = (PanelViewViewModel)DataContext;
            Setups setup = (Setups)e.AddedItems[0];
            vm.SetupSelectionChanged(setup);
        }
    }
}
