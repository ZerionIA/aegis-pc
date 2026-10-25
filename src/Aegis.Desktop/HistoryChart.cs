using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
namespace Aegis.Desktop;
public sealed class HistoryChart : FrameworkElement
{
    public static readonly DependencyProperty PointsProperty=DependencyProperty.Register(nameof(Points),typeof(ObservableCollection<ChartPoint>),typeof(HistoryChart),new PropertyMetadata(null,OnChanged));
    public ObservableCollection<ChartPoint>? Points{get=>(ObservableCollection<ChartPoint>?)GetValue(PointsProperty);set=>SetValue(PointsProperty,value);}
    private static void OnChanged(DependencyObject d,DependencyPropertyChangedEventArgs e){var chart=(HistoryChart)d;if(e.OldValue is ObservableCollection<ChartPoint> old)old.CollectionChanged-=chart.CollectionChanged;if(e.NewValue is ObservableCollection<ChartPoint> next)next.CollectionChanged+=chart.CollectionChanged;chart.InvalidateVisual();}
    private void CollectionChanged(object? s,NotifyCollectionChangedEventArgs e)=>InvalidateVisual();
    protected override void OnRender(DrawingContext dc)
    {
        double w=Math.Max(0,ActualWidth-16),h=Math.Max(0,ActualHeight-32);
        for(int i=0;i<=4;i++)dc.DrawLine(new Pen((Brush)System.Windows.Application.Current.Resources["Line"],1),new(8,16+h*i/4),new(w+8,16+h*i/4));
        if(Points is null||Points.Count<2)return;
        double start=Points[0].Seconds,end=Points[^1].Seconds,range=Math.Max(120,end-start);
        Draw(p=>p.Cpu,Color.FromRgb(82,218,238));Draw(p=>p.Memory,Color.FromRgb(181,156,255));
        void Draw(Func<ChartPoint,double?> value,Color color)
        {
            Point? previous=null;double previousTime=0;var pen=new Pen(new SolidColorBrush(color),2);
            foreach(var item in Points)
            {
                var v=value(item);if(!v.HasValue){previous=null;continue;}
                Point p=new(8+(item.Seconds-start)/range*w,16+(1-Math.Clamp(v.Value,0,100)/100)*h);
                if(previous.HasValue&&item.Seconds-previousTime<3)dc.DrawLine(pen,previous.Value,p);
                previous=p;previousTime=item.Seconds;
            }
        }
    }
}
