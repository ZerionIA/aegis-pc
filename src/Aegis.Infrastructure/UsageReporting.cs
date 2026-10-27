using System.Net.Http.Json;
using System.Text.Json;
namespace Aegis.Infrastructure;

/// <summary>Voluntary presence only. Contains no sensor, session, process, hardware or account data.</summary>
public sealed class UsageReporting : IAsyncDisposable
{
    public static readonly Uri Endpoint = new("https://zerionia.com/api/aegis/usage");
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private sealed record State(bool? Consent=null,string? Id=null,string? PendingDelete=null,DateTimeOffset LastSent=default);
    private readonly string file,version;
    private readonly bool internalRun;
    private readonly HttpClient http;
    private readonly SemaphoreSlim gate=new(1,1);
    private readonly CancellationTokenSource lifetime=new();
    private volatile State state=new();
    private volatile bool allowed;
    private CancellationTokenSource? sending;
    private DateTimeOffset lastAttempt;
    public bool? Consent=>state.Consent;
    public bool IsInternal=>internalRun;
    public bool DeletionPending=>state.PendingDelete is not null;
    public UsageReporting(string directory,string version,HttpClient http,bool internalRun)
    {
        if(version.Length>32||!System.Text.RegularExpressions.Regex.IsMatch(version,@"^\d+\.\d+\.\d+(-[a-z0-9.]+)?$"))throw new ArgumentException("Invalid version",nameof(version));
        file=Path.Combine(directory,"usage-consent.json");this.version=version;this.http=http;this.internalRun=internalRun;
        try
        {
            if(File.Exists(file))state=JsonSerializer.Deserialize<State>(File.ReadAllText(file))??new();
            if(state.Id is not null&&!Guid.TryParse(state.Id,out _)||state.PendingDelete is not null&&!Guid.TryParse(state.PendingDelete,out _))state=new();
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){state=new();}
        allowed=state.Consent==true;
    }
    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file+".tmp",JsonSerializer.Serialize(state));
        File.Move(file+".tmp",file,true);
    }
    public async Task SetConsentAsync(bool enabled)
    {
        allowed=false;
        if(!enabled)sending?.Cancel();
        await gate.WaitAsync(lifetime.Token);
        try
        {
            var before=state;
            state=enabled?state with{Consent=true,Id=state.Id??Guid.NewGuid().ToString("D"),LastSent=state.Consent==true?state.LastSent:default}
                :state with{Consent=false,PendingDelete=state.Id??state.PendingDelete,Id=null,LastSent=default};
            try{Save();}catch{state=before;throw;}
            allowed=enabled;lastAttempt=default;
        }
        finally{gate.Release();}
        await TickAsync();
    }
    private async Task<bool> SendAsync(string id,bool remove)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));sending=timeout;
        try
        {
            using var request=new HttpRequestMessage(remove?HttpMethod.Delete:HttpMethod.Post,Endpoint);
            request.Content=JsonContent.Create(new {id,version,consent=1,channel="release"});
            using var response=await http.SendAsync(request,timeout.Token);
            return response.IsSuccessStatusCode;
        }
        catch(Exception e)when(e is HttpRequestException or OperationCanceledException){return false;}
        finally{sending=null;}
    }
    public async Task TickAsync()
    {
        if(internalRun||lifetime.IsCancellationRequested)return;
        if(!await gate.WaitAsync(0))return;
        try
        {
            if(DateTimeOffset.UtcNow-lastAttempt<Interval)return;
            lastAttempt=DateTimeOffset.UtcNow;
            if(state.PendingDelete is {} old)
            {
                if(!await SendAsync(old,true))return;
                state=state with{PendingDelete=null};Save();
            }
            if(!allowed||state.Consent!=true||state.Id is not {} id||DateTimeOffset.UtcNow-state.LastSent<Interval)return;
            if(await SendAsync(id,false)){state=state with{LastSent=DateTimeOffset.UtcNow};Save();}
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or OperationCanceledException){}
        finally{gate.Release();}
    }
    public async ValueTask DisposeAsync()
    {
        allowed=false;await lifetime.CancelAsync();
        await gate.WaitAsync();gate.Release();
        // The gate remains valid for an already queued timer callback.
    }
}
