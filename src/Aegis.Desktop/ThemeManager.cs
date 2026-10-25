using System.Windows;
using System.Windows.Media;
namespace Aegis.Desktop;

public static class ThemeManager
{
    public static void Apply(string theme)
    {
        string[] colors=theme switch
        {
            "Light"=>["#F2F4EF","#FFFFFF","#E6EBE2","#142218","#526454","#3A6B28","#CDD7C8","#E6EDE0"],
            "Midnight"=>["#0C101A","#151D2B","#1C293D","#EDF4FB","#9BADCA","#83C9FF","#2B3D55","#1B304A"],
            _=>["#101511","#19211B","#222E24","#EEF4E9","#A3B59F","#C1F68B","#344936","#273B2A"]
        };
        string[] keys=["Bg","Panel","Raised","Text","Muted","Accent","Line","Hover"];
        for(int i=0;i<keys.Length;i++)System.Windows.Application.Current.Resources[keys[i]]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
    }
}
