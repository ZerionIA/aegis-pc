using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
namespace Aegis.Infrastructure;

public enum ComponentKind { File, Zip, SelectiveZip, OllamaModel }

/// <summary>Composant facultatif téléchargeable depuis l'interface. Toutes les sources et empreintes sont figées.</summary>
public sealed record ComponentDefinition(
    string Id, string Title, string Description, string Version, ComponentKind Kind,
    string Source, long DownloadBytes, string License, string? Sha256 = null,
    string? TargetFileName = null, string? DependsOn = null, string[]? ExtractOnly = null)
{
    public string SizeText => FormatSize(DownloadBytes);
    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1_000_000_000 => (bytes / 1e9).ToString("F1", System.Globalization.CultureInfo.CurrentCulture) + " Go",
        >= 1_000_000 => (bytes / 1e6).ToString("F0", System.Globalization.CultureInfo.CurrentCulture) + " Mo",
        _ => Math.Max(1, bytes / 1000) + " Ko"
    };
}

public sealed record ComponentProgress(string Phase, long Done, long Total)
{
    public double? Fraction => Total > 0 ? Math.Clamp((double)Done / Total, 0, 1) : null;
}

/// <summary>Erreur d'installation dont le message est destiné à l'utilisateur.</summary>
public sealed class ComponentException(string message, Exception? inner = null) : Exception(message, inner);

public sealed record ManifestFile(string Name, long Size, long CompressedSize, string Sha256);
public sealed record EngineManifest(string Id, string Version, string Source, long ArchiveSize, string ArchiveSha256, ManifestFile[] Files);

public static class ComponentCatalog
{
    public const string OllamaEngineId = "ollama-engine";
    public const string WhisperCliId = "whisper-cli";
    public const string WhisperModelId = "whisper-model-base";
    public const string LanguageModelId = "llm-qwen3.5-2b";
    public const string LanguageModelTag = "qwen3.5:2b";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static EngineManifest LoadEngineManifest()
    {
        using var stream = typeof(ComponentCatalog).Assembly.GetManifestResourceStream("ollama-engine.json")
            ?? throw new InvalidDataException("Manifeste du moteur IA absent de l'application.");
        return JsonSerializer.Deserialize<EngineManifest>(stream, Json) ?? throw new InvalidDataException("Manifeste du moteur IA illisible.");
    }

    public static IReadOnlyList<ComponentDefinition> All()
    {
        var engine = LoadEngineManifest();
        return
        [
            new(OllamaEngineId, "Moteur IA local (Ollama)",
                "Moteur de dialogue local, installé seulement s'il est demandé. Aegis ne télécharge que les fichiers processeur de l'archive officielle.",
                engine.Version, ComponentKind.SelectiveZip, engine.Source, engine.Files.Sum(f => f.CompressedSize), "MIT (Ollama et composants listés dans THIRD-PARTY-NOTICES.md)"),
            new(LanguageModelId, "Modèle de dialogue Qwen3.5 2B",
                "Modèle facultatif pour reformuler le bilan. Le bilan reste disponible sans lui. Intégrité vérifiée par Ollama (condensats SHA-256 du registre).",
                "2b", ComponentKind.OllamaModel, LanguageModelTag, 2_683_396_812, "Apache 2.0", DependsOn: OllamaEngineId),
            new(WhisperCliId, "Transcription vocale (whisper.cpp)",
                "Programme de transcription hors ligne, nécessaire à la commande vocale.",
                "1.8.3", ComponentKind.Zip, "https://github.com/ggml-org/whisper.cpp/releases/download/v1.8.3/whisper-bin-x64.zip", 3_968_674, "MIT",
                Sha256: "d824b1e37599f882b396e73f1ee0bfd5d0529f700314c48311dcbd00b803321d",
                ExtractOnly: ["whisper-cli.exe", "whisper.dll", "ggml.dll", "ggml-base.dll", "ggml-cpu.dll"]),
            new(WhisperModelId, "Modèle vocal Whisper base (multilingue)",
                "Modèle de reconnaissance vocale hors ligne, nécessaire à la commande vocale.",
                "ggml-base", ComponentKind.File, "https://huggingface.co/ggerganov/whisper.cpp/resolve/5359861c739e955e79d9a303bcbc70fb988958b1/ggml-base.bin",
                147_951_465, "MIT", Sha256: "60ed5bc3dd14eea856493d334349b405782ddcaf0028d4b5df4088345fba2efe", TargetFileName: "ggml-base.bin"),
        ];
    }
}

