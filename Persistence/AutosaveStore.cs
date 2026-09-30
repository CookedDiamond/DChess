using System;
using System.IO;
using System.Text.Json;

namespace DChess.Persistence {
    /// <summary>Atomic replacement and a previous-save backup protect a game from interrupted writes.</summary>
    public sealed class AutosaveStore {
        private readonly object _lock = new();
        public string Path { get; }
        public string LastError { get; private set; }
        public bool Exists => File.Exists(Path) || File.Exists(Path + ".bak");
        public static string DefaultPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DChess", "autosave.json");

        public AutosaveStore(string path = null) { Path = path ?? DefaultPath; }

        public bool Save(SessionState session, Func<bool> isCurrent = null) {
            lock (_lock) {
                try {
                    if (isCurrent != null && !isCurrent()) return false;
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path)));
                    string temporary = Path + ".tmp";
                    using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        JsonSerializer.Serialize(stream, session);
                        stream.Flush(true);
                    }
                    if (File.Exists(Path)) File.Replace(temporary, Path, Path + ".bak");
                    else File.Move(temporary, Path);
                    LastError = null;
                    return true;
                } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException || ex is NotSupportedException) {
                    LastError = "Autosave failed: " + ex.Message;
                    return false;
                }
            }
        }

        public SessionState Load() {
            lock (_lock) {
                foreach (string path in new[] { Path, Path + ".bak" }) {
                    if (!File.Exists(path)) continue;
                    try {
                        var session = JsonSerializer.Deserialize<SessionState>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty autosave.");
                        session.Validate();
                        LastError = path == Path ? null : "Recovered the previous autosave.";
                        return session;
                    } catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is JsonException || ex is ArgumentException || ex is NullReferenceException || ex is NotSupportedException) {
                        LastError = "Could not resume autosave: " + ex.Message;
                    }
                }
                return null;
            }
        }
    }
}
