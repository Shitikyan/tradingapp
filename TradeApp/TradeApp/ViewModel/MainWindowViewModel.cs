using TradeApp.Infrastructure;

namespace TradeApp.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {

        public MainWindowViewModel()
        {

        }

        ControlViewViewModel controlVM;
        public ControlViewViewModel ControlVM
        {
            get
            {
                if (controlVM == null)
                    controlVM = new ControlViewViewModel();
                return controlVM;

            }
            set
            {
                controlVM = value;
                this.RaisePropertyChanged(() => this.ControlVM);
            }
        }

        GraphViewViewModel graphVM;
        public GraphViewViewModel GraphVM
        {
            get
            {
                if (graphVM == null)
                    graphVM = new GraphViewViewModel();
                return graphVM;

            }
            set
            {
                graphVM = value;
                this.RaisePropertyChanged(() => this.GraphVM);
            }
        }

        LogViewViewModel logVM;
        public LogViewViewModel LogVM
        {
            get
            {
                if (logVM == null)
                    logVM = new LogViewViewModel();
                return logVM;

            }
            set
            {
                logVM = value;
                this.RaisePropertyChanged(() => this.LogVM);
            }
        }

        PanelViewViewModel panelVM;
        public PanelViewViewModel PanelVM
        {
            get
            {
                if (panelVM == null)
                    panelVM = new PanelViewViewModel();
                return panelVM;

            }
            set
            {
                panelVM = value;
                this.RaisePropertyChanged(() => this.PanelVM);
            }
        }

        PositionViewViewModel positionVM;
        public PositionViewViewModel PositionVM
        {
            get
            {
                if (positionVM == null)
                    positionVM = new PositionViewViewModel();
                return positionVM;

            }
            set
            {
                positionVM = value;
                this.RaisePropertyChanged(() => this.PositionVM);
            }
        }
    }
}