/// <summary>Dossier des composants installés (un sous-dossier par composant, marqueur d'installation complet).</summary>
public sealed class ComponentStore
{
    private const string Marker = ".aegis-component.json";
    public string Root { get; }
    public ComponentStore(string root)
    {
        Root = Path.GetFullPath(root);
        Directory.CreateDirectory(Root);
        CleanStaging();
    }
    public string StagingRoot => Path.Combine(Root, ".staging");
    public string PathOf(string id) => Path.Combine(Root, id);
    public bool IsInstalled(ComponentDefinition d) => File.Exists(Path.Combine(PathOf(d.Id), Marker))
        && ReadVersion(d.Id) == d.Version;
    public string? FindFile(string id, string fileName)
    {
        string dir = PathOf(id);
        if (!Directory.Exists(dir)) return null;
        return Directory.EnumerateFiles(dir, fileName, SearchOption.AllDirectories).FirstOrDefault();
    }
    private string? ReadVersion(string id)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(PathOf(id), Marker)));
            return doc.RootElement.GetProperty("version").GetString();
        }
        catch (Exception e) when (e is IOException or JsonException or KeyNotFoundException) { return null; }
    }
    internal void WriteMarker(string directory, ComponentDefinition d) =>
        File.WriteAllText(Path.Combine(directory, Marker), JsonSerializer.Serialize(new { id = d.Id, version = d.Version, source = d.Source, installedUtc = DateTimeOffset.UtcNow }));
    public void Remove(string id)
    {
        string dir = PathOf(id);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
    private void CleanStaging()
    {
        try { if (Directory.Exists(StagingRoot)) Directory.Delete(StagingRoot, true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}

public sealed class ComponentInstaller(HttpClient http, ComponentStore store)
{
    private const int CopyBuffer = 81920;

    public async Task InstallAsync(ComponentDefinition d, IProgress<ComponentProgress> progress, CancellationToken token)
    {
        if (d.Kind == ComponentKind.OllamaModel) throw new InvalidOperationException("Les modèles Ollama sont installés par le moteur.");
        EnsureDisk(d);
        string staging = Path.Combine(store.StagingRoot, d.Id + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            switch (d.Kind)
            {
                case ComponentKind.File: await DownloadFileAsync(d, staging, progress, token); break;
                case ComponentKind.Zip: await DownloadZipAsync(d, staging, progress, token); break;
                case ComponentKind.SelectiveZip: await Task.Run(() => ExtractSelective(d, staging, progress, token), token); break;
            }
            store.WriteMarker(staging, d);
            progress.Report(new("Finalisation", 1, 1));
            Commit(staging, store.PathOf(d.Id));
        }
        catch (OperationCanceledException) { throw; }
        catch (ComponentException) { throw; }
        catch (HttpRequestException e) { throw new ComponentException("Téléchargement impossible : vérifiez votre connexion Internet puis réessayez. (" + e.Message + ")", e); }
        catch (IOException e) { throw new ComponentException("Écriture impossible dans le dossier des composants : " + e.Message, e); }
        catch (UnauthorizedAccessException e) { throw new ComponentException("Accès refusé au dossier des composants : " + e.Message, e); }
        catch (InvalidDataException e) { throw new ComponentException("Archive invalide ou corrompue : " + e.Message, e); }
        finally { TryDelete(staging); }
    }

    private void EnsureDisk(ComponentDefinition d)
    {
        try
        {
            long need = (long)(d.DownloadBytes * 2.2) + 50_000_000;
            long free = new DriveInfo(Path.GetPathRoot(store.Root)!).AvailableFreeSpace;
            if (free < need) throw new ComponentException($"Espace disque insuffisant : {ComponentDefinition.FormatSize(need)} requis, {ComponentDefinition.FormatSize(free)} disponibles.");
        }
        catch (ArgumentException) { }
        catch (IOException) { }
    }

    private async Task DownloadFileAsync(ComponentDefinition d, string staging, IProgress<ComponentProgress> progress, CancellationToken token)
    {
        string name = d.TargetFileName ?? Path.GetFileName(new Uri(d.Source).LocalPath);
        string partial = Path.Combine(staging, name + ".partial");
        await DownloadVerifiedAsync(d, partial, progress, token);
        File.Move(partial, Path.Combine(staging, name));
    }

    private async Task DownloadZipAsync(ComponentDefinition d, string staging, IProgress<ComponentProgress> progress, CancellationToken token)
    {
        string archive = Path.Combine(staging, "archive.zip.partial");
        await DownloadVerifiedAsync(d, archive, progress, token);
        progress.Report(new("Extraction", 0, 1));
        await Task.Run(() =>
        {
            using var zip = ZipFile.OpenRead(archive);
            foreach (var entry in zip.Entries)
            {
                token.ThrowIfCancellationRequested();
                if (entry.FullName.EndsWith('/')) continue;
                string file = Path.GetFileName(entry.FullName);
                if (d.ExtractOnly is not null && !d.ExtractOnly.Contains(file, StringComparer.OrdinalIgnoreCase)) continue;
                string destination = SafeDestination(staging, file);
                entry.ExtractToFile(destination, true);
            }
        }, token);
        File.Delete(archive);
    }

    private async Task DownloadVerifiedAsync(ComponentDefinition d, string destination, IProgress<ComponentProgress> progress, CancellationToken token)
    {
        if (d.Sha256 is null) throw new InvalidOperationException("Empreinte absente du catalogue : téléchargement refusé.");
        using var response = await http.GetAsync(d.Source, HttpCompletionOption.ResponseHeadersRead, token);
        RequireHttps(response);
        if (!response.IsSuccessStatusCode) throw new ComponentException($"Le serveur a répondu {(int)response.StatusCode} {response.ReasonPhrase}. Réessayez plus tard.");
        long? declared = response.Content.Headers.ContentLength;
        if (declared is not null && declared != d.DownloadBytes) throw new ComponentException($"Taille inattendue ({declared} octets au lieu de {d.DownloadBytes}) : téléchargement refusé.");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long total = 0, lastReport = 0;
        await using (var input = await response.Content.ReadAsStreamAsync(token))
        await using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, CopyBuffer, true))
        {
            var buffer = new byte[CopyBuffer];
            int read;
            while ((read = await input.ReadAsync(buffer, token)) > 0)
            {
                total += read;
                if (total > d.DownloadBytes) throw new ComponentException("Le serveur envoie plus de données que prévu : téléchargement interrompu.");
                hash.AppendData(buffer, 0, read);
                await output.WriteAsync(buffer.AsMemory(0, read), token);
                if (total - lastReport >= 262144) { lastReport = total; progress.Report(new("Téléchargement", total, d.DownloadBytes)); }
            }
        }
        progress.Report(new("Vérification", total, d.DownloadBytes));
        string actual = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        if (total != d.DownloadBytes || actual != d.Sha256.ToLowerInvariant())
            throw new ComponentException("Le fichier téléchargé ne correspond pas à l'empreinte SHA-256 attendue. Il a été supprimé et n'a pas été installé.");
    }

    private void ExtractSelective(ComponentDefinition d, string staging, IProgress<ComponentProgress> progress, CancellationToken token)
    {
        var manifest = ComponentCatalog.LoadEngineManifest();
        long expected = manifest.Files.Sum(f => f.CompressedSize), counted = 0;
        using var remote = new HttpRangeStream(http, manifest.Source, manifest.ArchiveSize, token, n => { counted += n; progress.Report(new("Téléchargement", Math.Min(counted, expected), expected)); });
        using var zip = new ZipArchive(remote, ZipArchiveMode.Read);
        var byName = zip.Entries.ToDictionary(e => e.FullName, StringComparer.Ordinal);
        foreach (var file in manifest.Files)
        {
            token.ThrowIfCancellationRequested();
            if (!byName.TryGetValue(file.Name, out var entry) || entry.Length != file.Size)
                throw new ComponentException($"L'archive officielle ne contient pas le fichier attendu « {file.Name} » : installation refusée.");
            string destination = SafeDestination(staging, file.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using (var input = entry.Open())
            using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, CopyBuffer))
            {
                var buffer = new byte[CopyBuffer];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    token.ThrowIfCancellationRequested();
                    hash.AppendData(buffer, 0, read);
                    output.Write(buffer, 0, read);
                }
            }
            if (Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant() != file.Sha256)
                throw new ComponentException($"Empreinte SHA-256 incorrecte pour « {file.Name} » : installation refusée, rien n'a été conservé.");
        }
    }

    public static string SafeDestination(string root, string relative)
    {
        if (Path.IsPathRooted(relative) || relative.Split('/', '\\').Contains(".."))
            throw new ComponentException("Chemin dangereux dans l'archive : installation refusée.");
        string full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ComponentException("Chemin hors du dossier d'installation dans l'archive : installation refusée.");
        return full;
    }

    private static void RequireHttps(HttpResponseMessage response)
    {
        var uri = response.RequestMessage?.RequestUri;
        if (uri is not null && uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback)
            throw new ComponentException("Redirection vers une adresse non chiffrée refusée.");
    }

    private static void Commit(string staging, string target)
    {
        string old = target + ".old-" + Guid.NewGuid().ToString("N");
        bool hadOld = Directory.Exists(target);
        if (hadOld) Directory.Move(target, old);
        try { Directory.Move(staging, target); }
        catch { if (hadOld && !Directory.Exists(target)) Directory.Move(old, target); throw; }
        if (hadOld) TryDelete(old);
    }

    private static void TryDelete(string directory)
    {
        try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}

