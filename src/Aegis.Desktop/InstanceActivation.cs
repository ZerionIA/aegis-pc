using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
namespace Aegis.Desktop;

public sealed class InstanceActivation:IDisposable
{
    private readonly CancellationTokenSource stop=new();
    private static string Name=>"aegis-show-"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(AppContext.BaseDirectory).ToUpperInvariant())))[..24];
    public static bool TryShowExisting()
    {
        try{using var pipe=new NamedPipeClientStream(".",Name,PipeDirection.Out,PipeOptions.Asynchronous);pipe.Connect(1000);pipe.WriteByte(1);return true;}
        catch(Exception e)when(e is TimeoutException or IOException or UnauthorizedAccessException){return false;}
    }
    public async Task ListenAsync(Action show)
    {
        try
        {
            while(!stop.IsCancellationRequested)
            {
                using var server=new NamedPipeServerStream(Name,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(stop.Token);
                using var request=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);request.CancelAfter(TimeSpan.FromSeconds(3));
                byte[] value=new byte[1];
                try{if(await server.ReadAsync(value,request.Token)==1&&value[0]==1)show();}
                catch(OperationCanceledException)when(!stop.IsCancellationRequested){}
            }
        }
        catch(OperationCanceledException){}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){App.Log("Activation d’instance : "+e.Message);}
    }
    public void Dispose(){stop.Cancel();stop.Dispose();}
}
