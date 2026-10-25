using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Aegis.Domain;
namespace Aegis.Infrastructure;

public sealed record ReleaseUpdate(string Version,string FileName,string DownloadUrl,long Size,string Sha256);

/// <summary>Only published releases in the fixed project repository are eligible. SHA-256 provides integrity, not Authenticode identity.</summary>
public sealed class ReleaseUpdateClient(HttpClient http)
{
    public const string Repository="ZerionIA/aegis-pc";
    private const long MaximumInstaller=512_000_000;
    public async Task<ReleaseUpdate?> FindAsync(string currentVersion,CancellationToken token,bool exact=false)
    {
        var current=ReleaseVersion.Parse(currentVersion)??throw new InvalidDataException("Version Aegis invalide.");
        using var request=new HttpRequestMessage(HttpMethod.Get,"https://api.github.com/repos/"+Repository+"/releases?per_page=30");
        request.Headers.UserAgent.ParseAdd("Aegis/"+currentVersion);request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
        await using var input=await response.Content.ReadAsStreamAsync(token);
        using var bounded=new MemoryStream();var buffer=new byte[8192];int count;
        while((count=await input.ReadAsync(buffer,token))>0){if(bounded.Length+count>2_000_000)throw new InvalidDataException("Réponse GitHub trop volumineuse.");bounded.Write(buffer,0,count);}
        using var json=JsonDocument.Parse(bounded.ToArray());
        ReleaseUpdate? best=null;ReleaseVersion? bestVersion=null;
        foreach(var release in json.RootElement.EnumerateArray())
        {
            if(release.GetProperty("draft").GetBoolean())continue;
            string tag=release.GetProperty("tag_name").GetString()??"";var version=ReleaseVersion.Parse(tag);
            if(version is null||(!exact&&version.CompareTo(current)<=0)||(exact&&version.CompareTo(current)!=0))continue;
            if(!exact&&current.Beta is null&&release.GetProperty("prerelease").GetBoolean())continue;
            string text=tag.TrimStart('v');string name="Aegis-"+text+"-Setup.exe";
            foreach(var asset in release.GetProperty("assets").EnumerateArray())
            {
                if(asset.GetProperty("name").GetString()!=name)continue;
                string url=asset.GetProperty("browser_download_url").GetString()??"";
                string digest=asset.TryGetProperty("digest",out var value)?value.GetString()??"":"";
                long size=asset.GetProperty("size").GetInt64();
                if(!IsAllowedAsset(text,name,url,size,digest))continue;
                if(bestVersion is null||version.CompareTo(bestVersion)>0){bestVersion=version;best=new(text,name,url,size,digest[7..].ToLowerInvariant());}
            }
        }
        return best;
    }
    public static bool IsAllowedAsset(string version,string name,string url,long size,string digest)
    {
        return ReleaseVersion.Parse(version) is not null&&name=="Aegis-"+version+"-Setup.exe"
            &&url=="https://github.com/"+Repository+"/releases/download/v"+version+"/"+name
            &&size is >0 and <=MaximumInstaller&&digest.StartsWith("sha256:",StringComparison.Ordinal)
            &&digest.Length==71&&digest[7..].All(Uri.IsHexDigit);
    }
    public async Task<string> DownloadAsync(ReleaseUpdate update,string directory,IProgress<double>? progress,CancellationToken token)
    {
        if(!IsAllowedAsset(update.Version,update.FileName,update.DownloadUrl,update.Size,"sha256:"+update.Sha256))throw new InvalidDataException("Mise à jour non autorisée.");
        Directory.CreateDirectory(directory);string path=Path.Combine(directory,update.FileName);
        if(File.Exists(path)&&await VerifyAsync(path,update,token))return path;
        string partial=path+"."+Guid.NewGuid().ToString("N")+".partial";
        try
        {
            using var response=await http.GetAsync(update.DownloadUrl,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
            if(response.RequestMessage?.RequestUri?.Scheme!=Uri.UriSchemeHttps)throw new InvalidDataException("HTTPS obligatoire.");
            if(response.Content.Headers.ContentLength is long declared&&declared!=update.Size)throw new InvalidDataException("Taille de mise à jour inattendue.");
            await using(var source=await response.Content.ReadAsStreamAsync(token))
            await using(var output=new FileStream(partial,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true))
            {
                var buffer=new byte[81920];long total=0;int count;
                while((count=await source.ReadAsync(buffer,token))>0){total+=count;if(total>update.Size)throw new InvalidDataException("Téléchargement trop volumineux.");await output.WriteAsync(buffer.AsMemory(0,count),token);progress?.Report((double)total/update.Size);}
            }
            if(!await VerifyAsync(partial,update,token))throw new InvalidDataException("L’empreinte de la mise à jour ne correspond pas : installation refusée.");
            File.Move(partial,path,true);return path;
        }
        finally{if(File.Exists(partial))File.Delete(partial);}
    }
    public static async Task<bool> VerifyAsync(string path,ReleaseUpdate update,CancellationToken token)
    {
        if(!File.Exists(path)||new FileInfo(path).Length!=update.Size)return false;
        await using var file=File.OpenRead(path);return Convert.ToHexString(await SHA256.HashDataAsync(file,token)).Equals(update.Sha256,StringComparison.OrdinalIgnoreCase);
    }
}
