using System.Globalization;
using System.Net;
using System.Text;
using Aegis.Domain;
namespace Aegis.Infrastructure;
public static class Reports
{
    public static string Csv(Session s)
    {
        var b=new StringBuilder("utc,elapsed_seconds,device,metric,unit,value,state,source\r\n");
        double start=s.Samples.FirstOrDefault()?.MonotonicSeconds ?? 0;
        string Cell(string text) => "\"" + (text.Length>0 && "=+-@\t\r".Contains(text[0]) ? "'"+text : text).Replace("\"","\"\"") + "\"";
        foreach(var sample in s.Samples) foreach(var m in sample.Metrics)
            b.AppendLine(string.Join(',',Cell(sample.Utc.ToString("O")),(sample.MonotonicSeconds-start).ToString("F3",CultureInfo.InvariantCulture),Cell(m.Device),Cell(m.Name),Cell(m.Unit),m.Value?.ToString("G",CultureInfo.InvariantCulture) ?? "",Cell(m.State.ToString()),Cell(m.Source)));
        return b.ToString();
    }
    public static string Html(Session s)
    {
        var a=SessionAnalysis.Summarize(s); string E(string t)=>WebUtility.HtmlEncode(t);
        var b=new StringBuilder("<!doctype html><html lang=fr><meta charset=utf-8><meta name=viewport content='width=device-width'><title>Aegis · rapport</title><style>body{font:16px system-ui;background:#0b1220;color:#e7eff9;max-width:900px;margin:50px auto;padding:24px}h1,h2{color:#53d8e9}article{padding:20px;background:#142033;border-radius:12px;margin:14px 0}small{color:#a8bed3}</style>");
        b.Append($"<h1>Aegis · {E(s.Name)}</h1><p>{E(s.Started.ToLocalTime().ToString("g"))} · {a.SampleCount} échantillons · {a.DurationSeconds:F0} secondes</p><p>{E(s.Conditions)}</p>");
        foreach(var f in a.Findings) b.Append($"<article><h2>{E(f.Title)}</h2><p>{E(f.Detail)}</p><small>À vérifier : {E(f.NextCheck)}</small></article>");
        if(s.Frames is {Stats:{} stats}capture)b.Append($"<h2>Présentations : {E(capture.Process)}</h2><p>FPS moyens {stats.AverageFps:F1} · 1% low {stats.OnePercentLowFps:F1} · p99 {stats.P99Milliseconds:F2} ms</p><p>{E(capture.Status)}</p><small>1% low : inverse de la durée moyenne du 1 % des présentations les plus lentes. Ce n’est pas une mesure de toutes les images effectivement affichées.</small>");
        b.Append("<h2>Marqueurs</h2>"); foreach(var m in s.Markers) b.Append($"<p>{m.Seconds:F1} s · {E(m.Label)}</p>");
        b.Append("<small>Analyse descriptive · aucune certification de santé matérielle.</small></html>"); return b.ToString();
    }
}
