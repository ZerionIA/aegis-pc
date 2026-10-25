using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Aegis.Desktop;
public partial class MainWindow : Window
{
    private readonly MainViewModel model=new();
    private bool cleaned,closing;
    private DailyCompanion? companion;
    public MainWindow()
    {
        InitializeComponent();DataContext=model;model.UpdateInstallationRequested+=()=>Close();
        SourceInitialized+=(_,_)=>{companion=new(this,model);companion.Attach();};
        StateChanged+=(_,_)=>{if(WindowState==WindowState.Minimized&&model.MinimizeToTray)companion?.Hide();};
        Loaded+=async(_,_)=>
        {
            try{await model.InitializeAsync();}catch(Exception e){MessageBox.Show(e.Message,"Aegis");}
            var args=Environment.GetCommandLineArgs();if(args.Contains("--background"))companion?.Hide();int i=Array.IndexOf(args,"--smoke");
            if(i>=0&&i+1<args.Length)
            {
                await Task.Delay(7000);
                if(args.Contains("--daily")){if(companion is null||!companion.TrayVisible)throw new InvalidOperationException("Tray icon missing");companion.Hide();if(IsVisible)throw new InvalidOperationException("Tray hide failed");companion.Restore();model.SessionName="Quotidien test";model.RecordCommand.Execute(null);await Task.Delay(3000);var markers=model.SmokeMarkerCount;companion.SmokeDispatchMarker();if(model.SmokeMarkerCount!=markers+1)throw new InvalidOperationException("Shortcut marker missing");model.RecordCommand.Execute(null);await Task.Delay(2000);model.LastSessionCommand.Execute(null);await Task.Delay(500);model.SessionQuery="Quotidien";if(model.SessionsView.IsEmpty)throw new InvalidOperationException("Session search failed");model.SmokeWriteSupport(args[i+1]+".json");}
                if(args.Contains("--discover"))model.DiscoverCommand.Execute(null);
                if(args.Contains("--light"))model.Theme="Light";
                if(args.Contains("--settings")){model.SettingsCommand.Execute(null);model.AlertsEnabled=true;model.RamThreshold=1;await Task.Delay(5000);if(model.AlertHistory.Count==0)throw new InvalidOperationException("Smoke: no alert after three real RAM samples.");}
                if(args.Contains("--widget"))model.WidgetCommand.Execute(null);
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
        if(cleaned)return;e.Cancel=true;if(closing)return;closing=true;IsEnabled=false;
        bool saved;try{saved=await model.PrepareCloseAsync();}catch(Exception error){App.Log(error.ToString());closing=false;IsEnabled=true;MessageBox.Show("Fermeture interrompue : "+error.Message,"Aegis");return;}
        if(!saved&&MessageBox.Show("La session ne peut pas être sauvegardée. Fermer quand même et perdre les mesures non enregistrées ?","Aegis",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)!=MessageBoxResult.Yes){closing=false;IsEnabled=true;return;}
        try{await model.DisposeAsync();}catch(Exception error){MessageBox.Show("Fermeture : "+error.Message,"Aegis");}
        companion?.Dispose();cleaned=true;Close();
    }
}
