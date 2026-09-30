using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.UI.Analysis;

internal sealed record EvaluationScore(int Value, bool IsMate, bool WhiteWinsMateZero = false) {
    private bool WhiteWinning => Value > 0 || (Value == 0 && WhiteWinsMateZero);
    internal double WhiteFraction => IsMate ? (WhiteWinning ? 1 : 0) : 1 / (1 + Math.Exp(-Value / 250.0));
    internal string Label => IsMate ? (WhiteWinning ? "+" : "-") + "M" + Math.Abs(Value)
        : (Value / 100.0).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
    internal static EvaluationScore Parse(string line, bool whiteToMove) {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int index = Array.IndexOf(parts, "score");
        if (index < 0 || index + 2 >= parts.Length || line.Contains("bound") ||
            !int.TryParse(parts[index + 2], out int value) || (parts[index + 1] != "cp" && parts[index + 1] != "mate")) return null;
        return new(whiteToMove ? value : -value, parts[index + 1] == "mate", !whiteToMove);
    }
}

// Owned by a scene; no global score, no reference from Board/BotBoard or saves.
internal sealed class StockfishEvaluation : IDisposable {
    private CancellationTokenSource _request;
    private Task<EvaluationScore> _pending;
    private string _key;
    internal EvaluationScore Score { get; private set; }
    internal string Status { get; private set; } = "Stockfish: waiting";

    internal void Update(AnalysisPosition position) {
        string key = position.Fen ?? position.Unavailable;
        if (_key != key) {
            _request?.Cancel();
            _request?.Dispose();
            _request = null;
            _key = key;
            Score = null;
            _pending = null;
            if (position.Fen == null) { Status = position.Unavailable; return; }
            if (!File.Exists(EnginePath)) { Status = "Stockfish missing: run installer"; return; }
            Status = "Stockfish: calculating...";
            _request = new CancellationTokenSource();
            var token = _request.Token;
            _pending = Task.Run(() => EvaluateAsync(position.Fen, token));
            _ = _pending.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        if (_pending?.IsCompleted == true) {
            if (_pending.IsCompletedSuccessfully) {
                Score = _pending.Result;
                Status = Score == null ? "No Stockfish score" : "Stockfish: " + Score.Label + " (standard chess)";
            } else {
                _ = _pending.Exception; // Observe failures; never let an engine failure stop the game.
                Status = "Stockfish unavailable";
            }
            _pending = null;
        }
    }

    internal static string EnginePath => Path.Combine(AppContext.BaseDirectory, "Engines", "Stockfish", "stockfish.exe");

    internal static async Task<EvaluationScore> EvaluateAsync(string fen, CancellationToken token) {
        // Debounce rapid replay/instant-bot changes before loading the engine.
        await Task.Delay(120, token).ConfigureAwait(false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var process = new Process { StartInfo = new ProcessStartInfo(EnginePath) {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(EnginePath)
        } };
        process.Start();
        var stderr = process.StandardError.ReadToEndAsync();
        try {
            async Task Send(string command) => await process.StandardInput.WriteLineAsync(command).ConfigureAwait(false);
            async Task<string> Read() => await process.StandardOutput.ReadLineAsync(timeout.Token).ConfigureAwait(false)
                ?? throw new IOException("Stockfish exited unexpectedly.");
            await Send("uci");
            while (await Read() != "uciok") { }
            await Send("setoption name Threads value 1");
            await Send("setoption name Hash value 16");
            await Send("isready");
            while (await Read() != "readyok") { }
            await Send("position fen " + fen);
            await Send("go movetime 350");
            EvaluationScore score = null;
            while (true) {
                string line = await Read();
                if (line.StartsWith("bestmove ")) break;
                score = EvaluationScore.Parse(line, fen.Split(' ')[1] == "w") ?? score;
            }
            return score;
        } finally {
            // Cancellation, timeout, close and errors all terminate only our own engine.
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().ConfigureAwait(false);
            await stderr.ConfigureAwait(false);
        }
    }

    public void Dispose() { _request?.Cancel(); _request?.Dispose(); _request = null; }
}
