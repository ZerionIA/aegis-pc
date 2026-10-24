using LibreHardwareMonitor.Hardware;
using Aegis.Domain;
namespace Aegis.Collector;
internal sealed class HardwareProvider : IDisposable
{
    private readonly Computer computer=new() { IsCpuEnabled=true, IsGpuEnabled=true, IsMotherboardEnabled=false, IsStorageEnabled=false, IsControllerEnabled=false };
    public string? Warning { get; private set; }
    public HardwareProvider() { try { computer.Open(); } catch(Exception e) { Warning="Capteurs matériels partiels : "+e.Message; } }
    public List<Metric> Read()
    {
        var result=new List<Metric>();
        foreach(var h in computer.Hardware) ReadHardware(h,result);
        if(!result.Any(m=>m.Unit=="°C")) result.Add(new("cpu.temperature","CPU","Température non exposée","°C",null,"LibreHardwareMonitor",MetricState.Missing));
        return result;
    }
    private void ReadHardware(IHardware h,List<Metric> result)
    {
        try
        {
            h.Update();
            foreach(var s in h.Sensors)
            {
                string? unit=s.SensorType switch { SensorType.Temperature=>"°C",SensorType.Load=>"%",SensorType.Clock=>"MHz",SensorType.Fan=>"tr/min",SensorType.Power=>"W",SensorType.SmallData=>"Mo",SensorType.Data=>"Go",_=>null };
                if(unit is null)continue;
                double? v=s.Value is float value&&float.IsFinite(value)?value:null;
                result.Add(new("hw"+s.Identifier,h.Name,s.Name,unit,v,"LibreHardwareMonitor",v.HasValue?MetricState.Available:MetricState.Missing));
            }
            foreach(var sub in h.SubHardware)ReadHardware(sub,result);
        } catch(Exception e) { Warning="Collecte matérielle partielle : "+e.Message; }
    }
    public void Dispose()=>computer.Close();
}
