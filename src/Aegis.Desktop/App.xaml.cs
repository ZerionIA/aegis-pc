using System.IO;
using System.Windows;
namespace Aegis.Desktop;
public partial class App : System.Windows.Application
{
    private FileStream? instanceLock;
    private InstanceActivation? activation;
    /// <summary>Journal local des erreurs inattendues (aucun envoi réseau, aucune télémétrie).</summary>
    internal static void Log(string text)
    {
        try
        {
            string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AegisNext","logs");
            Directory.CreateDirectory(dir);string file=Path.Combine(dir,"aegis.log");
            if(File.Exists(file)&&new FileInfo(file).Length>1_000_000)File.Move(file,file+".old",true);
            File.AppendAllText(file,$"{DateTimeOffset.Now:O} {text}{Environment.NewLine}");
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException){}
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        try { instanceLock=new FileStream(Path.Combine(AppContext.BaseDirectory,".aegis-running.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.Read); }
        catch(IOException){if(!InstanceActivation.TryShowExisting())MessageBox.Show("Aegis est déjà ouvert, ou ses fichiers sont en cours de mise à jour.","Aegis");Shutdown();return;}
        if(e.Args.Contains("--install-hardware")||e.Args.Contains("--configure-basic-if-new"))
        {_ = SetupHardwareAsync(e.Args.Contains("--install-hardware"));return;}
        DispatcherUnhandledException += (_,a)=>{ Log(a.Exception.ToString()); a.Handled=true; if(a.Exception is System.Windows.Markup.XamlParseException){Shutdown(1);return;} MessageBox.Show("Une erreur inattendue s’est produite : "+a.Exception.Message+"\n\nLe détail est dans %LOCALAPPDATA%\\AegisNext\\logs\\aegis.log.","Aegis · erreur",MessageBoxButton.OK,MessageBoxImage.Warning); };
        TaskScheduler.UnobservedTaskException += (_,a)=>{ Log("Tâche non observée : "+a.Exception); a.SetObserved(); };
        AppDomain.CurrentDomain.UnhandledException += (_,a)=>Log("Erreur fatale : "+a.ExceptionObject);
        activation=new();_ = activation.ListenAsync(()=>Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,()=>{if(MainWindow is {} window){window.Show();window.WindowState=WindowState.Normal;window.Activate();}}));
        base.OnStartup(e);
        MainWindow=new MainWindow();MainWindow.Show();
    }
    protected override void OnExit(ExitEventArgs e){activation?.Dispose();instanceLock?.Dispose();base.OnExit(e);}
    private async Task SetupHardwareAsync(bool install)
    {
        try
        {
            int result=install?await HardwareSetup.InstallAsync():0;
            HardwareSetup.SetMode(install&&(result==0||result==HardwareSetup.RestartRequired),onlyIfNew:!install);
            Shutdown(result);
        }
        catch(Exception error){Log("Configuration matérielle : "+error);Shutdown(1);}
    }
}
