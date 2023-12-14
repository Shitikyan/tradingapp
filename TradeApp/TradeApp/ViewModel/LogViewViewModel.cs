using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using TradeApp.Infrastructure;
using TradeApp.Messaging;
using TradeApp.Model;

namespace TradeApp.ViewModel
{
    public class LogViewViewModel : ViewModelBase
    {
        public LogViewViewModel()
        {
            LogEntries = new ObservableCollection<LogEntry>();
            Mediator.Register(this);
        }

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        bool scroll;
        public bool Scroll
        {
            get
            {
                return scroll;
            }
            set
            {
                scroll = value;
                base.RaisePropertyChanged(() => this.Scroll);
            }
        }

        #region Message Handlers

        [MediatorMessageSink(MediatorMessages.LogMessage, ParameterType = typeof(LogEntry))]
        public void AddLogEntry(LogEntry entry)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                            () =>
                            {
                                LogEntries.Add(entry);
                                Scroll = true;
                                Scroll = false;
                            }));

        }

        #endregion

    }
}
