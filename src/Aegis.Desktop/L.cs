using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Markup;
namespace Aegis.Desktop;
public sealed class L:INotifyPropertyChanged
{
    public static L Current{get;}=new();
    private readonly Dictionary<string,string> french=Read("fr"),english=Read("en");
    private readonly Dictionary<string,string> keys;
    private L(){keys=french.ToDictionary(x=>x.Value,x=>x.Key,StringComparer.Ordinal);}
    private static Dictionary<string,string> Read(string code)
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Aegis.Desktop.Languages."+code+".json")??throw new InvalidDataException("Language pack missing: "+code);
        return JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;
    }
    public string Language{get;private set;}="fr";
    public string this[string key]=>Language=="en"&&english.TryGetValue(key,out var value)?value:french.GetValueOrDefault(key,key);
    public string Translate(string value)=>keys.TryGetValue(value,out var key)?this[key]:value;
    internal bool PacksComplete=>french.Count==english.Count&&french.Keys.All(key=>english.TryGetValue(key,out var text)&&!string.IsNullOrWhiteSpace(text));
    public void SetLanguage(string code)
    {
        string next=code=="en"?"en":"fr";if(next==Language)return;Language=next;
        CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(next);CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(next=="en"?"en-US":"fr-FR");
        PropertyChanged?.Invoke(this,new("Item[]"));PropertyChanged?.Invoke(this,new(nameof(Language)));Changed?.Invoke();
    }
    public event Action? Changed;
    public event PropertyChangedEventHandler? PropertyChanged;
}
[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension:MarkupExtension
{
    public string Key{get;set;}="";
    public override object ProvideValue(IServiceProvider provider)=>new Binding("["+Key+"]"){Source=L.Current,Mode=BindingMode.OneWay}.ProvideValue(provider);
}
public sealed class TranslationConverter:IValueConverter,IMultiValueConverter
{
    public object Convert(object[] values,Type targetType,object parameter,CultureInfo culture)=>Convert(values[0],targetType,parameter,culture);
    public object[] ConvertBack(object value,Type[] targetTypes,object parameter,CultureInfo culture)=>throw new NotSupportedException();
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is string text?L.Current.Translate(text):value;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
public sealed class LocalizedExtension:MarkupExtension
{
    public string Path{get;set;}="";
    public override object ProvideValue(IServiceProvider provider)
    {
        var binding=new MultiBinding{Converter=new TranslationConverter(),Mode=BindingMode.OneWay};
        binding.Bindings.Add(new Binding(Path));binding.Bindings.Add(new Binding(nameof(L.Language)){Source=L.Current});
        return binding.ProvideValue(provider);
    }
}
