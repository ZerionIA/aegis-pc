using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Aegis.Desktop;
public partial class MainWindow : Window
{
    private readonly MainViewModel model=new();
    private bool cleaned;
    public MainWindow()
    {
        InitializeComponent();DataContext=model;
        Loaded+=async(_,_)=>
        {
            try{await model.InitializeAsync();}catch(Exception e){MessageBox.Show(e.Message,"Aegis");}
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--smoke");
            if(i>=0&&i+1<args.Length)
            {
                await Task.Delay(7000);
                if(args.Contains("--assistant"))model.AssistantCommand.Execute(null);
                if(args.Contains("--settings")){model.SettingsCommand.Execute(null);model.AlertsEnabled=true;model.RamThreshold=1;await Task.Delay(5000);if(model.AlertHistory.Count==0)throw new InvalidOperationException("Smoke: no alert after three real RAM samples.");}
                if(args.Contains("--widget"))model.WidgetCommand.Execute(null);
                if(args.Contains("--ai"))await model.SmokeAssistantAsync();
                if(args.Contains("--sessions"))
                {
                    model.SessionName="Vérification locale";
                    if(args.Contains("--frames"))model.SelectedProcess=new(System.Diagnostics.Process.GetCurrentProcess().Id,"Aegis");
                    model.RecordCommand.Execute(null);await Task.Delay(6000);model.RecordCommand.Execute(null);await Task.Delay(2000);model.SessionsCommand.Execute(null);
                }
                await Task.Delay(500);UpdateLayout();
                Window capture=args.Contains("--widget")?System.Windows.Application.Current.Windows.OfType<SensorWidget>().Single():this;
                var bitmap=new RenderTargetBitmap((int)capture.ActualWidth,(int)capture.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(capture);
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(args[i+1]))encoder.Save(file);
                Close();
            }
        };
        Closing+=OnClosing;
    }
    private async void OnClosing(object? sender,CancelEventArgs e)
    {
        if(cleaned)return;e.Cancel=true;IsEnabled=false;
        try{await model.DisposeAsync();}catch(Exception error){MessageBox.Show("Fermeture : "+error.Message,"Aegis");}
        cleaned=true;Close();
    }
}
