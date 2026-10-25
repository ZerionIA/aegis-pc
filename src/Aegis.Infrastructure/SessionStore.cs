using System.Text.Json;
using Aegis.Application;
using Aegis.Domain;
using Microsoft.Data.Sqlite;
namespace Aegis.Infrastructure;
public sealed class SessionStore : ISessionStore
{
    private readonly string connectionString;
    private readonly string databasePath;
    /// <summary>Chemin d'une base illisible mise de côté au démarrage (null si la base était saine).</summary>
    public string? QuarantinedDatabase { get; private set; }
    public SessionStore(string directory)
    {
        Directory.CreateDirectory(directory);
        databasePath = Path.Combine(directory, "sessions.db");
        connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Pooling = false }.ToString();
        try { Initialize(); }
        catch (SqliteException e) when (e.SqliteErrorCode is 11 or 26)
        {
            // Base corrompue : on la conserve à part pour ne rien détruire, puis on repart d'une base neuve.
            SqliteConnection.ClearAllPools();
            QuarantinedDatabase = databasePath + ".corrupt-" + Guid.NewGuid().ToString("N");
            var moved = new List<string>();
            try
            {
                foreach (var suffix in new[] { "", "-wal", "-shm" })
                    if (File.Exists(databasePath + suffix)) { File.Move(databasePath + suffix, QuarantinedDatabase + suffix); moved.Add(suffix); }
            }
            catch
            {
                foreach (var suffix in moved.AsEnumerable().Reverse()) File.Move(QuarantinedDatabase + suffix, databasePath + suffix);
                throw;
            }
            Initialize();
        }
    }
    private void Initialize()
    {
        using var c = Open(); using var command = c.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE IF NOT EXISTS sessions(id TEXT PRIMARY KEY, name TEXT NOT NULL, started TEXT NOT NULL, data TEXT NOT NULL); PRAGMA user_version=1; SELECT count(*) FROM sessions;";
        command.ExecuteNonQuery();
    }
    private SqliteConnection Open() { var c = new SqliteConnection(connectionString); c.Open(); using var pragma = c.CreateCommand(); pragma.CommandText = "PRAGMA busy_timeout=5000"; pragma.ExecuteNonQuery(); return c; }
    public async Task SaveAsync(Session s, CancellationToken token = default)
    {
        if (s.Samples.Count > 28800 || s.Schema != 1) throw new InvalidDataException("Session hors limites.");
        // La sérialisation d'une longue session est coûteuse : hors du thread d'interface.
        string json = await Task.Run(() => JsonSerializer.Serialize(s), token);
        await using var c = Open(); await using var command = c.CreateCommand();
        command.CommandText = "INSERT INTO sessions(id,name,started,data) VALUES($id,$name,$started,$data) ON CONFLICT(id) DO UPDATE SET name=excluded.name, data=excluded.data";
        command.Parameters.AddWithValue("$id", s.Id.ToString()); command.Parameters.AddWithValue("$name", s.Name); command.Parameters.AddWithValue("$started", s.Started.ToString("O")); command.Parameters.AddWithValue("$data", json);
        await command.ExecuteNonQueryAsync(token);
    }
    public async Task<IReadOnlyList<Session>> ListAsync(CancellationToken token = default)
    {
        await using var c = Open(); await using var command = c.CreateCommand(); command.CommandText = "SELECT id,name,started,json_extract(data,'$.Ended') FROM sessions ORDER BY started DESC LIMIT 200";
        await using var r = await command.ExecuteReaderAsync(token); var list = new List<Session>();
        while (await r.ReadAsync(token))
            if (Guid.TryParse(r.GetString(0), out var id) && DateTimeOffset.TryParse(r.GetString(2), out var started))
                list.Add(new(id, r.GetString(1), started, !r.IsDBNull(3)&&DateTimeOffset.TryParse(r.GetString(3),out var ended)?ended:null, "", [], []));
        return list;
    }
    public async Task<Session?> LoadAsync(Guid id, CancellationToken token = default)
    {
        await using var c = Open(); await using var command = c.CreateCommand(); command.CommandText = "SELECT data FROM sessions WHERE id=$id"; command.Parameters.AddWithValue("$id", id.ToString());
        var raw = await command.ExecuteScalarAsync(token) as string;
        Session? s;
        try { s = raw is null ? null : JsonSerializer.Deserialize<Session>(raw); }
        catch (JsonException e) { throw new InvalidDataException("Cette session est illisible (données corrompues) : " + e.Message, e); }
        if (s is not null && s.Schema != 1) throw new InvalidDataException("Version de session non prise en charge.");
        return s;
    }
}
