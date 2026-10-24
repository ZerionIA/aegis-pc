using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using Aegis.Domain;
using Aegis.Collector;

if(args.Length<2||args[0]!="--pipe"||!args[1].StartsWith("aegis-",StringComparison.Ordinal)) return 2;
using var shutdown=new CancellationTokenSource();
Console.CancelKeyPress+=(_,e)=>{e.Cancel=true;shutdown.Cancel();};
using var pipe=new NamedPipeClientStream(".",args[1],PipeDirection.Out,PipeOptions.Asynchronous);
try { await pipe.ConnectAsync(15000,shutdown.Token); } catch(Exception e) when(e is TimeoutException or OperationCanceledException) { return 3; }
using var writer=new StreamWriter(pipe) { AutoFlush=true };
var windows=new WindowsProvider(); using var hardware=new HardwareProvider();
var watch=Stopwatch.StartNew(); using var timer=new PeriodicTimer(TimeSpan.FromSeconds(1));
try
{
    while(await timer.WaitForNextTickAsync(shutdown.Token))
    {
        List<Metric> metrics; string? warning=hardware.Warning;
        try { metrics=windows.Read(watch.Elapsed.TotalSeconds); metrics.AddRange(hardware.Read()); warning=hardware.Warning; }
        catch(Exception e) when(e is not OperationCanceledException)
        {
            // Une lecture défaillante ne doit pas arrêter la collecte : l'échantillon est déclaré vide, jamais inventé.
            metrics=[]; warning="Lecture des capteurs impossible sur ce cycle : "+e.Message;
        }
        var snapshot=new Snapshot(1,DateTimeOffset.UtcNow,watch.Elapsed.TotalSeconds,metrics,warning);
        await writer.WriteLineAsync(JsonSerializer.Serialize(snapshot));
    }
} catch(Exception e) when(e is IOException or OperationCanceledException) { }
return 0;
