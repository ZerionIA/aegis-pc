using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Microsoft.Win32;
using Aegis.Infrastructure;
namespace Aegis.Desktop;

public sealed partial class MainViewModel
{
    private bool minimizeToTray=true,hotkeyEnabled=true,startWithWindows;
    private string hotkeyStatus="Initialisation du raccourci…",sessionQuery="";
    private bool unsavedSession;
    public bool MinimizeToTray{get=>minimizeToTray;set{minimizeToTray=value;Changed();}}
    public bool HotkeyEnabled{get=>hotkeyEnabled;set{hotkeyEnabled=value;Changed();}}
    public bool StartWithWindows{get=>startWithWindows;set{startWithWindows=value;Changed();}}
    public string HotkeyStatus{get=>hotkeyStatus;set{hotkeyStatus=value;Changed();}}
    public string SessionQuery{get=>sessionQuery;set{sessionQuery=value;Changed();SessionsView?.Refresh();}}
    public bool IsRecording=>recorder.Active;
    public ICollectionView SessionsView{get;private set;}=null!;
    public AsyncCommand LastSessionCommand{get;private set;}=null!;
    public AsyncCommand SupportReportCommand{get;private set;}=null!;
    private void InitializeDaily()
    {
        SessionsView=new ListCollectionView(Sessions);
        SessionsView.Filter=row=>row is SessionRow item&&(item.Title.Contains(SessionQuery,StringComparison.OrdinalIgnoreCase)||item.Date.Contains(SessionQuery,StringComparison.OrdinalIgnoreCase));
        startWithWindows=StartupRegistration.IsEnabled(Path.Combine(AppContext.BaseDirectory,"Aegis.exe"));
        LastSessionCommand=new(async()=>
        {
            await RefreshSessions();if(Sessions.Count==0){Status="Aucune session enregistrée pour le moment.";return;}
            SessionQuery="";SelectedSession=Sessions[0];await LoadSession();Page="Sessions";
        },Error);
        SupportReportCommand=new(async()=>
        {
            var dialog=new SaveFileDialog{Filter="Diagnostic Aegis JSON|*.json",FileName="aegis-support"};
            if(dialog.ShowDialog()!=true)return;
            await File.WriteAllTextAsync(dialog.FileName,SupportReport.Create(VersionText,latest));Status="Rapport local préparé. Relisez-le avant de le joindre à votre message.";
        },Error);
    }
    public async Task<bool> PrepareCloseAsync()
    {
        if(recorder.Active)await FinishRecordingAsync();
        return !unsavedSession||selected is not null&&await SaveFinalAsync(selected);
    }
    internal int SmokeMarkerCount=>recorder.Current().Markers.Count;
    internal void SmokeWriteSupport(string path)=>File.WriteAllText(path,SupportReport.Create(VersionText,latest));
}
