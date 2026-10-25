using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Threading;
using Aegis.Infrastructure;
namespace Aegis.Desktop;

public sealed record ThemeChoice(string Id,string Label);
public sealed record ProjectLink(string Title,string Description,string Url);
public sealed partial class MainViewModel
{
    private readonly HttpClient updateHttp=new(){Timeout=TimeSpan.FromSeconds(90)};
    private ReleaseUpdate? nextUpdate;
    private CancellationTokenSource? updateRequest;
    private readonly DispatcherTimer updateTimer=new(){Interval=TimeSpan.FromMinutes(15)};
    private string theme="Forest",updateStatus="Vérification automatique des releases GitHub activée.";
    private bool autoUpdates=true,updateBusy,updateReady;
    private double updateProgress;
    private DateTimeOffset lastUpdateCheck;
    public ThemeChoice[] Themes{get;}=[new("Forest","Forêt"),new("Midnight","Minuit"),new("Light","Clair")];
    public ProjectLink[] Projects{get;}=[
        new("Zerionia Studio","Le studio, les outils et les autres projets.","https://zerionia.com"),
        new("@ZerioniaHQ","Les nouvelles versions et les coulisses du studio.","https://x.com/ZerioniaHQ"),
        new("Nos projets open source","Explorer le GitHub ZerionIA et contribuer.","https://github.com/ZerionIA"),
        new("Nous contacter","contact@zerionia.com","mailto:contact@zerionia.com")];
    public bool IsDiscover=>Page=="Zerionia";
    public Command DiscoverCommand{get;private set;}=null!;
    public ParameterCommand OpenProjectCommand{get;private set;}=null!;
    public AsyncCommand CheckUpdatesCommand{get;private set;}=null!;
    public AsyncCommand InstallUpdateCommand{get;private set;}=null!;
    public event Action? UpdateInstallationRequested;
    public string Theme{get=>theme;set{theme=Themes.Any(t=>t.Id==value)?value:"Forest";ThemeManager.Apply(theme);Changed();}}
    public bool AutoUpdates{get=>autoUpdates;set{autoUpdates=value;Changed();if(!value)updateRequest?.Cancel();}}
    public string UpdateStatus{get=>updateStatus;private set{updateStatus=value;Changed();}}
    public bool UpdateBusy{get=>updateBusy;private set{updateBusy=value;Changed();}}
    public bool UpdateReady{get=>updateReady;private set{updateReady=value;Changed();InstallUpdateCommand?.Refresh();}}
    public double UpdateProgress{get=>updateProgress;private set{updateProgress=value;Changed();}}
    public string UpdateButtonLabel=>UpdateReady?"Installer la mise à jour":"À jour";
    public ProjectLink? SelectedProject{get;set;}
    private void InitializeProduct()
    {
        DiscoverCommand=new(()=>Page="Zerionia");
        OpenProjectCommand=new(value=>{if(value is ProjectLink link&&Projects.Contains(link))Process.Start(new ProcessStartInfo(link.Url){UseShellExecute=true});});
        CheckUpdatesCommand=new(()=>CheckUpdatesAsync(),Error,()=>!recorder.Active);
        InstallUpdateCommand=new(InstallUpdateAsync,Error,()=>UpdateReady&&!recorder.Active);
        var errorPath=Path.Combine(data,"update-error.txt");
        if(File.Exists(errorPath)){try{UpdateStatus="Dernière installation : "+File.ReadAllText(errorPath);}catch(IOException){}}
        var cache=Path.Combine(data,"updates");
        if(Directory.Exists(cache))foreach(var directory in Directory.EnumerateDirectories(cache,"runner-*"))
        {try{if(Directory.GetLastWriteTimeUtc(directory)<DateTime.UtcNow.AddDays(-1))Directory.Delete(directory,true);}catch(Exception e)when(e is IOException or UnauthorizedAccessException){}}
        updateTimer.Tick+=async(_,_)=>await CheckAutomaticUpdatesAsync();updateTimer.Start();
    }
    private Task CheckAutomaticUpdatesAsync()=>AutoUpdates&&!recorder.Active&&DateTimeOffset.UtcNow-lastUpdateCheck>=TimeSpan.FromHours(24)?CheckUpdatesAsync():Task.CompletedTask;
    private async Task CheckUpdatesAsync()
    {
        if(UpdateBusy||recorder.Active||shuttingDown)return;
        updateRequest?.Dispose();updateRequest=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);updateRequest.CancelAfter(TimeSpan.FromMinutes(10));
        UpdateBusy=true;UpdateProgress=0;
        try
        {
            UpdateStatus="Vérification des releases GitHub…";
            var client=new ReleaseUpdateClient(updateHttp);nextUpdate=await client.FindAsync(VersionText,updateRequest.Token);
            lastUpdateCheck=DateTimeOffset.UtcNow;SaveSettings(false);
            UpdateReady=false;
            if(nextUpdate is null){UpdateStatus="Vous utilisez la dernière version disponible sur ce canal.";return;}
            UpdateStatus="Téléchargement de "+nextUpdate.Version+" · "+(nextUpdate.Size/1_000_000)+" Mo…";
            await client.DownloadAsync(nextUpdate,Path.Combine(data,"updates"),new Progress<double>(p=>UpdateProgress=p*100),updateRequest.Token);
            UpdateReady=true;Changed(nameof(UpdateButtonLabel));UpdateStatus="Version "+nextUpdate.Version+" prête. L’installation se lance après votre confirmation et conserve vos sessions.";
        }
        catch(OperationCanceledException){UpdateStatus="Vérification ou téléchargement suspendu. Réessayez après la session.";}
        catch(Exception e)when(e is HttpRequestException or IOException or System.Text.Json.JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException){UpdateStatus="Mise à jour indisponible : "+e.Message;}
        finally{UpdateBusy=false;}
    }
    private async Task InstallUpdateAsync()
    {
        if(recorder.Active||nextUpdate is null)throw new InvalidOperationException("Terminez la session avant la mise à jour.");
        string exe=Path.Combine(AppContext.BaseDirectory,"updater","Aegis.Updater.exe");
        if(!File.Exists(exe))throw new FileNotFoundException("Outil de mise à jour absent : réinstallez le paquet complet.");
        if(!await ReleaseUpdateClient.VerifyAsync(Path.Combine(data,"updates",nextUpdate.FileName),nextUpdate,lifetime.Token))throw new InvalidDataException("Installateur modifié ou absent.");
        var info=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
        info.ArgumentList.Add(Environment.ProcessId.ToString());info.ArgumentList.Add(nextUpdate.Version);info.ArgumentList.Add(data);
        string source=Path.GetDirectoryName(exe)!;
        string runner=Path.Combine(data,"updates","runner-"+Guid.NewGuid().ToString("N"));
        foreach(var file in Directory.EnumerateFiles(source,"*",SearchOption.AllDirectories))
        {
            string destination=Path.Combine(runner,Path.GetRelativePath(source,file));Directory.CreateDirectory(Path.GetDirectoryName(destination)!);File.Copy(file,destination);
        }
        info.FileName=Path.Combine(runner,"Aegis.Updater.exe");
        info.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"Aegis.exe"));
        Process.Start(info);UpdateInstallationRequested?.Invoke();
    }
}
