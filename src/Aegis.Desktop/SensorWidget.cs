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
        this.model=model;Title="Aegis HUD";Width=242;SizeToContent=SizeToContent.Height;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;
        Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;Focusable=false;DataContext=model;
        var stack=new StackPanel{Margin=new Thickness(12,9,12,9)};
        stack.Children.Add(new TextBlock{Text="AEGIS",FontSize=10,Foreground=new SolidColorBrush(Color.FromRgb(193,246,139)),Margin=new Thickness(0,0,0,6)});
        foreach(var (label,property) in new[]{("CPU","Cpu"),("GPU","Gpu"),("RAM","Memory"),("CPU °C","CpuTemperature"),("GPU °C","GpuTemperature")})
        {
            var row=new DockPanel{Margin=new Thickness(0,1,0,1)};row.Children.Add(new TextBlock{Text=label,Width=76,Foreground=Brushes.White,FontSize=13});
            var value=new TextBlock{Foreground=Brushes.White,FontSize=13,FontWeight=FontWeights.SemiBold};value.SetBinding(TextBlock.TextProperty,new Binding(property));row.Children.Add(value);stack.Children.Add(row);
        }
        var state=new TextBlock{FontSize=10,Foreground=new SolidColorBrush(Color.FromRgb(193,246,139)),Margin=new Thickness(0,7,0,0)};state.SetBinding(TextBlock.TextProperty,new Binding(nameof(MainViewModel.HudSession)));stack.Children.Add(state);
        panel=new Border{Background=new SolidColorBrush(Color.FromRgb(10,14,12)),CornerRadius=new CornerRadius(7),Child=stack};Content=panel;
        SourceInitialized+=(_,_)=>{var hwnd=new WindowInteropHelper(this).Handle;int style=GetWindowLong(hwnd,-20);SetWindowLong(hwnd,-20,style|0x08000000|0x00000020|0x00000080);};
        Loaded+=(_,_)=>Apply();model.PropertyChanged+=OnChanged;Closed+=(_,_)=>model.PropertyChanged-=OnChanged;
    }
    private void OnChanged(object? sender,PropertyChangedEventArgs args){if(args.PropertyName is null or nameof(MainViewModel.HudCorner) or nameof(MainViewModel.HudOpacity) or nameof(MainViewModel.HudScale) or nameof(MainViewModel.DetectedGame))Apply();}
    private void Apply()
    {
        panel.Opacity=model.HudOpacity;panel.LayoutTransform=new ScaleTransform(model.HudScale,model.HudScale);Width=242*model.HudScale;UpdateLayout();
        var hwnd=new WindowInteropHelper(this).Handle;if(hwnd==IntPtr.Zero)return;
        var screen=System.Windows.Forms.Screen.FromHandle(model.GameWindow==IntPtr.Zero?new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle:model.GameWindow);var bounds=screen.Bounds;var dpi=VisualTreeHelper.GetDpi(this);
        int x=model.HudCorner.EndsWith("Right",StringComparison.Ordinal)?bounds.Right-(int)(ActualWidth*dpi.DpiScaleX)-16:bounds.Left+16;
        int y=model.HudCorner.StartsWith("Bottom",StringComparison.Ordinal)?bounds.Bottom-(int)(ActualHeight*dpi.DpiScaleY)-16:bounds.Top+16;
        SetWindowPos(hwnd,new IntPtr(-1),x,y,0,0,0x0010|0x0001);
    }
    internal bool IsPassive{get{int style=GetWindowLong(new WindowInteropHelper(this).Handle,-20);return (style&0x08000020)==0x08000020;}}
    [DllImport("user32.dll")]private static extern int GetWindowLong(IntPtr hwnd,int index);
    [DllImport("user32.dll")]private static extern int SetWindowLong(IntPtr hwnd,int index,int value);
    [DllImport("user32.dll")]private static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
}
