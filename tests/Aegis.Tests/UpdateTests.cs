using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Aegis.Domain;
using Aegis.Infrastructure;

public static class UpdateTests
{
    public static async Task RunAsync(Action<bool,string> check,string root)
    {
        check(ReleaseVersion.Parse("0.3.0-beta.10")!.CompareTo(ReleaseVersion.Parse("0.3.0-beta.2"))>0,"Release versions compare beta numbers numerically");
        check(ReleaseVersion.Parse("0.3.0")!.CompareTo(ReleaseVersion.Parse("0.3.0-beta.99"))>0,"Stable release follows prerelease");
        check(ReleaseVersion.Parse("../0.3.0") is null&&ReleaseVersion.Parse("0.3.0-beta.x") is null,"Malformed release tags rejected");
        byte[] bytes=[77,90,10,23,42];string hash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        const string version="0.4.0-beta.1",name="Aegis-0.4.0-beta.1-Setup.exe";
        const string url="https://github.com/ZerionIA/aegis-pc/releases/download/v0.4.0-beta.1/"+name;
        string Releases(string assetUrl=url,string digestPrefix="sha256:")=>JsonSerializer.Serialize(new[]{new{draft=false,prerelease=true,tag_name="v"+version,assets=new[]{new{name,browser_download_url=assetUrl,digest=digestPrefix+hash,size=bytes.Length}}}});
        using var http=new HttpClient(new Handler(req=>req.RequestUri!.Host=="api.github.com"?System.Text.Encoding.UTF8.GetBytes(Releases()):bytes));
        var client=new ReleaseUpdateClient(http);
        var candidate=await client.FindAsync("0.3.0-beta.1",CancellationToken.None);
        check(candidate is not null&&candidate.Version==version,"Newer beta release found through fixed repository");
        check(await client.FindAsync("0.4.0-beta.1",CancellationToken.None) is null,"Same version never offered as update");
        check(await client.FindAsync("0.3.0",CancellationToken.None) is null,"Stable channel does not install a beta");
        using(var poisoned=new HttpClient(new Handler(_=>System.Text.Encoding.UTF8.GetBytes(Releases("https://evil.invalid/setup.exe")))))
            check(await new ReleaseUpdateClient(poisoned).FindAsync("0.3.0-beta.1",CancellationToken.None) is null,"Installer outside project release path rejected");
        using(var missing=new HttpClient(new Handler(_=>System.Text.Encoding.UTF8.GetBytes(Releases(digestPrefix:"invalid:")))))
            check(await new ReleaseUpdateClient(missing).FindAsync("0.3.0-beta.1",CancellationToken.None) is null,"Missing SHA256 metadata prevents update");
        string dir=Path.Combine(root,"updates-"+Guid.NewGuid().ToString("N"));
        string file=await client.DownloadAsync(candidate!,dir,null,CancellationToken.None);
        check(await ReleaseUpdateClient.VerifyAsync(file,candidate!,CancellationToken.None),"Downloaded installer size and hash verified");
        await File.WriteAllBytesAsync(file,[1,2,3]);
        check(!await ReleaseUpdateClient.VerifyAsync(file,candidate!,CancellationToken.None),"Modified downloaded installer rejected");
        using(var corrupt=new HttpClient(new Handler(_=>[77,90,11,23,42])))
        {
            bool rejected=false;try{await new ReleaseUpdateClient(corrupt).DownloadAsync(candidate!,dir,null,CancellationToken.None);}catch(InvalidDataException){rejected=true;}
            check(rejected&&!Directory.EnumerateFiles(dir,"*.partial").Any(),"Bad download hash rejected and partial file cleaned");
        }
        check(!ReleaseUpdateClient.IsAllowedAsset(version,name,url,513_000_000,"sha256:"+hash),"Oversized release asset rejected");
    }
    private sealed class Handler(Func<HttpRequestMessage,byte[]> body):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
            =>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){RequestMessage=request,Content=new ByteArrayContent(body(request))});
    }
}