/// <summary>Flux en lecture seule sur une ressource HTTP (requêtes Range) pour ne lire que certaines entrées d'un ZIP.</summary>
internal sealed class HttpRangeStream : Stream
{
    private const int Block = 1024 * 1024;
    private readonly HttpClient http;
    private readonly string original;
    private Uri? resolved;
    private readonly CancellationToken token;
    private readonly Action<long> onBytes;
    private byte[] cache = [];
    private long cacheStart = -1, position;
    public HttpRangeStream(HttpClient http, string url, long length, CancellationToken token, Action<long> onBytes)
    { this.http = http; original = url; Length = length; this.token = token; this.onBytes = onBytes; }
    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position { get => position; set => position = value; }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin)
    {
        position = origin switch { SeekOrigin.Begin => offset, SeekOrigin.Current => position + offset, _ => Length + offset };
        if (position < 0) throw new IOException("Position négative.");
        return position;
    }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (position >= Length || count == 0) return 0;
        token.ThrowIfCancellationRequested();
        if (cacheStart < 0 || position < cacheStart || position >= cacheStart + cache.Length) Fill(position);
        int available = (int)(cacheStart + cache.Length - position);
        int n = Math.Min(count, available);
        Buffer.BlockCopy(cache, (int)(position - cacheStart), buffer, offset, n);
        position += n;
        return n;
    }
    private void Fill(long start)
    {
        long end = Math.Min(Length, start + Block) - 1;
        for (int attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, resolved ?? new Uri(original));
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
            try
            {
                using var response = http.Send(request, HttpCompletionOption.ResponseHeadersRead, token);
                if (response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                {
                    if (resolved is not null) { resolved = null; continue; }
                    throw new ComponentException("Le serveur ne prend pas en charge le téléchargement partiel (HTTP " + (int)response.StatusCode + ").");
                }
                var range = response.Content.Headers.ContentRange;
                if (range?.Length is long total && total != Length) throw new ComponentException("La taille de l'archive officielle a changé : installation refusée.");
                resolved ??= response.RequestMessage?.RequestUri;
                using var stream = response.Content.ReadAsStream(token);
                var data = new byte[end - start + 1];
                int filled = 0;
                while (filled < data.Length)
                {
                    int read = stream.Read(data, filled, data.Length - filled);
                    if (read == 0) throw new IOException("Flux interrompu.");
                    filled += read;
                }
                cache = data; cacheStart = start; onBytes(data.Length);
                return;
            }
            catch (HttpRequestException) when (attempt < 2) { Thread.Sleep(500 * (attempt + 1)); }
            catch (IOException) when (attempt < 2) { Thread.Sleep(500 * (attempt + 1)); }
        }
    }
}