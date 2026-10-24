using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aegis.Domain;
namespace Aegis.Infrastructure;
public sealed class PresentMonCapture : IAsyncDisposable
{
    private Process? process;
    private string? file;
    private string? target;
    private string sessionName="";
    private string executable="";
    private Task<string>? error;
    public bool Active=>process is not null&&!process.HasExited;
    public void Start(string exe,int processId,string name,string directory)
    {
        if(process is not null)throw new InvalidOperationException("Capture frames déjà ouverte.");
        if(!File.Exists(exe))throw new FileNotFoundException("PresentMon introuvable.");
        Directory.CreateDirectory(directory);file=Path.Combine(directory,Guid.NewGuid()+".frames.csv");target=name;sessionName="aegis-"+Guid.NewGuid().ToString("N");executable=exe;
        var info=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardError=true,RedirectStandardOutput=true};
        foreach(var a in new[]{"--process_id",processId.ToString(CultureInfo.InvariantCulture),"--output_file",file,"--v1_metrics","--no_console_stats","--session_name",sessionName,"--terminate_on_proc_exit","--timed","28800","--terminate_after_timed"})info.ArgumentList.Add(a);
        process=Process.Start(info)??throw new IOException("Capture PresentMon impossible.");error=process.StandardError.ReadToEndAsync();_=process.StandardOutput.ReadToEndAsync();
    }
    public async Task<FrameCapture> StopAsync(CancellationToken token)
    {
        if(process is null)return new("PresentMon","","",null,"Pas de capture de frames",null);
        try
        {
            if(!process.HasExited)
            {
                var info=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardError=true,RedirectStandardOutput=true};
                info.ArgumentList.Add("--session_name");info.ArgumentList.Add(sessionName);info.ArgumentList.Add("--terminate_existing_session");
                using var stop=Process.Start(info);if(stop is not null){_=stop.StandardOutput.ReadToEndAsync();_=stop.StandardError.ReadToEndAsync();await stop.WaitForExitAsync(token);}
                try{await process.WaitForExitAsync(token).WaitAsync(TimeSpan.FromSeconds(5),token);}catch(TimeoutException){process.Kill(true);await process.WaitForExitAsync(token);}
            }
            string err=error is null?"":await error;
            if(file is null||!File.Exists(file))return new("PresentMon",target??"","",null,EtwMessage(err),null);
            return await ImportAsync(file,target??"",token);
        }
        finally{if(!process.HasExited)process.Kill(true);process.Dispose();process=null;}
    }
    /// <summary>Message explicite quand PresentMon ne peut pas ouvrir sa session ETW. Aegis ne modifie jamais ces droits.</summary>
    public static string EtwMessage(string stderr)
    {
        string detail=stderr.Trim();if(detail.Length>200)detail=detail[..200];
        bool denied=detail.Contains("access",StringComparison.OrdinalIgnoreCase)||detail.Contains("denied",StringComparison.OrdinalIgnoreCase)||detail.Contains("accès",StringComparison.OrdinalIgnoreCase)||detail.Contains("privilege",StringComparison.OrdinalIgnoreCase)||detail.Contains("ETW",StringComparison.OrdinalIgnoreCase);
        return denied
            ?"Capture des frames refusée par Windows (accès ETW). PresentMon exige des droits d'administrateur ou l'appartenance au groupe « Performance Log Users ». Aegis ne modifie pas ces réglages : relancez Aegis en administrateur ou ajoutez votre compte vous-même. Les mesures matérielles de la session restent valides. "+detail
            :"Capture de frames absente : processus incompatible ou PresentMon arrêté trop tôt. "+detail;
    }
    public static async Task<FrameCapture> ImportAsync(string file,string name,CancellationToken token=default)
    {
        if(new FileInfo(file).Length>512L*1024*1024)throw new InvalidDataException("Capture supérieure à 512 Mo : import refusé.");
        using var reader=new StreamReader(file);var header=Split(await reader.ReadLineAsync(token)??"");
        int chain=Array.IndexOf(header,"SwapChainAddress"),time=Array.IndexOf(header,"MsBetweenPresents");
        if(time<0)throw new InvalidDataException("CSV incompatible : colonne MsBetweenPresents absente.");
        var groups=new Dictionary<string,List<double>>();int skipped=0,rows=0;
        while(await reader.ReadLineAsync(token) is {} line)
        {
            if(++rows>1000000)throw new InvalidDataException("Capture supérieure à un million de frames : import refusé.");
            var cells=Split(line);
            if(cells.Length<=time||!double.TryParse(cells[time],NumberStyles.Float,CultureInfo.InvariantCulture,out double t)||t<=0||!double.IsFinite(t)){skipped++;continue;}
            string key=chain>=0&&cells.Length>chain?cells[chain]:"unique";
            if(!groups.TryGetValue(key,out var values)){if(groups.Count>=64){skipped++;continue;}groups[key]=values=[];}values.Add(t);
        }
        if(groups.Count==0)return new("PresentMon · cadence des Present()",name,"",null,"Aucune frame exploitable. Capture non prise en charge ou accès ETW refusé.",file);
        var dominant=groups.MaxBy(g=>g.Value.Count);
        return new("PresentMon · cadence des Present()",name,dominant.Key,FrameAnalysis.Calculate(dominant.Value),$"Swapchain dominant · {skipped} ligne(s) écartée(s) · {groups.Count} chaîne(s). Mesure des présentations, pas certification de l’affichage effectif.",file);
    }
    public static string[] Split(string line)
    {
        var result=new List<string>();var cell=new StringBuilder();bool quoted=false;
        for(int i=0;i<line.Length;i++){char c=line[i];if(c=='"'){if(quoted&&i+1<line.Length&&line[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}else if(c==','&&!quoted){result.Add(cell.ToString());cell.Clear();}else cell.Append(c);}result.Add(cell.ToString());return result.ToArray();
    }
    public async ValueTask DisposeAsync(){if(process is not null){using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));try{await StopAsync(timeout.Token);}catch(Exception e)when(e is IOException or OperationCanceledException or InvalidDataException){if(process is not null&&!process.HasExited)process.Kill(true);process?.Dispose();process=null;}}}
}
