using Aegis.Domain;
namespace Aegis.Application;
public interface ISessionStore
{
    Task SaveAsync(Session session, CancellationToken token = default);
    Task<IReadOnlyList<Session>> ListAsync(CancellationToken token = default);
    Task<Session?> LoadAsync(Guid id, CancellationToken token = default);
}
public sealed record AssistantReply(string Text, string Provider, bool Generated);
public interface ISessionAssistant
{
    Task<AssistantReply> AskAsync(string question, Session? session, Snapshot? current, CancellationToken token);
}
public sealed class SessionRecorder
{
    private readonly List<Snapshot> samples = [];
    private readonly List<Marker> markers = [];
    private Guid id;
    private DateTimeOffset started;
    private string name = "", conditions = "";
    public bool Active { get; private set; }
    public bool LimitReached { get; private set; }
    public int Count => samples.Count;
    public void Start(string title, string context)
    {
        if (Active) throw new InvalidOperationException("Une session est déjà en cours.");
        samples.Clear(); markers.Clear(); id = Guid.NewGuid(); started = DateTimeOffset.UtcNow;
        name = string.IsNullOrWhiteSpace(title) ? "Session" : title.Trim(); conditions = context.Trim(); Active = true; LimitReached = false;
    }
    public void Add(Snapshot s)
    {
        if (!Active) return;
        if (samples.Count >= 28800) { LimitReached = true; Active = false; return; }
        samples.Add(s);
    }
    public void Mark(string text) { if (Active && samples.Count > 0) markers.Add(new(samples[^1].MonotonicSeconds - samples[0].MonotonicSeconds, text)); }
    public Session Current() => new(id,name,started,Active ? null : DateTimeOffset.UtcNow,conditions,samples.ToArray(),markers.ToArray());
    public Session Stop() { if (!Active && !LimitReached) throw new InvalidOperationException("Aucune session en cours."); Active = false; return Current(); }
}
