using Aegis.Infrastructure;
namespace Aegis.Desktop;

public sealed partial class MainViewModel
{
    private bool fullSensorAccess=true;
    public bool FullSensorAccess{get=>fullSensorAccess;set{fullSensorAccess=value;Changed();}}
    public string CpuHardwareStatus=>CpuTemperature!="—"?"Température CPU reçue du collecteur.":!HardwareAccess.IsInstalled
        ?"Prérequis PawnIO absent : l’installateur officiel est inclus dans Aegis."
        :"PawnIO installé. Les capteurs CPU complets nécessitent une confirmation administrateur Windows.";
    public AsyncCommand InstallHardwareCommand{get;private set;}=null!;
    public AsyncCommand RestartSensorsCommand{get;private set;}=null!;
    private void InitializeHardware()
    {
        RestartSensorsCommand=new(RestartSensorsAsync,Error,()=>!recorder.Active);
        InstallHardwareCommand=new(async()=>
        {
            int result=await HardwareSetup.InstallAsync();
            if(result==HardwareSetup.Cancelled){Status="Installation du prérequis annulée. Le mode basique reste disponible.";return;}
            if(result==HardwareSetup.RestartRequired){FullSensorAccess=true;SaveSettings(false);Status="PawnIO installé : Windows demande un redémarrage du PC avant les capteurs complets.";return;}
            if(result!=0)throw new InvalidOperationException("Installation PawnIO non terminée : "+result);
            FullSensorAccess=true;await RestartSensorsAsync();
        },Error,()=>!recorder.Active);
    }
    private async Task RestartSensorsAsync()
    {
        if(recorder.Active)throw new InvalidOperationException("Terminez la session avant de reconfigurer les capteurs.");
        if(!SaveSettings(false))throw new System.IO.IOException("Impossible d’enregistrer le mode de capteurs.");latest=null;SensorGroups.Clear();Cpu=Gpu=Memory=Temperature="—";
        Changed(nameof(CpuTemperature));Changed(nameof(GpuTemperature));Changed(nameof(CpuHardwareStatus));
        await collector.StartAsync(FullSensorAccess);
        Status="Collecteur relancé. La lecture complète demande une confirmation Windows ; son refus conserve le mode basique.";
    }
}
