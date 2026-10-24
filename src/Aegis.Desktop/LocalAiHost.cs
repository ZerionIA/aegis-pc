using System.Diagnostics;
using System.IO;
namespace Aegis.Desktop;
/// <summary>Moteur Ollama local facultatif, démarré à la demande et arrêté pendant les sessions.</summary>
public sealed class LocalAiHost(string dataDirectory) : IDisposable
{
    public const int Port=11539;
    private Process? process;
    public string ModelsDirectory=>Path.Combine(dataDirectory,"models");
    /// <summary>Moteur installé depuis l'interface, sinon moteur livré avec d'anciennes versions portables.</summary>
    public string? EnginePath
    {
        get
        {
            foreach(var candidate in new[]{Path.Combine(dataDirectory,"components","ollama-engine","ollama.exe"),Path.Combine(AppContext.BaseDirectory,"tools","ollama","ollama.exe")})
                if(File.Exists(candidate))return candidate;
            return null;
        }
    }
    public bool EngineAvailable=>EnginePath is not null;
    public bool Running=>process is not null&&!process.HasExited;
    public void Start()
    {
        if(Running)return;
        string exe=EnginePath??throw new FileNotFoundException("Moteur IA non installé. Ouvrez Réglages → Composants facultatifs pour l’installer.");
        Directory.CreateDirectory(ModelsDirectory);
        var info=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};info.ArgumentList.Add("serve");
        info.Environment["OLLAMA_HOST"]="127.0.0.1:"+Port;
        info.Environment["OLLAMA_MODELS"]=ModelsDirectory;
        info.Environment["OLLAMA_NUM_PARALLEL"]="1";info.Environment["OLLAMA_MAX_LOADED_MODELS"]="1";
        info.Environment["OLLAMA_NOPRUNE"]="1";
        process?.Dispose();
        process=Process.Start(info)??throw new IOException("Moteur local impossible à démarrer.");
    }
    /// <summary>Libère le moteur (mémoire, processus) ; il redémarre à la prochaine demande.</summary>
    public void Stop()
    {
        if(process is null)return;
        try{if(!process.HasExited){process.Kill(true);process.WaitForExit(3000);}}
        catch(Exception e) when(e is InvalidOperationException or System.ComponentModel.Win32Exception){}
        process.Dispose();process=null;
    }
    /// <summary>Un modèle est présent si son manifeste est dans le dossier des modèles Aegis.</summary>
    public bool HasModel(string tag)
    {
        var parts=tag.Split(':');if(parts.Length!=2)return false;
        return File.Exists(Path.Combine(ModelsDirectory,"manifests","registry.ollama.ai","library",parts[0],parts[1]));
    }
    public void Dispose()=>Stop();
}