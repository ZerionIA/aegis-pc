using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using Aegis.Infrastructure;
namespace Aegis.Desktop;

internal static class HardwareSetup
{
    internal const int Cancelled=1223,RestartRequired=3010;
    public static async Task<int> InstallAsync()
    {
        if(HardwareAccess.IsInstalled)return 0;
        string installer=Path.Combine(AppContext.BaseDirectory,"prerequisites","PawnIO_setup.exe");
        // Keep the verified executable locked against replacement while Windows launches it.
        await using var file=new FileStream(installer,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(!await HardwareAccess.VerifyInstallerAsync(file))throw new InvalidDataException("Prérequis PawnIO invalide : réinstallez le paquet officiel Aegis.");
        var info=new ProcessStartInfo(installer){UseShellExecute=true,Verb="runas"};info.ArgumentList.Add("-install");
        try
        {
            using var process=Process.Start(info)??throw new IOException("Impossible de lancer le prérequis matériel.");
            await process.WaitForExitAsync();
            if(process.ExitCode==RestartRequired)return RestartRequired;
            return process.ExitCode==0&&HardwareAccess.IsInstalled?0:process.ExitCode==0?1:process.ExitCode;
        }
        catch(System.ComponentModel.Win32Exception e)when(e.NativeErrorCode==Cancelled){return Cancelled;}
    }
    public static void SetMode(bool enabled,bool onlyIfNew=false)
    {
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--data-dir");string directory=index>=0&&index+1<args.Length?Path.GetFullPath(args[index+1]):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AegisNext");Directory.CreateDirectory(directory);
        string path=Path.Combine(directory,"settings.json");
        JsonObject settings=File.Exists(path)?JsonNode.Parse(File.ReadAllText(path))?.AsObject()??new():new();
        if(onlyIfNew&&settings.ContainsKey(nameof(MainViewModel.FullSensorAccess)))return;
        settings[nameof(MainViewModel.FullSensorAccess)]=enabled;
        File.WriteAllText(path+".hardware.tmp",settings.ToJsonString());File.Move(path+".hardware.tmp",path,true);
    }
}
