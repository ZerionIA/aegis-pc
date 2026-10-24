using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Aegis.Domain;
namespace Aegis.Collector;
internal sealed class WindowsProvider
{
    [StructLayout(LayoutKind.Sequential)] private struct FileTime { public uint Low,High; public readonly ulong Total => ((ulong)High<<32)|Low; }
    [StructLayout(LayoutKind.Sequential)] private struct MemoryStatus { public uint Length,Load; public ulong Total,Available,TotalPage,AvailablePage,TotalVirtual,AvailableVirtual,AvailableExtended; }
    [DllImport("kernel32.dll",SetLastError=true)] private static extern bool GetSystemTimes(out FileTime idle,out FileTime kernel,out FileTime user);
    [DllImport("kernel32.dll",SetLastError=true)] private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    private (ulong Idle,ulong Kernel,ulong User)? previousCpu;
    private readonly Dictionary<string,(long Rx,long Tx)> previousNet=[];
    private double previousTime;
    public List<Metric> Read(double now)
    {
        var m=new List<Metric>();
        void Add(string id,string device,string name,string unit,double? v,MetricState state=MetricState.Available)=>m.Add(new(id,device,name,unit,v,"Windows API",v.HasValue?state:MetricState.Missing));
        if(GetSystemTimes(out var idle,out var kernel,out var user))
        {
            double? load=null;
            if(previousCpu is {} p && idle.Total>=p.Idle && kernel.Total>=p.Kernel && user.Total>=p.User) load=Metrics.CpuPercent(idle.Total-p.Idle,kernel.Total-p.Kernel,user.Total-p.User);
            previousCpu=(idle.Total,kernel.Total,user.Total); Add("cpu.load",Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")??"CPU","Charge CPU globale","%",load);
        }
        else Add("cpu.load","CPU","Charge CPU globale","%",null);
        var memory=new MemoryStatus { Length=(uint)Marshal.SizeOf<MemoryStatus>() };
        if(GlobalMemoryStatusEx(ref memory)) { Add("ram.load","Mémoire","Mémoire physique utilisée","%",memory.Total>0?(double)(memory.Total-memory.Available)/memory.Total*100:null); Add("ram.used","Mémoire","Mémoire utilisée","Go",(memory.Total-memory.Available)/1073741824d); Add("ram.total","Mémoire","Mémoire totale","Go",memory.Total/1073741824d); }
        double? rx=null,tx=null; double delta=now-previousTime;
        foreach(var nic in NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up&&n.NetworkInterfaceType!=NetworkInterfaceType.Loopback))
        {
            try { var s=nic.GetIPStatistics(); if(previousNet.TryGetValue(nic.Id,out var p)) { var r=Metrics.Rate(s.BytesReceived,p.Rx,delta); var t=Metrics.Rate(s.BytesSent,p.Tx,delta); if(r.HasValue)rx=(rx??0)+r; if(t.HasValue)tx=(tx??0)+t; } previousNet[nic.Id]=(s.BytesReceived,s.BytesSent); } catch(NetworkInformationException) { }
        }
        previousTime=now; Add("net.rx","Réseau","Réception interfaces actives","Ko/s",rx/1024); Add("net.tx","Réseau","Envoi interfaces actives","Ko/s",tx/1024);
        foreach(var drive in DriveInfo.GetDrives().Where(d=>d.DriveType==DriveType.Fixed))
        { try { if(drive.IsReady) Add("disk."+drive.Name,"Stockage "+drive.Name,"Espace occupé","%",(drive.TotalSize-drive.AvailableFreeSpace)/(double)drive.TotalSize*100); } catch(Exception e) when(e is IOException or UnauthorizedAccessException) { } }
        Add("system.uptime","Windows","Temps depuis démarrage","h",Environment.TickCount64/3600000d);
        return m;
    }
}
