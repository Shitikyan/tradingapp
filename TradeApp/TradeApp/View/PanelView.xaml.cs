using System.Windows.Controls;
using TradeApp.DataAccess;
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
