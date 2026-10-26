using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
namespace Aegis.Desktop;
public sealed class SensorWidget:Window
{
    private readonly MainViewModel model;
    private readonly Border panel;
    public SensorWidget(MainViewModel model)
    {
        this.model=model;Title="Aegis HUD";SizeToContent=SizeToContent.WidthAndHeight;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;
        Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;Focusable=false;DataContext=model;
        var stack=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(12,7,12,7)};
        stack.Children.Add(new TextBlock{Text="AEGIS",FontSize=10,Foreground=new SolidColorBrush(Color.FromRgb(193,246,139)),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,12,0)});
        foreach(var (label,property) in new[]{("CPU","Cpu"),("GPU","Gpu"),("RAM","Memory"),("CPU °C","CpuTemperature"),("GPU °C","GpuTemperature")})
        {
            var row=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,13,0),VerticalAlignment=VerticalAlignment.Center};
            row.Children.Add(new TextBlock{Text=label,Foreground=new SolidColorBrush(Color.FromRgb(163,181,159)),FontSize=11,Margin=new Thickness(0,0,6,0),VerticalAlignment=VerticalAlignment.Center});
            var value=new TextBlock{Foreground=Brushes.White,FontSize=12,FontWeight=FontWeights.SemiBold,MinWidth=44};value.SetBinding(TextBlock.TextProperty,new Binding(property));row.Children.Add(value);stack.Children.Add(row);
        }
        var state=new TextBlock{FontSize=11,Foreground=new SolidColorBrush(Color.FromRgb(193,246,139)),VerticalAlignment=VerticalAlignment.Center};state.SetBinding(TextBlock.TextProperty,new Binding(nameof(MainViewModel.HudBarSession)));stack.Children.Add(state);
        panel=new Border{Background=new SolidColorBrush(Color.FromRgb(10,14,12)),CornerRadius=new CornerRadius(7),Child=stack};Content=panel;
        SourceInitialized+=(_,_)=>{var hwnd=new WindowInteropHelper(this).Handle;int style=GetWindowLong(hwnd,-20);SetWindowLong(hwnd,-20,style|0x08000000|0x00000020|0x00000080);};
        Loaded+=(_,_)=>Apply();model.PropertyChanged+=OnChanged;Closed+=(_,_)=>model.PropertyChanged-=OnChanged;
    }
    private void OnChanged(object? sender,PropertyChangedEventArgs args){if(args.PropertyName is null or nameof(MainViewModel.HudCorner) or nameof(MainViewModel.HudOpacity) or nameof(MainViewModel.HudScale) or nameof(MainViewModel.DetectedGame))Apply();}
    private void Apply()
    {
        panel.Opacity=model.HudOpacity;panel.LayoutTransform=new ScaleTransform(model.HudScale,model.HudScale);UpdateLayout();
        var hwnd=new WindowInteropHelper(this).Handle;if(hwnd==IntPtr.Zero)return;
        var screen=System.Windows.Forms.Screen.FromHandle(model.GameWindow==IntPtr.Zero?new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle:model.GameWindow);var bounds=screen.Bounds;var dpi=VisualTreeHelper.GetDpi(this);
        int x=model.HudCorner.EndsWith("Center",StringComparison.Ordinal)?bounds.Left+(bounds.Width-(int)(ActualWidth*dpi.DpiScaleX))/2:model.HudCorner.EndsWith("Right",StringComparison.Ordinal)?bounds.Right-(int)(ActualWidth*dpi.DpiScaleX)-16:bounds.Left+16;
        int y=model.HudCorner.StartsWith("Bottom",StringComparison.Ordinal)?bounds.Bottom-(int)(ActualHeight*dpi.DpiScaleY)-16:bounds.Top+16;
        SetWindowPos(hwnd,new IntPtr(-1),x,y,0,0,0x0010|0x0001);
    }
    internal bool IsPassive{get{int style=GetWindowLong(new WindowInteropHelper(this).Handle,-20);return (style&0x08000020)==0x08000020;}}
    [DllImport("user32.dll")]private static extern int GetWindowLong(IntPtr hwnd,int index);
    [DllImport("user32.dll")]private static extern int SetWindowLong(IntPtr hwnd,int index,int value);
    [DllImport("user32.dll")]private static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
}
