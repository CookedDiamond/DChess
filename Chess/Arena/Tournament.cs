using DChess.BotApi;
using DChess.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.Chess.Arena;

public sealed class TournamentSettings {
    public List<string> Bots { get; set; } = new();
    public int GamesPerPairing { get; set; } = 2;
    public int TimeLimitMilliseconds { get; set; } = 1000;
    public int MaxPlies { get; set; } = 500;
    public int Seed { get; set; } = Random.Shared.Next();
    public GameConfiguration Configuration { get; set; } = new();
    public BoardState InitialPosition { get; set; }

    public void Validate() {
        if (Bots.Count < 2 || (Bots.Count & (Bots.Count - 1)) != 0) throw new ArgumentException("Choose a power of two entrants (2, 4, 8, ...).");
        foreach (string name in Bots) {
            var bot = BotRegistry.Find(name);
            if (bot == null || bot.IsHuman) throw new ArgumentException($"Bot '{name}' is unavailable.");
        }
        if (GamesPerPairing < 2 || GamesPerPairing % 2 != 0 || TimeLimitMilliseconds <= 0 || MaxPlies <= 0)
            throw new ArgumentException("Choose an even game count of at least two and positive time/move limits.");
        if (InitialPosition != null) {
            var board = InitialPosition.Restore();
            if (board.GetMoveCount() != 0) throw new ArgumentException("The tournament starting position must have no move history.");
        } else Configuration.CreateBoard();
    }
    public TournamentSettings Copy() => new() { Bots = Bots.ToList(), GamesPerPairing = GamesPerPairing,
        TimeLimitMilliseconds = TimeLimitMilliseconds, MaxPlies = MaxPlies,
        Seed = Seed, Configuration = Configuration.Copy(), InitialPosition = InitialPosition };
}

public enum TournamentStatus { Running, Paused, Completed, Failed }

public sealed class PairingStatistics {
    public int WinsA { get; set; }
    public int WinsB { get; set; }
    public int Draws { get; set; }
    public int Games { get; set; }
    public int WhiteWins { get; set; }
    public int BlackWins { get; set; }
    public double AveragePlies { get; set; }
    public double AverageThinkA { get; set; }
    public double AverageThinkB { get; set; }
    public Dictionary<string, int> EndReasons { get; set; } = new();
    public double ScoreA => WinsA + Draws * .5;
    public double ScoreB => WinsB + Draws * .5;

    public static PairingStatistics Calculate(MatchState match) {
        var stats = new PairingStatistics();
        long[] time = new long[2]; int[] moves = new int[2]; int plies = 0;
        foreach (var game in match.Games.Where(g => g.Result is GameResult.WhiteWins or GameResult.BlackWins or GameResult.Draw)) {
            stats.Games++;
            if (game.Result == GameResult.Draw) stats.Draws++;
            else {
                bool whiteWins = game.Result == GameResult.WhiteWins;
                if (whiteWins) stats.WhiteWins++; else stats.BlackWins++;
                int winner = whiteWins ? game.WhitePlayerIndex : 1 - game.WhitePlayerIndex;
                if (winner == 0) stats.WinsA++; else stats.WinsB++;
            }
            string reason = game.ResultReason ?? "Unknown";
            stats.EndReasons[reason] = stats.EndReasons.GetValueOrDefault(reason) + 1;
            plies += game.Positions.Count - 1 - game.OpeningPlies;
            for (int i = game.OpeningPlies + 1; i < game.Positions.Count; i++) {
                int player = i % 2 == 1 ? game.WhitePlayerIndex : 1 - game.WhitePlayerIndex;
                time[player] += game.Positions[i].ThinkMilliseconds; moves[player]++;
            }
        }
        stats.AveragePlies = stats.Games == 0 ? 0 : (double)plies / stats.Games;
        stats.AverageThinkA = moves[0] == 0 ? 0 : (double)time[0] / moves[0];
        stats.AverageThinkB = moves[1] == 0 ? 0 : (double)time[1] / moves[1];
        return stats;
    }
}

