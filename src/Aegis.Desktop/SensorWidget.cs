using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
namespace Aegis.Desktop;

public sealed class SensorWidget : Window
{
    public SensorWidget(MainViewModel model)
    {
        Title="Aegis · Panneau capteurs";Width=360;Height=330;MinWidth=300;MinHeight=300;
        Background=new SolidColorBrush(Color.FromRgb(11,20,34));Foreground=Brushes.White;
        Topmost=true;DataContext=model;
        var stack=new StackPanel{Margin=new Thickness(20)};
        stack.Children.Add(new TextBlock{Text="AEGIS / LIVE",FontSize=18,Foreground=Brushes.Cyan,Margin=new Thickness(0,0,0,16)});
        foreach(var (label,property) in new[]{("CPU","Cpu"),("GPU","Gpu"),("RAM","Memory"),("Temp. max","Temperature")})
        {
            var row=new DockPanel{Margin=new Thickness(0,4,0,4)};
            row.Children.Add(new TextBlock{Text=label,Width=125,FontSize=16});
            var value=new TextBlock{FontSize=18};value.SetBinding(TextBlock.TextProperty,new Binding(property));row.Children.Add(value);stack.Children.Add(row);
        }
        var pin=new CheckBox{Content="Toujours au premier plan",IsChecked=true,Margin=new Thickness(0,14,0,8),Foreground=Brushes.White};
        pin.Checked+=(_,_)=>Topmost=true;pin.Unchecked+=(_,_)=>Topmost=false;stack.Children.Add(pin);
        stack.Children.Add(new TextBlock{Text="Panneau de bureau. Le plein écran exclusif peut le masquer.",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray});
        Content=stack;
    }
}
