using DChess.Chess.Arena;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace DChess.Persistence;

public sealed class TournamentSummary {
    public Guid Id { get; set; }
    public DateTimeOffset Started { get; set; }
    public int Entrants { get; set; }
    public TournamentStatus Status { get; set; }
    public string Champion { get; set; }
    public string Configuration { get; set; }
    public int GamesPerPairing { get; set; }
    public int TimeLimitMilliseconds { get; set; }
    public static TournamentSummary From(TournamentState state) => new() {
        Id = state.Id, Started = state.Started, Entrants = state.Settings.Bots.Count, Status = state.Status,
        Champion = state.Champion.HasValue ? state.EntrantName(state.Champion.Value) : null,
        Configuration = state.Settings.Configuration.Description, GamesPerPairing = state.Settings.GamesPerPairing,
        TimeLimitMilliseconds = state.Settings.TimeLimitMilliseconds
    };
}

/// <summary>Each tournament has its own compressed archive, replaced atomically with a backup.</summary>
public sealed class TournamentStore {
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<Guid, long> _revisions = new();
    public string DirectoryPath { get; }
    public string LastError { get; private set; }
    public TournamentStore(string directory = null) => DirectoryPath = directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DChess", "Tournaments");
    public string ArchivePath(Guid id) => Path.Combine(DirectoryPath, id.ToString("N") + ".json.gz");
    public long LatestRevision(Guid id) => _revisions.GetValueOrDefault(id);
    public bool Save(TournamentState state) {
        lock (_lock) {
            try {
                // A previous runner may still be flushing its pause checkpoint
                // after a resumed runner starts. Never let that older write win.
                if (state.Revision < LatestRevision(state.Id)) return true;
                Directory.CreateDirectory(DirectoryPath);
                string path = ArchivePath(state.Id), temporary = path + ".tmp";
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    using (var compressed = new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: true)) JsonSerializer.Serialize(compressed, state);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                _revisions[state.Id] = state.Revision;
                string summaryPath = Path.Combine(DirectoryPath, state.Id.ToString("N") + ".summary.json");
                File.WriteAllText(summaryPath + ".tmp", JsonSerializer.Serialize(TournamentSummary.From(state)));
                if (File.Exists(summaryPath)) File.Replace(summaryPath + ".tmp", summaryPath, summaryPath + ".bak");
                else File.Move(summaryPath + ".tmp", summaryPath);
                LastError = null; return true;
            } catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException) {
                LastError = "Tournament save failed: " + e.Message; return false;
            }
        }
    }
    public TournamentState Load(Guid id) {
        lock (_lock) {
            foreach (string path in new[] { ArchivePath(id), ArchivePath(id) + ".bak" }) {
                if (!File.Exists(path)) continue;
                try {
                    using var file = File.OpenRead(path); using var compressed = new GZipStream(file, CompressionMode.Decompress);
                    var state = JsonSerializer.Deserialize<TournamentState>(compressed);
                    if (state?.Version != 1 || state.Id != id || state.Settings?.Bots?.Count < 2 ||
                        state.Pairings == null || state.Settings?.Bots == null || state.Settings.Configuration?.Variants == null ||
                        !Enum.IsDefined(state.Status) || state.Settings.GamesPerPairing < 2 || state.Settings.GamesPerPairing % 2 != 0 ||
                        state.Settings.TimeLimitMilliseconds < 1 || state.Settings.MaxPlies < 1 ||
                        (state.Status == TournamentStatus.Completed && (!state.Champion.HasValue || state.Pairings.Any(p => !p.Winner.HasValue))) ||
                        (state.Champion.HasValue && (state.Champion < 0 || state.Champion >= state.Settings.Bots.Count)) ||
                        (state.Settings.Bots.Count & (state.Settings.Bots.Count - 1)) != 0) throw new InvalidDataException("Invalid tournament archive.");
                    // Archive viewing needs neither installed bots nor registered variants.
                    if (state.Pairings.Count == 0 || state.Pairings.Any(p => p.Number <= 0 || p.Round < 0 || p.Round >= Math.Log2(state.Settings.Bots.Count) || p.Statistics == null ||
                        p.EntrantA == p.EntrantB || p.EntrantA < 0 || p.EntrantB < 0 || p.EntrantA >= state.Settings.Bots.Count ||
                        p.EntrantB >= state.Settings.Bots.Count || (p.Winner != null && p.Winner != p.EntrantA && p.Winner != p.EntrantB)))
                        throw new InvalidDataException("Invalid tournament bracket.");
                    if (state.Pairings.Select(p => p.Number).Distinct().Count() != state.Pairings.Count)
                        throw new InvalidDataException("Duplicate tournament pairings.");
                    foreach (var pairing in state.Pairings) if (pairing.Match != null)
                        foreach (var game in pairing.Match.Games) game.Restore();
                    LastError = path.EndsWith(".bak") ? "Recovered tournament backup." : null;
                    return state;
                } catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or NullReferenceException or NotSupportedException) {
                    LastError = "Could not load tournament: " + e.Message;
                }
            }
            return null;
        }
    }
    public List<TournamentSummary> ListSummaries() {
        lock (_lock) {
            if (!Directory.Exists(DirectoryPath)) return new();
            var ids = Directory.EnumerateFiles(DirectoryPath, "*.json.gz*")
                .Select(path => Path.GetFileName(path).Split('.')[0]).Distinct()
                .Select(text => Guid.TryParseExact(text, "N", out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty);
            var summaries = new List<TournamentSummary>();
            foreach (var id in ids) {
                TournamentSummary summary = null;
                string path = Path.Combine(DirectoryPath, id.ToString("N") + ".summary.json");
                try {
                    if (File.Exists(path) && (!File.Exists(ArchivePath(id)) || File.GetLastWriteTimeUtc(path) >= File.GetLastWriteTimeUtc(ArchivePath(id))))
                        summary = JsonSerializer.Deserialize<TournamentSummary>(File.ReadAllText(path));
                    if (summary != null && (summary.Id != id || summary.Entrants < 2 || !Enum.IsDefined(summary.Status))) summary = null;
                } catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
                // Existing archives without an index can still be browsed. Normal
                // history listing never deserializes their full replay data.
                if (summary == null) { var archive = Load(id); if (archive != null) summary = TournamentSummary.From(archive); }
                if (summary != null) summaries.Add(summary);
            }
            return summaries.OrderByDescending(s => s.Started).ToList();
        }
    }
}
