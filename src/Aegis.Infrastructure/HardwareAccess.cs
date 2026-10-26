using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
namespace Aegis.Infrastructure;

public static class HardwareAccess
{
    public const string PawnIoVersion="2.2.0";
    public const string InstallerSha256="1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032";
    public static readonly Version MinimumVersion=new(2,2,0);
    public static bool RequiresInstall(Version? installed)=>installed is null||installed<MinimumVersion;
    public static bool ShouldElevate(bool fullSensors,bool installed,bool readable)=>fullSensors&&installed&&!readable;
    public static Version? InstalledVersion
    {
        get
        {
            foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
            {
                try
                {
                    using var root=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,view);
                    using var key=root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO");
                    if(Version.TryParse(key?.GetValue("DisplayVersion") as string,out var version))return version;
                }
                catch(Exception e)when(e is UnauthorizedAccessException or System.Security.SecurityException){}
            }
            return null;
        }
    }
    public static bool IsInstalled=>!RequiresInstall(InstalledVersion);
    public static bool IsElevated{get{using var identity=WindowsIdentity.GetCurrent();return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);}}
    public static bool CanReadDriver
    {
        get
        {
            using var handle=CreateFile(@"\\?\GLOBALROOT\Device\PawnIO",0xC0000000,3,IntPtr.Zero,3,0,IntPtr.Zero);
            return !handle.IsInvalid;
        }
    }
    public static async Task<bool> VerifyInstallerAsync(Stream stream,CancellationToken token=default)
    {
        if(stream.Length!=3410960)return false;
        return Convert.ToHexString(await SHA256.HashDataAsync(stream,token)).Equals(InstallerSha256,StringComparison.OrdinalIgnoreCase);
    }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern SafeFileHandle CreateFile(string name,uint access,uint sharing,IntPtr security,uint disposition,uint flags,IntPtr template);
}
