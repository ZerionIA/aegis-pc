namespace Aegis.Domain;

public enum MetricState { Available, Missing, Error, Stale, Warming }
public sealed record Metric(string Id, string Device, string Name, string Unit, double? Value, string Source, MetricState State);
public sealed record Snapshot(int Protocol, DateTimeOffset Utc, double MonotonicSeconds, IReadOnlyList<Metric> Metrics, string? Warning = null);
public sealed record Marker(double Seconds, string Label);
public sealed record Session(Guid Id, string Name, DateTimeOffset Started, DateTimeOffset? Ended, string Conditions, IReadOnlyList<Snapshot> Samples, IReadOnlyList<Marker> Markers, int Schema = 1, FrameCapture? Frames = null);
public sealed record FrameCapture(string Provider, string Process, string SwapChain, FrameStats? Stats, string Status, string? RawFile);
public sealed record Finding(string Id, string Title, string Detail, string NextCheck);
public sealed record SessionSummary(int SampleCount, double DurationSeconds, double? CpuAverage, double? CpuPeak, double? MemoryPeak, double? GpuPeak, double? TemperaturePeak, int MissingValues, IReadOnlyList<Finding> Findings);

public static class Metrics
{
    public static bool IsTemperatureReading(Metric metric)=>metric.Unit=="°C"&&!metric.Name.Contains("Distance",StringComparison.OrdinalIgnoreCase)&&!metric.Name.Contains("TjMax",StringComparison.OrdinalIgnoreCase);
    public static double? Latest(Snapshot? s, string id) => s?.Metrics.FirstOrDefault(x => x.Id == id && x.State == MetricState.Available)?.Value;
    public static double? Rate(long current, long previous, double elapsed) => elapsed > 0 && current >= previous ? (current - previous) / elapsed : null;
    public static double? CpuPercent(ulong idleDelta, ulong kernelDelta, ulong userDelta)
    {
        double total = (double)kernelDelta + userDelta;
        return total > 0 && idleDelta <= total ? Math.Clamp((total - idleDelta) / total * 100, 0, 100) : null;
    }
}

public static class SessionAnalysis
{
    public static SessionSummary Summarize(Session session)
    {
        double[] Values(string id) => session.Samples.Select(s => Metrics.Latest(s, id)).Where(v => v.HasValue && double.IsFinite(v.Value)).Select(v => v!.Value).ToArray();
        double? Max(double[] v) => v.Length == 0 ? null : v.Max();
        var cpu = Values("cpu.load"); var ram = Values("ram.load");
        var gpu = session.Samples.SelectMany(s => s.Metrics).Where(m => m.Id.Contains("gpu") && m.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) && m.Unit == "%" && m.State == MetricState.Available && m.Value.HasValue).Select(m => m.Value!.Value).ToArray();
        var temperatures = session.Samples.SelectMany(s => s.Metrics).Where(m => Metrics.IsTemperatureReading(m) && m.State == MetricState.Available && m.Value.HasValue).Select(m => m.Value!.Value).ToArray();
        var missing = session.Samples.Sum(s => s.Metrics.Count(m => m.State != MetricState.Available));
        var findings = new List<Finding>();
        if(session.Frames?.Stats is null) findings.Add(new("no-frames", "Fluidité non mesurée", "Cette session ne contient aucune capture de présentations exploitable. Les FPS et les saccades ne peuvent pas être évalués à partir des seules moyennes CPU/RAM.", "Sélectionner le processus du jeu dans Sessions et réaliser une capture PresentMon."));
        if (cpu.Any(v => v >= 95)) findings.Add(new("cpu-high", "Forte charge CPU observée", "La charge globale atteint 95 % ou plus sur au moins un échantillon. Cela ne prouve pas une limitation du jeu.", "Consulter les programmes actifs et refaire une capture dans les mêmes conditions."));
        if (ram.Any(v => v >= 90)) findings.Add(new("ram-high", "Mémoire fortement occupée", "La mémoire physique utilisée atteint 90 % ou plus. La pagination n’est pas mesurée ; aucune cause de saccade n’est démontrée.", "Comparer une session après fermeture des applications inutilisées."));
        if (temperatures.Any(v => v >= 85)) findings.Add(new("temp-high", "Température à examiner", "Un capteur atteint au moins 85 °C. Ce seuil de consultation ne constitue pas une limite constructeur ni une preuve de throttling.", "Vérifier le composant et sa limite officielle, puis consulter l’évolution."));
        if (missing > 0) findings.Add(new("missing", "Mesures indisponibles", "Certaines mesures sont absentes, en démarrage ou en erreur. Elles ne sont pas comptées comme zéro.", "Consulter la liste des capteurs et les informations de collecte."));
        if (findings.Count == 0) findings.Add(new("no-rule", "Aucun seuil de consultation atteint", "Les règles actuelles n’ont relevé aucun dépassement sur les données disponibles. Cela ne certifie pas la fluidité ni la santé du PC.", "Examiner les marqueurs et les données détaillées si un problème a été ressenti."));
        var duration = session.Samples.Count >= 2 ? session.Samples[^1].MonotonicSeconds - session.Samples[0].MonotonicSeconds : 0;
        return new(session.Samples.Count, Math.Max(0,duration), cpu.Length == 0 ? null : cpu.Average(), Max(cpu), Max(ram), Max(gpu), Max(temperatures), missing, findings);
    }
}

public sealed record FrameStats(int Frames, double AverageFps, double P99Milliseconds, double OnePercentLowFps);
public static class FrameAnalysis
{
    // 1% low = inverse of the mean duration of the slowest ceil(N*0.01) presented frames.
    public static FrameStats? Calculate(IEnumerable<double> times)
    {
        var v = times.Where(t => double.IsFinite(t) && t > 0).Order().ToArray();
        if (v.Length == 0) return null;
        int slow = Math.Max(1, (int)Math.Ceiling(v.Length * .01));
        return new(v.Length, 1000 / v.Average(), v[Math.Clamp((int)Math.Ceiling(v.Length * .99) - 1,0,v.Length-1)], 1000 / v.TakeLast(slow).Average());
    }
}
