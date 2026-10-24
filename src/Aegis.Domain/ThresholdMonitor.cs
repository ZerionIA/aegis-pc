namespace Aegis.Domain;

/// <summary>User-selected consultation threshold, sustained for three fresh samples.</summary>
public sealed class ThresholdMonitor
{
    private int count;
    private bool notified;
    private double? previousTime, previousThreshold;
    public bool Observe(double? value, double threshold, double seconds)
    {
        bool discontinuity = previousTime is double p && (seconds <= p || seconds - p > 3);
        if (discontinuity || previousThreshold != threshold) { count = 0; notified = false; }
        previousTime = seconds; previousThreshold = threshold;
        if (!double.IsFinite(threshold) || value is null || !double.IsFinite(value.Value)) { count = 0; notified = false; return false; }
        if (value < threshold - 3) { count = 0; notified = false; return false; }
        if (value < threshold) { count = 0; return false; }
        if (++count < 3 || notified) return false;
        notified = true;
        return true;
    }
}
