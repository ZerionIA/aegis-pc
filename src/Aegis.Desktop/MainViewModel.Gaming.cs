using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using Aegis.Domain;
namespace Aegis.Desktop;

public sealed record Choice(string Id,string Label);
public sealed class SensorGroup(string title)
{
    public string Title{get;}=title;
    public ObservableCollection<MetricRow> Items{get;}=[];
}
public sealed partial class MainViewModel
{
    private readonly GameSessionPlanner gamePlanner=new();
    private readonly DispatcherTimer gameTimer=new(){Interval=TimeSpan.FromSeconds(4)};
    private bool closingRequested,hudAutoOpened,gameScanBusy,automaticSession,autoRecordGames=true,showGameHud=true,autoGameFrames;
    private DateTimeOffset recordingSince;
    private string customGameExecutables="",detectedGame="Aucun jeu détecté",hudCorner="TopCenter";
    private double hudOpacity=.82,hudScale=1;
    internal IntPtr GameWindow{get;private set;}
    internal string? SmokeGameOnly{get;set;}
    internal string GamingDebug=>$"detected={DetectedGame}; latest={latest is not null}; command={RecordCommand.CanExecute(null)}; active={recorder.Active}; planner={gamePlanner.Active?.Title}; status={Status}";
    public ObservableCollection<SensorGroup> SensorGroups{get;}=[];
    private string sensorFilter="all",sensorQuery="";
    private bool includeMissingSensors=true;
    public Choice[] SensorFilters{get;}=[new("all","Tous les composants"),new("Processeur · CPU","Processeur · CPU"),new("Carte graphique · GPU","Carte graphique · GPU"),new("Mémoire · RAM","Mémoire · RAM"),new("Réseau","Réseau"),new("Stockage","Stockage"),new("Système","Système")];
    public string SensorFilter{get=>sensorFilter;set{sensorFilter=value;Changed();if(latest is not null)RefreshSensorGroups(latest);}}
    public string SensorQuery{get=>sensorQuery;set{sensorQuery=value;Changed();if(latest is not null)RefreshSensorGroups(latest);}}
    public bool IncludeMissingSensors{get=>includeMissingSensors;set{includeMissingSensors=value;Changed();if(latest is not null)RefreshSensorGroups(latest);}}
    public bool AutoRecordGames{get=>autoRecordGames;set{autoRecordGames=value;Changed();}}
    public bool AutoGameFrames{get=>autoGameFrames;set{autoGameFrames=value;Changed();}}
    public bool ShowGameHud{get=>showGameHud;set{showGameHud=value;if(!value)widget?.Close();Changed();}}
    public string CustomGameExecutables{get=>customGameExecutables;set{customGameExecutables=value;Changed();}}
    public string DetectedGame{get=>detectedGame;private set{detectedGame=value;Changed();}}
    public string HudCorner{get=>hudCorner;set{hudCorner=Corners.Any(c=>c.Id==value)?value:"TopCenter";Changed();}}
    public double HudOpacity{get=>hudOpacity;set{if(double.IsFinite(value)){hudOpacity=Math.Clamp(value,.25,1);Changed();}}}
    public double HudScale{get=>hudScale;set{if(double.IsFinite(value)){hudScale=Math.Clamp(value,.75,1.5);Changed();}}}
    public Choice[] Corners{get;}=[new("TopCenter","En haut au centre"),new("BottomCenter","En bas au centre"),new("TopLeft","En haut à gauche"),new("TopRight","En haut à droite"),new("BottomLeft","En bas à gauche"),new("BottomRight","En bas à droite")];
    public Choice[] Languages{get;}=[new("fr","Français"),new("en","English")];
    public string Language{get=>L.Current.Language;set{L.Current.SetLanguage(value);Changed(null);}}
    public string CpuTemperature=>ComponentTemperature("cpu");
    public string GpuTemperature=>ComponentTemperature("gpu");
    public string HudBarSession=>recorder.Active?"● "+(DateTimeOffset.UtcNow-recordingSince).ToString(@"hh:mm:ss"):"● LIVE";
    public string HudSession=>recorder.Active?L.Current.Translate("● Session en cours")+" · "+(DateTimeOffset.UtcNow-recordingSince).ToString(@"hh\:mm\:ss"):L.Current.Translate("Mesures en direct");
    private string ComponentTemperature(string component)
    {
        var values=latest?.Metrics.Where(m=>m.Id.Contains(component,StringComparison.OrdinalIgnoreCase)&&Metrics.IsTemperatureReading(m)&&m.State==MetricState.Available&&m.Value.HasValue).Select(m=>m.Value!.Value).ToArray();
        return values is {Length:>0}?Format(values.Max(),"°C"):"—";
    }
    private void InitializeGaming(){L.Current.Changed+=OnLanguageChanged;gameTimer.Tick+=async(_,_)=>await ScanGamesAsync();gameTimer.Start();}
    private void OnLanguageChanged(){if(selected is not null)ShowSummary(selected);SensorGroups.Clear();if(latest is not null)RefreshSensorGroups(latest);Changed(null);}
    private void OpenHud(){hudAutoOpened=false;if(widget is not null){ShowGameHud=false;return;}widget=new(this);widget.Closed+=(_,_)=>widget=null;widget.Show();}
    private async Task ScanGamesAsync()
    {
        if(gameScanBusy||shuttingDown||closingRequested)return;gameScanBusy=true;
        try
        {
            string custom=CustomGameExecutables;
            var found=await Task.Run(()=>
            {
                var list=new List<(GameProcess Game,IntPtr Window)>();
                foreach(var process in Process.GetProcesses())using(process)
                {
                    try{if(SmokeGameOnly is not null&&!string.Equals(process.ProcessName,SmokeGameOnly,StringComparison.OrdinalIgnoreCase))continue;string? name=GameCatalog.Title(process.ProcessName,custom);if(name is not null)list.Add((new(process.Id,process.StartTime.ToUniversalTime().Ticks,process.ProcessName,name),process.MainWindowHandle));}
                    catch(Exception e)when(e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException){}
                }
                return list.OrderByDescending(g=>g.Window!=IntPtr.Zero).ThenBy(g=>g.Game.Id).ToArray();
            });
            if(shuttingDown||closingRequested)return;
            DetectedGame=found.FirstOrDefault().Game?.Title??"Aucun jeu détecté";
            GameWindow=found.FirstOrDefault().Window;
            if(!AutoRecordGames&&automaticSession)
            {gamePlanner.SuppressActive();automaticSession=false;if(recorder.Active)await ToggleRecord();}
            var change=gamePlanner.Observe(found.Select(g=>g.Game).ToArray(),AutoRecordGames&&!recorder.Active&&pendingSave is null&&latest is not null&&RecordCommand.CanExecute(null));
            if(change is {Started:true})
            {
                automaticSession=true;SessionName=change.Game.Title;
                // Sensor capture by default. ETW/PresentMon remains an explicit optional choice.
                SelectedProcess=AutoGameFrames?new(change.Game.Id,change.Game.Executable):new(0,"Capteurs uniquement");
                await ToggleRecord();
            }
            else if(change is {Started:false}&&automaticSession)
            {automaticSession=false;if(recorder.Active)await ToggleRecord();}
            if(found.Length==0&&hudAutoOpened){widget?.Close();hudAutoOpened=false;}
            if(ShowGameHud&&found.Length>0&&widget is null){hudAutoOpened=true;widget=new(this);widget.Closed+=(_,_)=>widget=null;widget.Show();}
        }
        catch(Exception e){Error(e);if(!recorder.Active){automaticSession=false;gamePlanner.SuppressActive();}}
        finally{gameScanBusy=false;}
    }
    private void ManualSessionIntent(){if(automaticSession||gamePlanner.Active is not null){gamePlanner.SuppressActive();automaticSession=false;}}
    internal void ResumeAfterCancelledClose(){closingRequested=false;gameTimer.Start();}
    internal void SmokePreferences()
    {
        if(!L.Current.PacksComplete)throw new InvalidOperationException("Language packs incomplete");
        string previous=Language;Language="en";
        if(L.Current.Translate("Réglages")!="Settings")throw new InvalidOperationException("English pack missing");
        Language=previous;HudCorner="BottomRight";HudOpacity=.55;HudScale=1.2;
        if(!SaveSettings(false))throw new InvalidOperationException("Preferences not saved");
        using var json=System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(data,"settings.json")));
        if(json.RootElement.GetProperty("HudCorner").GetString()!="BottomRight"||json.RootElement.GetProperty("HudScale").GetDouble()!=1.2)throw new InvalidOperationException("HUD preferences not persisted");
        HudCorner="TopCenter";HudOpacity=.82;HudScale=1;
        if(latest is not null)
        {
            RefreshSensorGroups(latest);var first=SensorGroups[0];RefreshSensorGroups(latest);
            if(!ReferenceEquals(first,SensorGroups[0]))throw new InvalidOperationException("Sensor group state lost each sample");
            SensorQuery="__no_sensor_matches__";if(SensorGroups.Count!=0)throw new InvalidOperationException("Sensor search ineffective");SensorQuery="";
        }
        SaveSettings(false);
    }
    private static string SensorCategory(Metric metric)
    {
        string id=metric.Id.ToLowerInvariant();
        if(id.Contains("gpu"))return "Carte graphique · GPU";
        if(id.Contains("cpu"))return "Processeur · CPU";
        if(id.StartsWith("ram."))return "Mémoire · RAM";
        if(id.StartsWith("net."))return "Réseau";
        if(id.StartsWith("disk."))return "Stockage";
        return "Système";
    }
    private static int SensorOrder(Metric metric)=>metric.Unit switch{"°C"=>0,"%"=>1,"W"=>2,"MHz"=>3,_=>4};
    private void RefreshSensorGroups(Snapshot snapshot)
    {
        string[] order=["Processeur · CPU","Carte graphique · GPU","Mémoire · RAM","Réseau","Stockage","Système"];
        var visible=new HashSet<string>();
        foreach(var category in order)
        {
            var rows=snapshot.Metrics.Where(m=>SensorCategory(m)==category&&(SensorFilter=="all"||SensorFilter==category)&&(IncludeMissingSensors||m.Value.HasValue)&&(m.Name.Contains(SensorQuery,StringComparison.OrdinalIgnoreCase)||m.Device.Contains(SensorQuery,StringComparison.OrdinalIgnoreCase))).OrderBy(m=>!m.Value.HasValue).ThenBy(SensorOrder).ThenBy(m=>m.Device).ThenBy(m=>m.Name,StringComparer.CurrentCultureIgnoreCase)
                .Select(m=>new MetricRow(m.Device,m.Name,Format(m.Value,m.Unit),m.Source)).ToArray();
            if(rows.Length==0)continue;visible.Add(category);
            var group=SensorGroups.FirstOrDefault(g=>g.Title==category);
            if(group is null){group=new(category);int index=SensorGroups.Count(g=>Array.IndexOf(order,g.Title)<Array.IndexOf(order,category));SensorGroups.Insert(index,group);}
            for(int i=0;i<rows.Length;i++){if(i>=group.Items.Count)group.Items.Add(rows[i]);else if(group.Items[i]!=rows[i])group.Items[i]=rows[i];}
            while(group.Items.Count>rows.Length)group.Items.RemoveAt(group.Items.Count-1);
        }
        foreach(var removed in SensorGroups.Where(g=>!visible.Contains(g.Title)).ToArray())SensorGroups.Remove(removed);
    }
}
