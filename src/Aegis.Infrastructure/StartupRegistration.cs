using Microsoft.Win32;
namespace Aegis.Infrastructure;

public static class StartupRegistration
{
    private const string Key="Software\\Microsoft\\Windows\\CurrentVersion\\Run",Name="AegisPCMonitor";
    public static string Command(string executable)
    {
        if(!Path.IsPathFullyQualified(executable)||executable.Contains('"'))throw new ArgumentException("Chemin de démarrage invalide.");
        return "\""+Path.GetFullPath(executable)+"\" --background";
    }
    public static bool IsEnabled(string executable)
    {
        try{using var key=Registry.CurrentUser.OpenSubKey(Key);return string.Equals(key?.GetValue(Name) as string,Command(executable),StringComparison.OrdinalIgnoreCase);}
        catch(Exception e)when(e is UnauthorizedAccessException or System.Security.SecurityException){return false;}
    }
    public static void SetEnabled(string executable,bool enabled)
    {
        string expected=Command(executable);
        using(var read=Registry.CurrentUser.OpenSubKey(Key))
        {
            string? existing=read?.GetValue(Name) as string;
            if(existing is not null&&!string.Equals(existing,expected,StringComparison.OrdinalIgnoreCase))
            {if(!enabled)return;throw new InvalidOperationException("Une autre installation Aegis gère déjà ce démarrage. Désactivez-la depuis cette installation.");}
            if((existing is not null)==enabled)return;
        }
        using var key=Registry.CurrentUser.CreateSubKey(Key);
        if(enabled)key.SetValue(Name,expected,RegistryValueKind.String);else key.DeleteValue(Name,false);
    }
}
