using System.Text.Json;
using Aegis.Domain;
namespace Aegis.Infrastructure;

public static class SupportReport
{
    public static string Create(string version,Snapshot? snapshot)=>JsonSerializer.Serialize(new
    {
        app="Aegis",version,createdUtc=DateTimeOffset.UtcNow,
        osVersion=Environment.OSVersion.Version.ToString(),architecture=System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
        runtime=Environment.Version.ToString(),
        sensorCount=snapshot?.Metrics.Count??0,
        available=snapshot?.Metrics.Count(m=>m.State==MetricState.Available)??0,
        unavailable=snapshot?.Metrics.Count(m=>m.State!=MetricState.Available)??0,
        sources=snapshot?.Metrics.Select(m=>m.Source is "Windows API" or "LibreHardwareMonitor"?m.Source:"Autre source").Distinct().Order().ToArray()??[],
        states=snapshot?.Metrics.GroupBy(m=>m.State.ToString()).ToDictionary(g=>g.Key,g=>g.Count())??new(),
        note="Rapport local sans nom de session, adresse e-mail, chemin, nom de machine, identifiant matériel ni valeur de capteur. Relisez-le avant partage."
    },new JsonSerializerOptions{WriteIndented=true});
}
