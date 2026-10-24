using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using Aegis.Domain;
namespace Aegis.Desktop;
/// <summary>Lance et supervise le processus collecteur. Redémarrable après une panne.</summary>
public sealed class CollectorClient : IAsyncDisposable
{
    private Process? process;
    private NamedPipeServerStream? pipe;
    private CancellationTokenSource? stop;
    private Task? loop;
    public event Action<Snapshot>? Sample;
    public event Action<string>? Fault;
    public async Task StartAsync()
    {
        await StopCurrentAsync();
        string exe=Path.Combine(AppContext.BaseDirectory,"collector","Aegis.Collector.exe");
        if(!File.Exists(exe))throw new FileNotFoundException("Collecteur absent. Réinstallez Aegis ou utilisez le dossier portable complet.",exe);
        string name="aegis-"+Guid.NewGuid().ToString("N");
        var cancellation=new CancellationTokenSource();stop=cancellation;
        pipe=new NamedPipeServerStream(name,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
        var info=new ProcessStartInfo(exe) { UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden };
        info.ArgumentList.Add("--pipe"); info.ArgumentList.Add(name);
        try{process=Process.Start(info)??throw new IOException("Le collecteur n'a pas pu démarrer.");}
        catch{pipe.Dispose();pipe=null;throw;}
        loop=ReadAsync(pipe,cancellation.Token);
    }
    private async Task ReadAsync(NamedPipeServerStream server,CancellationToken token)
    {
        try
        {
            using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token)) {timeout.CancelAfter(TimeSpan.FromSeconds(20));await server.WaitForConnectionAsync(timeout.Token);}
            using var reader=new StreamReader(server);
            while(!token.IsCancellationRequested)
            {
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(20));
                string? line=await reader.ReadLineAsync(timeout.Token);
                if(line is null)throw new IOException("Le collecteur s’est arrêté.");
                if(line.Length>1000000)throw new InvalidDataException("Message collecteur hors limites.");
                var s=JsonSerializer.Deserialize<Snapshot>(line);
                if(s is null||s.Protocol!=1||s.Metrics.Count>3000||!double.IsFinite(s.MonotonicSeconds))throw new InvalidDataException("Protocole collecteur incompatible.");
                Sample?.Invoke(s);
            }
        }catch(Exception e) when(e is IOException or OperationCanceledException or JsonException or InvalidDataException or ObjectDisposedException) { if(!token.IsCancellationRequested)Fault?.Invoke(e is OperationCanceledException?"Le collecteur ne répond plus (délai dépassé).":e.Message); }
    }
    private async Task StopCurrentAsync()
    {
        var cancellation=stop;stop=null;
        if(cancellation is not null)await cancellation.CancelAsync();
        try{pipe?.Dispose();}catch(IOException){}
        pipe=null;
        try{if(process is not null&&!process.HasExited){process.Kill(entireProcessTree:true);await process.WaitForExitAsync();}}
        catch(Exception e) when(e is InvalidOperationException or System.ComponentModel.Win32Exception){}
        process?.Dispose();process=null;
        if(loop is not null)try{await loop;}catch(Exception e) when(e is ObjectDisposedException or OperationCanceledException){}
        loop=null;cancellation?.Dispose();
    }
    public async ValueTask DisposeAsync()=>await StopCurrentAsync();
}