public sealed class TournamentPairing {
    public int Number { get; set; }
    public int Round { get; set; }
    public int EntrantA { get; set; }
    public int EntrantB { get; set; }
    public int? Winner { get; set; }
    public string Decision { get; set; }
    public MatchState Match { get; set; }
    public PairingStatistics Statistics { get; set; } = new();
    // Completed match states and statistics are replaced, never mutated, by the runner.
    public TournamentPairing Copy() => new() { Number = Number, Round = Round, EntrantA = EntrantA, EntrantB = EntrantB,
        Winner = Winner, Decision = Decision, Match = Match, Statistics = Statistics };
}

public sealed class TournamentState {
    public int Version { get; set; } = 1;
    public long Revision { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset Started { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? Finished { get; set; }
    public TournamentSettings Settings { get; set; }
    public TournamentStatus Status { get; set; } = TournamentStatus.Running;
    public string Error { get; set; }
    public int? Champion { get; set; }
    public List<TournamentPairing> Pairings { get; set; } = new();
    public TournamentState Copy() => new() { Version = Version, Revision = Revision, Id = Id, Started = Started, Finished = Finished,
        Settings = Settings, Status = Status, Error = Error, Champion = Champion, Pairings = Pairings.Select(p => p.Copy()).ToList() };
    public string EntrantName(int index) => $"{Settings.Bots[index]} [#{index + 1}]";
    public string RoundName(int round) {
        int remaining = Settings.Bots.Count >> round;
        return remaining switch { 2 => "Final", 4 => "Semi-finals", 8 => "Quarter-finals", _ => $"Round of {remaining}" };
    }
}

/// <summary>Sequential knockout runner. No Stockfish and no UI work occurs on this worker.</summary>
public sealed class TournamentRunner {
    private readonly object _lock = new();
    private readonly TournamentState _state;
    private readonly TournamentStore _store;
    private readonly CancellationTokenSource _cancel = new();
    private Match _activeMatch;
    private TournamentPairing _activePairing;
    private int _started;
    private readonly object _saveLock = new();
    private TournamentState _pendingSave;
    private bool _saveRunning;
    private Task _saveTask = Task.CompletedTask;
    public Match ActiveMatch { get { lock (_lock) return _activeMatch; } }
    public int? ActivePairingNumber { get { lock (_lock) return _activePairing?.Number; } }
    public Match MatchForPairing(int number) { lock (_lock) return _activePairing?.Number == number ? _activeMatch : null; }

    public TournamentRunner(TournamentSettings settings, TournamentStore store = null, TournamentState resume = null) {
        settings.Validate();
        _store = store;
        _state = resume?.Copy() ?? new TournamentState { Settings = settings.Copy() };
        _state.Settings = settings.Copy();
        _state.Settings.InitialPosition ??= BoardState.Capture(_state.Settings.Configuration.CreateBoard());
        _state.Revision = Math.Max(_state.Revision, store?.LatestRevision(_state.Id) ?? 0);
        if (_state.Status == TournamentStatus.Completed) throw new ArgumentException("This tournament is already complete.");
        _state.Status = TournamentStatus.Running;
        _state.Error = null;
        if (_state.Pairings.Count == 0) AddRound(Enumerable.Range(0, settings.Bots.Count).ToList(), 0);
    }
    public TournamentState View() { lock (_lock) return _state.Copy(); }
    public Task Start() {
        if (Interlocked.Exchange(ref _started, 1) != 0) throw new InvalidOperationException("Tournament already started.");
        return Task.Factory.StartNew(Run, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    public void Pause() {
        lock (_lock) {
            if (_state.Status != TournamentStatus.Running) return;
            _cancel.Cancel();
            if (_activeMatch != null && _activePairing != null) {
                _activePairing.Match = _activeMatch.CaptureState();
                _activePairing.Statistics = PairingStatistics.Calculate(_activePairing.Match);
            }
            _state.Status = TournamentStatus.Paused;
            Save();
            _activeMatch?.Cancel();
        }
    }
    private void Save() {
        _state.Revision++;
        if (_store == null) return;
        lock (_saveLock) {
            _pendingSave = _state.Copy();
            if (_saveRunning) return;
            _saveRunning = true;
            _saveTask = Task.Factory.StartNew(() => {
                Thread.CurrentThread.Name = "Tournament history";
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                while (true) {
                    TournamentState snapshot;
                    lock (_saveLock) {
                        snapshot = _pendingSave; _pendingSave = null;
                        if (snapshot == null) { _saveRunning = false; return; }
                    }
                    if (!_store.Save(snapshot)) lock (_lock) _state.Error = _store.LastError;
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
    public void Flush() { Task saving; lock (_saveLock) saving = _saveTask; saving.GetAwaiter().GetResult(); }
    private void AddRound(List<int> entrants, int round) {
        for (int i = 0; i < entrants.Count; i += 2) _state.Pairings.Add(new TournamentPairing {
            Number = _state.Pairings.Count + 1, Round = round, EntrantA = entrants[i], EntrantB = entrants[i + 1]
        });
    }
    public void Run() {
        try {
            lock (_lock) Save();
            while (!_cancel.IsCancellationRequested) {
                TournamentPairing pairing;
                lock (_lock) {
                    pairing = _state.Pairings.FirstOrDefault(p => p.Winner == null);
                    if (pairing == null) {
                        int lastRound = _state.Pairings.Max(p => p.Round);
                        var winners = _state.Pairings.Where(p => p.Round == lastRound).Select(p => p.Winner.Value).ToList();
                        if (winners.Count == 1) {
                            _state.Champion = winners[0]; _state.Status = TournamentStatus.Completed;
                            _state.Finished = DateTimeOffset.UtcNow; Save(); break;
                        }
                        AddRound(winners, lastRound + 1); pairing = _state.Pairings[^(winners.Count / 2)];
                    }
                    _activePairing = pairing;
                    _activeMatch = null;
                }
                PlayPairing(pairing);
            }
        } catch (Exception ex) {
            lock (_lock) {
                if (!_cancel.IsCancellationRequested) { _state.Status = TournamentStatus.Failed; _state.Error = ex.Message; Save(); }
            }
        } finally { lock (_lock) { _activeMatch = null; _activePairing = null; } }
    }
    private void PlayPairing(TournamentPairing pairing) {
        int target = Math.Max(_state.Settings.GamesPerPairing, pairing.Match?.GameCount ?? 0);
        while (!_cancel.IsCancellationRequested) {
            var settings = new MatchSettings {
                Player1 = BotRegistry.Find(_state.Settings.Bots[pairing.EntrantA]), Player2 = BotRegistry.Find(_state.Settings.Bots[pairing.EntrantB]),
                Games = target, TimeLimitMilliseconds = TiebreakTime(_state.Settings.TimeLimitMilliseconds,
                    (target - _state.Settings.GamesPerPairing) / 2), MaxPlies = _state.Settings.MaxPlies,
                AlternateColors = true, Configuration = _state.Settings.Configuration.Copy(), UsePairedOpenings = true,
                OpeningSeed = _state.Settings.Seed, InitialPosition = _state.Settings.InitialPosition
            };
            var match = new Match(settings, pairing.Match);
            lock (_lock) { if (_cancel.IsCancellationRequested) return; _activeMatch = match; }
            match.GameFinished += _ => {
                lock (_lock) {
                    if (_cancel.IsCancellationRequested) return;
                    pairing.Match = match.CaptureState(); pairing.Statistics = PairingStatistics.Calculate(pairing.Match); Save();
                }
            };
            match.Run();
            lock (_lock) {
                if (_cancel.IsCancellationRequested) return;
                if (match.Error != null) throw new InvalidOperationException(match.Error);
                pairing.Match = match.CaptureState(); pairing.Statistics = PairingStatistics.Calculate(pairing.Match);
                if (pairing.Statistics.Games != target) throw new InvalidOperationException("Pairing ended before all games were completed.");
                if (pairing.Statistics.ScoreA != pairing.Statistics.ScoreB) {
                    pairing.Winner = pairing.Statistics.ScoreA > pairing.Statistics.ScoreB ? pairing.EntrantA : pairing.EntrantB;
                    pairing.Decision = target > _state.Settings.GamesPerPairing ? "Won after color-paired tiebreak games" : "Won on points";
                } else { target += 2; Save(); continue; }
                Save(); return;
            }
        }
    }
    public static int TiebreakTime(int initial, int pair) => Math.Max(1, (int)(initial * Math.Pow(.8, pair)));
}
