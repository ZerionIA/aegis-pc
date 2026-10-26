using Aegis.Infrastructure;
internal static class HardwareTests
{
    public static async Task RunAsync(Action<bool,string> check)
    {
        check(HardwareAccess.RequiresInstall(null),"Missing hardware driver is offered by setup");
        check(HardwareAccess.RequiresInstall(new Version(2,1,0)),"Older prerequisite is upgraded");
        check(!HardwareAccess.RequiresInstall(new Version(2,2,0,0)),"Installed compatible driver is reused");
        check(!HardwareAccess.RequiresInstall(new Version(3,0,0)),"Newer shared driver is never downgraded");
        check(!HardwareAccess.ShouldElevate(false,true,false),"Basic mode never requests elevation");
        check(!HardwareAccess.ShouldElevate(true,false,false),"Missing driver does not cause a pointless elevation prompt");
        check(!HardwareAccess.ShouldElevate(true,true,true)&&HardwareAccess.ShouldElevate(true,true,false),"Complete mode requests elevation only when access is denied");
        await using var small=new MemoryStream(new byte[32]);
        check(!await HardwareAccess.VerifyInstallerAsync(small),"Wrong prerequisite payload length rejected");
        await using var fake=new MemoryStream(new byte[3410960]);
        check(!await HardwareAccess.VerifyInstallerAsync(fake),"Same-size untrusted prerequisite rejected by pinned SHA256");
    }
}
