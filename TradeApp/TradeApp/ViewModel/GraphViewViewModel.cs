using Microsoft.Research.DynamicDataDisplay.Common;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using System;
using System.Windows.Threading;
using TradeApp.ConnectorService;
using TradeApp.DataAccess;
using TradeApp.Infrastructure;
using TradeApp.Messaging;
using TradeApp.Model;

namespace TradeApp.ViewModel
{
    public class GraphViewViewModel:ViewModelBase
    {

        public RingArray<CandleSticks> candleStickCollection;
        public RingArray<PricePoint> wmaCollection;
        public RingArray<PricePoint> highBoundCollection;
        public RingArray<PricePoint> lowBoundCollection;

        public GraphViewViewModel()
        {
            Mediator.Register(this);           
        }

        EnumerableDataSource<CandleSticks> priceDS;
        public EnumerableDataSource<CandleSticks> PriceDS
        {
            get
            {
                return priceDS;
            }
            set
            {
                value.SetXMapping(k => k.OpenTime.UtcTicks);
                value.SetYMapping(k => (double)k.Close);
               // value.AddMapping(CandleStickPointMarker.OpenProperty, k => (double)k.Open);
                //value.AddMapping(CandleStickPointMarker.HighProperty, k => (double)k.High);
                //value.AddMapping(CandleStickPointMarker.LowProperty, k => (double)k.Low);
                priceDS = value;
                base.RaisePropertyChanged(() => this.PriceDS);
            }
        }

        EnumerableDataSource<PricePoint> wmaDS;
        public EnumerableDataSource<PricePoint> WmaDS
        {
            get
            {
                return wmaDS;
            }
            set
            {
                value.SetXMapping(k => k._Time.UtcTicks);
                value.SetYMapping(k => (double)k._Price); ;

                wmaDS = value;

                base.RaisePropertyChanged(() => this.WmaDS);
            }
        }

        EnumerableDataSource<PricePoint> highBoundDS;
        public EnumerableDataSource<PricePoint> HighBoundDS
        {
            get
            {
                return highBoundDS;
            }
            set
            {
                value.SetXMapping(k => k._Time.UtcTicks);
                value.SetYMapping(k => (double)k._Price); ;

                highBoundDS = value;

                base.RaisePropertyChanged(() => this.HighBoundDS);
            }
        }

        EnumerableDataSource<PricePoint> lowBoundDS;
        public EnumerableDataSource<PricePoint> LowBoundDS
        {
            get
            {
                return lowBoundDS;
            }
            set
            {
                value.SetXMapping(k => k._Time.UtcTicks);
                value.SetYMapping(k => (double)k._Price); ;

                lowBoundDS = value;

                base.RaisePropertyChanged(() => this.LowBoundDS);
            }
        }

        [MediatorMessageSink(MediatorMessages.InitializeGraphViewModel, ParameterType = typeof(int))]        
        public void Initialize(int period)
        {
            candleStickCollection = new RingArray<CandleSticks>(period);
            wmaCollection = new RingArray<PricePoint>(period);
            highBoundCollection = new RingArray<PricePoint>(2);
            lowBoundCollection = new RingArray<PricePoint>(2);

            PriceDS = new EnumerableDataSource<CandleSticks>(candleStickCollection);
            WmaDS = new EnumerableDataSource<PricePoint>(wmaCollection);
            HighBoundDS = new EnumerableDataSource<PricePoint>(highBoundCollection);
            LowBoundDS = new EnumerableDataSource<PricePoint>(lowBoundCollection);
        }

        [MediatorMessageSink(MediatorMessages.WmaPeriodChanged, ParameterType = typeof(int))]
        public void Reset(int period)
        {
            Initialize(period);
        }

        [MediatorMessageSink(MediatorMessages.NewPriceData, ParameterType = typeof(PriceData))]
        public void UpdateGraphData(PriceData chartData)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                            () =>
                            {
                                candleStickCollection.Add(chartData.CandleStick);
                                wmaCollection.Add(chartData.WmaPoint);
                            }));

        }

        [MediatorMessageSink(MediatorMessages.UpdateHighBound, ParameterType = typeof(decimal))]
        public void UpdateHighBound(decimal hbound)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                                () =>
                                {
                                    try
                                    {
                                        PricePoint firstPricePoint = new PricePoint(candleStickCollection[0].CloseTime, hbound, 0);
                                        PricePoint lastPricePoint = new PricePoint(candleStickCollection[candleStickCollection.Count - 1].CloseTime, hbound, 0);

                                        highBoundCollection = new RingArray<PricePoint>(2);
                                        highBoundCollection.Add(firstPricePoint);
                                        highBoundCollection.Add(lastPricePoint);

                                        HighBoundDS = new EnumerableDataSource<PricePoint>(highBoundCollection);
                                    }catch
                                    {}
                                }));
        }

        [MediatorMessageSink(MediatorMessages.UpdateLowBound, ParameterType = typeof(decimal))]
        public void UpdateLowBound(decimal lbound)
        {
            App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(

                               () =>
                               {
                                   try
                                   {
                                       PricePoint firstPricePoint = new PricePoint(candleStickCollection[0].CloseTime, lbound, 0);
                                       PricePoint lastPricePoint = new PricePoint(candleStickCollection[candleStickCollection.Count - 1].CloseTime, lbound, 0);

                                       lowBoundCollection = new RingArray<PricePoint>(2);
                                       lowBoundCollection.Add(firstPricePoint);
                                       lowBoundCollection.Add(lastPricePoint);

                                       LowBoundDS = new EnumerableDataSource<PricePoint>(lowBoundCollection);
                                   }
                                   catch { }

                               }));
        }
    }
}
