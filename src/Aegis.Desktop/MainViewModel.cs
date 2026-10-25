using System.Net.Http;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Aegis.Domain;
using Aegis.Application;
using Aegis.Infrastructure;
namespace Aegis.Desktop;
public sealed record MetricRow(string Device,string Name,string Reading,string Source);
public sealed record SessionRow(Guid Id,string Title,string Date,bool Interrupted=false){public string Display=>(Interrupted?"À reprendre · ":"")+Title+" · "+Date;}
public sealed record ChartPoint(double Seconds,double? Cpu,double? Memory);
public sealed record ProcessRow(int Id,string Name);
public sealed partial class MainViewModel : INotifyPropertyChanged,IAsyncDisposable
{
    private readonly Dispatcher dispatcher=System.Windows.Application.Current.Dispatcher;
    private readonly CollectorClient collector=new();
    private readonly SessionRecorder recorder=new();
    private readonly SessionStore store;
    private readonly PresentMonCapture frames=new();
    private readonly string data;
    private readonly SemaphoreSlim checkpointGate=new(1,1);
    private DateTimeOffset lastCheckpoint=DateTimeOffset.MinValue,statusHold=DateTimeOffset.MinValue;
    private int collectorRestarts,healthySamples;
    private bool shuttingDown;
    private SensorWidget? widget;
    private readonly ThresholdMonitor cpuAlert=new(), ramAlert=new(), tempAlert=new();
    private bool alertsEnabled;
    private double cpuThreshold=95,ramThreshold=90,tempThreshold=85;
    public bool AlertsEnabled{get=>alertsEnabled;set{alertsEnabled=value;Changed();}}
    public double CpuThreshold{get=>cpuThreshold;set{if(double.IsFinite(value)&&value is >=1 and <=100){cpuThreshold=value;Changed();}}}
    public double RamThreshold{get=>ramThreshold;set{if(double.IsFinite(value)&&value is >=1 and <=100){ramThreshold=value;Changed();}}}
    public double TempThreshold{get=>tempThreshold;set{if(double.IsFinite(value)&&value is >=1 and <=150){tempThreshold=value;Changed();}}}
    public ObservableCollection<string> AlertHistory{get;}=[];
    public Command WidgetCommand{get;}
    public static string VersionText=>(typeof(MainViewModel).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute),false).FirstOrDefault() as System.Reflection.AssemblyInformationalVersionAttribute)?.InformationalVersion.Split('+')[0]??"0.2.0";
    public string AppVersion => $"Aegis {VersionText} · Bêta";
    private readonly CancellationTokenSource lifetime=new();
    private readonly List<Snapshot> live=[];
    private Snapshot? latest;
    private Session? selected;
    public ObservableCollection<MetricRow> MetricRows {get;}=[];
    public ObservableCollection<SessionRow> Sessions {get;}=[];
    public ObservableCollection<ProcessRow> Processes {get;}=[];
    public ObservableCollection<ChartPoint> Chart {get;}=[];
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new(name));
    private string page="Vue d’ensemble",status="Connexion au collecteur…",cpu="—",memory="—",gpu="—",temperature="—",sessionName="Ma session",conditions="",summary="Aucune session sélectionnée.",comparison="Sélectionnez deux sessions pour comparer les mesures.",selectedDevice="—";
    private string presentMonPath="",frameStatus="Frames : aucune capture sélectionnée.";
    public string PresentMonPath{get=>presentMonPath;set{presentMonPath=value;Changed();}}
    public string FrameStatus{get=>frameStatus;private set{frameStatus=value;Changed();}}
    private ProcessRow? selectedProcess;public ProcessRow? SelectedProcess{get=>selectedProcess;set{selectedProcess=value;Changed();}}
    public string Page {get=>page;private set{page=value;Changed();Changed(nameof(IsOverview));Changed(nameof(IsSessions));Changed(nameof(IsHardware));Changed(nameof(IsSettings));Changed(nameof(IsDiscover));}}
    public bool IsOverview=>Page=="Vue d’ensemble";public bool IsSessions=>Page=="Sessions";public bool IsHardware=>Page=="Matériel";public bool IsSettings=>Page=="Réglages";
    public string Status {get=>status;private set{status=value;statusHold=DateTimeOffset.UtcNow.AddSeconds(6);Changed();}}
    /// <summary>Message d'ambiance : n'écrase jamais un message d'action récent.</summary>
    private void PassiveStatus(string text){if(DateTimeOffset.UtcNow<statusHold||status==text)return;status=text;Changed(nameof(Status));}
    public string Cpu {get=>cpu;private set{cpu=value;Changed();}}public string Memory{get=>memory;private set{memory=value;Changed();}}public string Gpu{get=>gpu;private set{gpu=value;Changed();}}public string Temperature{get=>temperature;private set{temperature=value;Changed();}}
    public string SelectedDevice{get=>selectedDevice;private set{selectedDevice=value;Changed();}}

    public string SessionName{get=>sessionName;set{sessionName=value;Changed();}}public string Conditions{get=>conditions;set{conditions=value;Changed();}}
    public string Summary{get=>summary;private set{summary=value;Changed();}}public string Comparison{get=>comparison;private set{comparison=value;Changed();}}


    private SessionRow? selectedSession;public SessionRow? SelectedSession{get=>selectedSession;set{selectedSession=value;Changed();}}public SessionRow? CompareSession{get;set;}
    public string RecordLabel=>recorder.Active?"Arrêter et conserver":"Démarrer une session";
    public string RecordingStatus=>recorder.Active?$"Enregistrement · {recorder.Count} échantillons":"Prêt · collecte à 1 Hz";

    public Command OverviewCommand{get;}public Command SessionsCommand{get;}public Command HardwareCommand{get;}public Command SettingsCommand{get;}public Command MarkerCommand{get;}public Command SaveSettingsCommand{get;}
    public AsyncCommand RecordCommand{get;}public AsyncCommand LoadCommand{get;}public AsyncCommand ExportCsvCommand{get;}public AsyncCommand ExportHtmlCommand{get;}public AsyncCommand CompareCommand{get;}
    public Command RefreshProcessesCommand{get;}
    public MainViewModel()
    {
        var arguments=Environment.GetCommandLineArgs();var dataIndex=Array.IndexOf(arguments,"--data-dir");
        data=dataIndex>=0&&dataIndex+1<arguments.Length?Path.GetFullPath(arguments[dataIndex+1]):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AegisNext");
        Directory.CreateDirectory(data);
        store=new(data);
        WidgetCommand=new(()=>{if(widget is null){widget=new(this);widget.Closed+=(_,_)=>widget=null;widget.Show();}else widget.Activate();});
        OverviewCommand=new(()=>Page="Vue d’ensemble");SessionsCommand=new(()=>Page="Sessions");HardwareCommand=new(()=>Page="Matériel");SettingsCommand=new(()=>Page="Réglages");
        MarkerCommand=new(()=>{recorder.Mark("Saccade ressentie");Status="Marqueur ajouté à la session.";},()=>recorder.Active&&recorder.Count>0);
        SaveSettingsCommand=new(()=>SaveSettings());
        RefreshProcessesCommand=new(RefreshProcesses);
        RecordCommand=new(ToggleRecord,Error);LoadCommand=new(LoadSession,Error);ExportCsvCommand=new(()=>Export(false),Error);ExportHtmlCommand=new(()=>Export(true),Error);
        CompareCommand=new(Compare,Error);
        LoadSettings();
        if(string.IsNullOrEmpty(PresentMonPath)&&File.Exists(Path.Combine(AppContext.BaseDirectory,"tools","PresentMon.exe")))PresentMonPath=Path.Combine(AppContext.BaseDirectory,"tools","PresentMon.exe");
        
        
        if(store.QuarantinedDatabase is not null)Status="La base de sessions était illisible : elle a été conservée à part ("+Path.GetFileName(store.QuarantinedDatabase)+") et une base neuve a été créée.";
        InitializeProduct();InitializeDaily();
        RefreshProcesses();
        collector.Sample+=s=>dispatcher.BeginInvoke(()=>Accept(s));collector.Fault+=e=>dispatcher.BeginInvoke(()=>_=OnCollectorFaultAsync(e));
    }
    public async Task InitializeAsync(){await RefreshSessions();await collector.StartAsync();_ = CheckAutomaticUpdatesAsync();}
    private static string Format(double? v,string unit)=>v.HasValue?v.Value.ToString("F1",CultureInfo.CurrentCulture)+" "+unit:"Indisponible";
    private void Accept(Snapshot s)
    {
        latest=s;live.Add(s);if(live.Count>300)live.RemoveAt(0);
        if(++healthySamples>=30)collectorRestarts=0;
        Cpu=Format(Metrics.Latest(s,"cpu.load"),"%");Memory=Format(Metrics.Latest(s,"ram.load"),"%");
        var g=s.Metrics.FirstOrDefault(m=>m.Id.Contains("gpu")&&m.Unit=="%"&&m.Name.Contains("Core",StringComparison.OrdinalIgnoreCase));Gpu=Format(g?.Value,"%");SelectedDevice=g?.Device??s.Metrics.FirstOrDefault(m=>m.Id.Contains("gpu"))?.Device??"GPU non exposé";
        var temps=s.Metrics.Where(m=>m.Unit=="°C"&&m.Value.HasValue).ToArray();Temperature=Format(temps.Length>0?temps.Max(m=>m.Value):null,"°C");
        // Le tableau détaillé n'est reconstruit que lorsqu'il est visible : moins de travail pendant les sessions.
        if(IsHardware){MetricRows.Clear();foreach(var m in s.Metrics)MetricRows.Add(new(m.Device,m.Name,Format(m.Value,m.Unit),m.Source));}
        Chart.Add(new(s.MonotonicSeconds,Metrics.Latest(s,"cpu.load"),Metrics.Latest(s,"ram.load")));if(Chart.Count>120)Chart.RemoveAt(0);
        recorder.Add(s);Changed(nameof(RecordingStatus));if(recorder.Count==1)MarkerCommand.Refresh();
        if(AlertsEnabled)
        {
            void Notice(string text){AlertHistory.Insert(0,s.Utc.ToLocalTime().ToString("T")+" · "+text);if(AlertHistory.Count>50)AlertHistory.RemoveAt(50);if(recorder.Active)recorder.Mark("Seuil : "+text);Status=text;}
            if(cpuAlert.Observe(Metrics.Latest(s,"cpu.load"),CpuThreshold,s.MonotonicSeconds))Notice($"CPU ≥ {CpuThreshold:F0} % pendant 3 mesures");
            if(ramAlert.Observe(Metrics.Latest(s,"ram.load"),RamThreshold,s.MonotonicSeconds))Notice($"RAM ≥ {RamThreshold:F0} % pendant 3 mesures");
            if(tempAlert.Observe(temps.Length>0?temps.Max(m=>m.Value):null,TempThreshold,s.MonotonicSeconds))Notice($"Capteur ≥ {TempThreshold:F0} °C pendant 3 mesures · à examiner selon le composant");
        }
        else{cpuAlert.Observe(null,CpuThreshold,s.MonotonicSeconds);ramAlert.Observe(null,RamThreshold,s.MonotonicSeconds);tempAlert.Observe(null,TempThreshold,s.MonotonicSeconds);}
        PassiveStatus(s.Warning??"Collecte locale active · températures selon disponibilité · aucun FPS simulé");
        if(recorder.Active&&DateTimeOffset.UtcNow-lastCheckpoint>=TimeSpan.FromSeconds(60)){lastCheckpoint=DateTimeOffset.UtcNow;_=CheckpointAsync(recorder.Current());}
        if(recorder.LimitReached){_=PersistLimitAsync();}
    }
    private async Task OnCollectorFaultAsync(string reason)
    {
        if(shuttingDown)return;
        healthySamples=0;latest=null;Cpu=Memory=Gpu=Temperature="—";MetricRows.Clear();
        try
        {
            if(recorder.Active)await FinishRecordingAsync("Collecteur interrompu ("+reason+") : session conservée jusqu’à l’interruption.");
            Changed(nameof(RecordLabel));Changed(nameof(RecordingStatus));MarkerCommand.Refresh();
            if(collectorRestarts>=3){Status="Collecteur indisponible : "+reason+" — redémarrez Aegis.";return;}
            collectorRestarts++;Status=$"Collecteur interrompu ({reason}) · nouvelle tentative {collectorRestarts}/3…";
            await Task.Delay(TimeSpan.FromSeconds(2*collectorRestarts),lifetime.Token);
            await collector.StartAsync();
        }
        catch(OperationCanceledException){}
        catch(Exception e) when(e is IOException or System.ComponentModel.Win32Exception or InvalidOperationException){Status="Collecteur indisponible : "+e.Message;}
    }
    private bool limitHandled;
    private async Task PersistLimitAsync()
    {
        if(limitHandled)return;limitHandled=true;
        await FinishRecordingAsync("Limite de 8 h atteinte : session conservée.");
        Changed(nameof(RecordLabel));Changed(nameof(RecordingStatus));MarkerCommand.Refresh();
    }
    /// <summary>Sauvegarde périodique : saute le cycle si la précédente n'est pas finie, ne bloque jamais l'interface.</summary>
    private async Task CheckpointAsync(Session s)
    {
        if(!await checkpointGate.WaitAsync(0))return;
        try{await store.SaveAsync(s,lifetime.Token);}
        catch(OperationCanceledException){}
        catch(Exception e) when(e is Microsoft.Data.Sqlite.SqliteException or IOException or InvalidDataException){dispatcher.Invoke(()=>Status="Sauvegarde intermédiaire impossible : "+e.Message);}
        finally{checkpointGate.Release();}
    }
    /// <summary>Sauvegarde finale : attend une éventuelle sauvegarde en cours, signale l'échec sans perdre la session en mémoire.</summary>
    private async Task<bool> SaveFinalAsync(Session s)
    {
        await checkpointGate.WaitAsync();
        try{await store.SaveAsync(s);unsavedSession=false;return true;}
        catch(Exception e) when(e is Microsoft.Data.Sqlite.SqliteException or IOException or InvalidDataException)
        {unsavedSession=true;Status="La session n’a pas pu être enregistrée ("+e.Message+"). Elle reste en mémoire : exportez-la en CSV avant de quitter.";return false;}
        finally{checkpointGate.Release();}
    }
    /// <summary>Termine la session : mesures enregistrées d'abord, puis frames (échec toléré), puis bilan.</summary>
    private async Task FinishRecordingAsync(string? notice=null)
    {
        var session=recorder.Stop();selected=session;
        bool saved=await SaveFinalAsync(session);
        try
        {
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var capture=await frames.StopAsync(timeout.Token);FrameStatus=DescribeFrames(capture);
            if(capture.Stats is not null||capture.RawFile is not null){selected=session with{Frames=capture};saved=await SaveFinalAsync(selected);}
        }
        catch(Exception e) when(e is IOException or OperationCanceledException or InvalidDataException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException){FrameStatus="Frames indisponibles : "+e.Message;}
        ShowSummary(selected);
        try{await RefreshSessions();}catch(Exception e) when(e is Microsoft.Data.Sqlite.SqliteException){Status="Liste des sessions indisponible : "+e.Message;}
        if(saved)Status=notice??"Session enregistrée.";
    }
    private void Error(Exception e)
    {
        Status=e switch
        {
            OperationCanceledException=>"Opération annulée.",
            System.Net.Http.HttpRequestException=>"Connexion GitHub indisponible. Réessayez plus tard.",
            Microsoft.Data.Sqlite.SqliteException=>"Base de sessions inaccessible : "+e.Message,
            UnauthorizedAccessException=>"Accès refusé : "+e.Message,
            _=>e.Message
        };
        if(e is not (OperationCanceledException or InvalidOperationException or FileNotFoundException or IOException or InvalidDataException))App.Log(e.ToString());
    }
    private async Task ToggleRecord()
    {
        if(recorder.Active)await FinishRecordingAsync();
        else
        {
            if(latest is null)throw new InvalidOperationException("Le collecteur ne fournit pas encore de mesures : patientez quelques secondes puis réessayez.");
            updateRequest?.Cancel();
            limitHandled=false;selected=null;
            if(SelectedProcess is {Id:>0})
            {
                try
                {
                    frames.Start(PresentMonPath,SelectedProcess.Id,SelectedProcess.Name,Path.Combine(data,"frames"));
                    FrameStatus="Capture des présentations : "+SelectedProcess.Name+" · résultats disponibles à l’arrêt.";
                }
                catch(Exception e) when(e is IOException or InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
                {FrameStatus="Frames indisponibles : "+e.Message+" Les capteurs sont enregistrés normalement.";}
            }
            else FrameStatus="Capteurs uniquement · aucun processus sélectionné pour les frames.";
            recorder.Start(SessionName,Conditions);lastCheckpoint=DateTimeOffset.UtcNow.AddSeconds(-50);
            Status="Session démarrée · les mesures restent sur ce PC.";
        }
        Changed(nameof(RecordLabel));Changed(nameof(RecordingStatus));MarkerCommand.Refresh();
    }    private async Task RefreshSessions(){var id=selected?.Id??SelectedSession?.Id;Sessions.Clear();foreach(var s in await store.ListAsync())Sessions.Add(new(s.Id,s.Name,s.Started.ToLocalTime().ToString("g"),s.Ended is null));SelectedSession=Sessions.FirstOrDefault(s=>s.Id==id);}
    private async Task LoadSession(){if(SelectedSession is null)return;selected=await store.LoadAsync(SelectedSession.Id);if(selected is not null)ShowSummary(selected);}
    private static string DescribeFrames(FrameCapture f)=>f.Stats is {} x?$"{f.Process} · {x.Frames} présentations · FPS moyens {x.AverageFps:F1} · 1% low {x.OnePercentLowFps:F1} · p99 {x.P99Milliseconds:F2} ms\n{f.Status}":f.Status;
    private void ShowSummary(Session s){var a=SessionAnalysis.Summarize(s);Summary=(s.Ended is null?"Sauvegarde intermédiaire retrouvée : la session a pu être interrompue.\n\n":"")+$"{s.Name} · {a.DurationSeconds:F0} s · {a.SampleCount} échantillons\n\n"+(s.Frames is {} f?DescribeFrames(f)+"\n\n":"")+string.Join("\n\n",a.Findings.Select(f=>$"{f.Title}\n{f.Detail}\nÀ vérifier : {f.NextCheck}"));}
    private void RefreshProcesses()
    {
        int previous=SelectedProcess?.Id??0;Processes.Clear();Processes.Add(new(0,"Capteurs uniquement"));
        foreach(var p in Process.GetProcesses().OrderBy(p=>p.ProcessName)){using(p){try{if(p.MainWindowHandle!=IntPtr.Zero)Processes.Add(new(p.Id,p.ProcessName));}catch(Exception e)when(e is InvalidOperationException or System.ComponentModel.Win32Exception){}}}
        SelectedProcess=Processes.FirstOrDefault(p=>p.Id==previous)??Processes[0];
    }
    private Session Context()=>selected??(recorder.Active?recorder.Current():new(Guid.NewGuid(),"Vue en direct",DateTimeOffset.UtcNow,null,"",live.ToArray(),[]));
    private async Task Export(bool html)
    {
        var s=Context();if(s.Samples.Count==0)throw new InvalidOperationException("Aucune mesure à exporter.");
        var d=new SaveFileDialog {Filter=html?"Rapport HTML|*.html":"CSV|*.csv",FileName="aegis-session"};
        if(d.ShowDialog()!=true)return;
        await File.WriteAllTextAsync(d.FileName,html?Reports.Html(s):Reports.Csv(s));Status="Export enregistré : "+d.FileName;
    }
    private async Task Compare()
    {
        if(SelectedSession is null||CompareSession is null)throw new InvalidOperationException("Choisissez deux sessions.");
        var a=await store.LoadAsync(SelectedSession.Id);var b=await store.LoadAsync(CompareSession.Id);if(a is null||b is null)return;
        var x=SessionAnalysis.Summarize(a);var y=SessionAnalysis.Summarize(b);
        string Delta(double? first,double? second)=>first.HasValue&&second.HasValue?$"{first:F1} → {second:F1} ({second-first:+0.0;-0.0;0.0} points)":"Mesure absente";
        Comparison=$"{a.Name} → {b.Name}\nCPU moyen : {Delta(x.CpuAverage,y.CpuAverage)}\nRAM maximale : {Delta(x.MemoryPeak,y.MemoryPeak)}\n\nComparaison descriptive : scène, durée et réglages ne sont pas validés comme identiques. Aucune conclusion causale.\nConditions A : {a.Conditions}\nConditions B : {b.Conditions}";
    }
    private sealed record Settings(string PresentMon="",bool Alerts=false,double CpuLimit=95,double RamLimit=90,double TempLimit=85,string Theme="Forest",bool AutoUpdates=true,DateTimeOffset LastUpdateCheck=default,bool MinimizeToTray=true,bool HotkeyEnabled=true);
    private void LoadSettings()
    {
        try{var path=Path.Combine(data,"settings.json");if(!File.Exists(path))return;var settings=JsonSerializer.Deserialize<Settings>(File.ReadAllText(path));if(settings is null)return;PresentMonPath=settings.PresentMon;AlertsEnabled=settings.Alerts;CpuThreshold=settings.CpuLimit;RamThreshold=settings.RamLimit;TempThreshold=settings.TempLimit;Theme=settings.Theme;AutoUpdates=settings.AutoUpdates;lastUpdateCheck=settings.LastUpdateCheck;MinimizeToTray=settings.MinimizeToTray;HotkeyEnabled=settings.HotkeyEnabled;}
        catch(Exception e)when(e is IOException or JsonException or UnauthorizedAccessException){Status="Réglages ignorés : "+e.Message;}
    }
    private bool SaveSettings(bool announce=true)
    {
        try{if(announce)StartupRegistration.SetEnabled(Path.Combine(AppContext.BaseDirectory,"Aegis.exe"),StartWithWindows);var path=Path.Combine(data,"settings.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(new Settings(PresentMonPath,AlertsEnabled,CpuThreshold,RamThreshold,TempThreshold,Theme,AutoUpdates,lastUpdateCheck,MinimizeToTray,HotkeyEnabled)));File.Move(path+".tmp",path,true);if(announce)Status="Réglages enregistrés.";return true;}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException){Error(e);return false;}
    }
    private static async Task Safe(Func<Task> step){try{await step();}catch(Exception e){App.Log("Fermeture : "+e);}}
    /// <summary>Fermeture : chaque étape est isolée pour que la session soit enregistrée et les processus enfants arrêtés quoi qu'il arrive.</summary>
    public async ValueTask DisposeAsync()
    {
        shuttingDown=true;updateTimer.Stop();updateRequest?.Cancel();
        widget?.Close();
        await Safe(async()=>await lifetime.CancelAsync());
        await Safe(async()=>
        {
            if(!recorder.Active)return;
            var s=recorder.Stop();
            try
            {
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try{var capture=await frames.StopAsync(timeout.Token);if(capture.Stats is not null||capture.RawFile is not null)s=s with{Frames=capture};}
                catch(Exception e) when(e is IOException or OperationCanceledException or InvalidDataException or InvalidOperationException or System.ComponentModel.Win32Exception){App.Log("Frames à la fermeture : "+e.Message);}
            }
            finally{await SaveFinalAsync(s);}
        });
        await Safe(async()=>await frames.DisposeAsync().AsTask());
        await Safe(async()=>await collector.DisposeAsync().AsTask());
        await checkpointGate.WaitAsync();checkpointGate.Release();
        lifetime.Dispose();
    }
}
