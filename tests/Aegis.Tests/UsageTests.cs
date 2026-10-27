using System.Net;
using System.Text.Json;
using Aegis.Infrastructure;
internal static class UsageTests
{
    private sealed class Handler:HttpMessageHandler
    {
        public List<(HttpMethod Method,string Json)> Calls{get;}=[];
        public bool Offline;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls.Add((request.Method,await request.Content!.ReadAsStringAsync(token)));
            if(Offline)throw new HttpRequestException("Offline");
            return new(HttpStatusCode.NoContent);
        }
    }
    public static async Task RunAsync(Action<bool,string> check,string root)
    {
        string dir=Path.Combine(root,"usage-"+Guid.NewGuid().ToString("N"));
        var handler=new Handler();using var http=new HttpClient(handler);
        await using(var usage=new UsageReporting(dir,"0.7.0-beta.1",http,false))
        {
            await usage.TickAsync();check(usage.Consent is null&&handler.Calls.Count==0&&!File.Exists(Path.Combine(dir,"usage-consent.json")),"No reporting or installation ID before consent");
            await usage.SetConsentAsync(true);
            using var payload=JsonDocument.Parse(handler.Calls.Single().Json);
            check(handler.Calls[0].Method==HttpMethod.Post&&payload.RootElement.EnumerateObject().Select(p=>p.Name).Order().SequenceEqual(new[]{"channel","consent","id","version"})&&Guid.TryParse(payload.RootElement.GetProperty("id").GetString(),out _),"Opt-in sends only the minimal presence contract");
            await usage.TickAsync();check(handler.Calls.Count==1,"Presence signals are throttled");
            handler.Offline=true;await usage.SetConsentAsync(false);
            check(usage.Consent==false&&usage.DeletionPending&&handler.Calls.Last().Method==HttpMethod.Delete,"Offline opt-out stops activity and queues deletion");
        }
        handler.Offline=false;
        await using(var reloaded=new UsageReporting(dir,"0.7.0-beta.1",http,false))
        {
            await reloaded.TickAsync();
            check(reloaded.Consent==false&&!reloaded.DeletionPending&&handler.Calls.Last().Method==HttpMethod.Delete&&handler.Calls.Count(c=>c.Method==HttpMethod.Post)==1,"Pending deletion survives restart without resuming activity");
            var state=await File.ReadAllTextAsync(Path.Combine(dir,"usage-consent.json"));
            check(!state.Contains("\"Id\":\"")&&!state.Contains("\"PendingDelete\":\""),"Opt-out clears local identifiers after deletion");
        }
        int before=handler.Calls.Count;
        await using(var internalRun=new UsageReporting(Path.Combine(dir,"internal"),"0.7.0-beta.1",http,true))
        {
            await internalRun.SetConsentAsync(true);await internalRun.TickAsync();await internalRun.SetConsentAsync(false);
            check(handler.Calls.Count==before,"Internal runs never contact the usage endpoint");
        }
        await File.WriteAllTextAsync(Path.Combine(dir,"usage-consent.json"),"INVALID");
        await using(var corrupt=new UsageReporting(dir,"0.7.0-beta.1",http,false))
        {await corrupt.TickAsync();check(corrupt.Consent is null&&handler.Calls.Count==before,"Corrupt consent fails closed");}
        string consentDir=Path.Combine(dir,"persisted");
        await using(var first=new UsageReporting(consentDir,"0.7.0-beta.1",http,false)){await first.SetConsentAsync(true);}
        int after=handler.Calls.Count;
        await using(var second=new UsageReporting(consentDir,"0.7.0-beta.1",http,false))
        {await second.TickAsync();check(second.Consent==true&&handler.Calls.Count==after,"Consent and successful signal throttle persist across restarts");}
    }
}
