using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Persistence;
using DChess.UI.Scenes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.Tests;

[TestClass]
[DoNotParallelize]
public class TournamentTests {
    public class LegalBot : IChessBot {
        public string Name => "Test legal";
        public virtual Move Think(BotBoard board, BotTimer timer) => board.GetLegalMoves()[0];
    }
    public class NullBot : LegalBot {
        public override Move Think(BotBoard board, BotTimer timer) => null;
    }
    public class OneMoveBot : LegalBot {
        private int _moves;
        public override Move Think(BotBoard board, BotTimer timer) => ++_moves <= 1 ? base.Think(board, timer) : null;
    }
    public class TwoMoveBot : LegalBot {
        private int _moves;
        public override Move Think(BotBoard board, BotTimer timer) => ++_moves <= 2 ? base.Think(board, timer) : null;
    }
    public class ThreeMoveBot : LegalBot {
        private int _moves;
        public override Move Think(BotBoard board, BotTimer timer) => ++_moves <= 3 ? base.Think(board, timer) : null;
    }
    public class FasterTimeBot : LegalBot {
        public override Move Think(BotBoard board, BotTimer timer) => timer.TimeLimitMilliseconds >= 1000 ? null : base.Think(board, timer);
    }
    public class MuchFasterTimeBot : LegalBot {
        public override Move Think(BotBoard board, BotTimer timer) => timer.TimeLimitMilliseconds >= 650 ? null : base.Think(board, timer);
    }
    public class BlockingBot : LegalBot {
        public static readonly ManualResetEventSlim Entered = new(false), Release = new(false);
        public override Move Think(BotBoard board, BotTimer timer) { Entered.Set(); Release.Wait(TimeSpan.FromSeconds(10)); return base.Think(board, timer); }
    }
    public class FutureVariant : Variant {
        public int After { get; }
        public FutureVariant(int after) => After = after;
        public override Variant Clone() => new FutureVariant(After);
        public override VariantOutcome GetOutcome(Board board) => board.GetMoveCount() >= After ? new(GameResult.WhiteWins, "future variant objective") : null;
    }
    public class SetupVariant : Variant {
        public static int Calls;
        public override void ConfigureInitialBoard(Board board) {
            Calls++;
            var piece = board.GetPiece(new(3, 0)); board.RemovePiece(new(3, 0));
            board.PlacePiece(new(Calls % 8, 2), piece);
        }
    }

    [ClassInitialize]
    public static void RegisterTests(TestContext _) {
        foreach (var (name, type) in new[] {
            ("T-Legal", typeof(LegalBot)), ("T-Null", typeof(NullBot)), ("T-One", typeof(OneMoveBot)),
            ("T-Two", typeof(TwoMoveBot)), ("T-Three", typeof(ThreeMoveBot)), ("T-Faster", typeof(FasterTimeBot)),
            ("T-MuchFaster", typeof(MuchFasterTimeBot)), ("T-Blocking", typeof(BlockingBot))
        }) BotRegistry.Register(new(name, type));
        VariantRegistry.Register(new("test-future", "Future objective", typeof(FutureVariant),
            s => new FutureVariant(int.Parse(s.Parameters.GetValueOrDefault("after", "2"))),
            v => new() { Kind = "test-future", Parameters = new() { ["after"] = ((FutureVariant)v).After.ToString() } }));
        VariantRegistry.Register(new("test-setup", "Changing setup", typeof(SetupVariant), _ => new SetupVariant(), _ => new() { Kind = "test-setup" }));
    }
    private static TournamentSettings Settings(params string[] bots) => new() { Bots = bots.ToList(), GamesPerPairing = 2, TimeLimitMilliseconds = 1000, MaxPlies = 50, Seed = 0 };

    [TestMethod]
    public void InvalidEntrantAndGameCountsAreRejected() {
        Assert.ThrowsException<ArgumentException>(() => Settings("T-Legal", "T-Null", "T-One").Validate());
        var settings = Settings("T-Legal", "T-Null"); settings.GamesPerPairing = 3;
        Assert.ThrowsException<ArgumentException>(settings.Validate);
        settings.GamesPerPairing = 1; Assert.ThrowsException<ArgumentException>(settings.Validate);
        settings.GamesPerPairing = 2; settings.Bots[0] = "Human"; Assert.ThrowsException<ArgumentException>(settings.Validate);
    }
    [TestMethod]
    public void EightEntrantsAdvanceThroughQuarterFinalsSemiFinalsAndFinal() {
        var runner = new TournamentRunner(Settings("T-Legal", "T-Null", "T-One", "T-Null", "T-Two", "T-Null", "T-Three", "T-Null"));
        runner.Run(); var state = runner.View();
        Assert.AreEqual(TournamentStatus.Completed, state.Status, state.Error);
        Assert.AreEqual(0, state.Champion);
        CollectionAssert.AreEqual(new[] { 4, 2, 1 }, state.Pairings.GroupBy(p => p.Round).Select(g => g.Count()).ToArray());
        Assert.AreEqual("Quarter-finals", state.RoundName(0)); Assert.AreEqual("Semi-finals", state.RoundName(1)); Assert.AreEqual("Final", state.RoundName(2));
        Assert.IsTrue(state.Pairings.All(p => p.Winner.HasValue && p.Statistics.Games == 2));
    }
    [TestMethod]
    public void OpeningPairsSwapColorsAndIncludeBookMovesInReplay() {
        var settings = Settings("T-Legal", "T-Null"); settings.GamesPerPairing = 4;
        var runner = new TournamentRunner(settings); runner.Run();
        var state = runner.View(); Assert.AreEqual(TournamentStatus.Completed, state.Status, state.Error);
        var games = state.Pairings.Single().Match.Games;
        CollectionAssert.AreEqual(new[] { 0, 1, 0, 1 }, games.Select(g => g.WhitePlayerIndex).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 0, 6, 6 }, games.Select(g => g.OpeningPlies).ToArray());
        Assert.AreEqual(games[2].OpeningName, games[3].OpeningName);
        CollectionAssert.AreEqual(games[2].Positions[6].Types, games[3].Positions[6].Types);
        CollectionAssert.AreEqual(games[2].Positions[6].Teams, games[3].Positions[6].Teams);
        Assert.AreEqual(4.0, state.Pairings[0].Statistics.ScoreA);
        Assert.AreEqual(1000, games[3].TimeLimitMilliseconds);
    }
    [TestMethod]
    public void TiedPairingUsesFasterColorPairedGamesAndRealPoints() {
        var runner = new TournamentRunner(Settings("T-Faster", "T-Null")); runner.Run();
        var pairing = runner.View().Pairings.Single();
        Assert.AreEqual(0, pairing.Winner);
        CollectionAssert.AreEqual(new[] { 1000, 1000, 800, 800 }, pairing.Match.Games.Select(g => g.TimeLimitMilliseconds).ToArray());
        Assert.AreEqual(3.0, pairing.Statistics.ScoreA); Assert.AreEqual(1.0, pairing.Statistics.ScoreB);
        Assert.AreEqual(6, pairing.Match.Games[2].OpeningPlies);
        Assert.AreEqual(640, TournamentRunner.TiebreakTime(1000, 2));
        Assert.AreEqual(1, TournamentRunner.TiebreakTime(1000, 100));
    }
    [TestMethod]
    public void EveryCurrentVariantWorksInConfiguredMatchesAndBookPairs() {
        foreach (var definition in VariantRegistry.All.Where(v => !v.Id.StartsWith("test-"))) {
            var settings = Settings("T-Legal", "T-Null"); settings.GamesPerPairing = 4;
            settings.Configuration.Variants = new() { VariantState.Capture(definition.Restore(new() { Kind = definition.Id })) };
            var runner = new TournamentRunner(settings); runner.Run(); var state = runner.View();
            Assert.AreEqual(TournamentStatus.Completed, state.Status, definition.Id + ": " + state.Error);
            Assert.AreEqual(definition.Id, state.Pairings[0].Match.Board.Variants.Single().Kind);
            Assert.AreEqual(6, state.Pairings[0].Match.Games[2].OpeningPlies);
        }
    }
    [TestMethod]
    public void FutureVariantUsesSharedRegistrationPersistenceAndOutcomeHooks() {
        var configuration = new GameConfiguration { Variants = new() { new() { Kind = "test-future", Parameters = new() { ["after"] = "2" } } } };
        var restored = BoardState.Capture(configuration.CreateBoard()).Restore();
        Assert.AreEqual(2, ((FutureVariant)restored.Variants.Single()).After);
        var match = new Match(new MatchSettings { Player1 = BotRegistry.Find("T-Legal"), Player2 = BotRegistry.Find("T-Legal"), Games = 1, Configuration = configuration });
        match.Run(); Assert.IsNull(match.Error);
        Assert.AreEqual(GameResult.WhiteWins, match.Games[0].Result);
        Assert.AreEqual("future variant objective", match.Games[0].ResultReason);
    }
    [TestMethod]
    public void SetupIsFrozenSoFutureVariantsCannotChangeTheSecondColorConfiguration() {
        var settings = Settings("T-Legal", "T-Null");
        settings.Configuration.Variants = new() { new() { Kind = "test-setup" } };
        var runner = new TournamentRunner(settings); int callsBeforePlay = SetupVariant.Calls;
        runner.Run(); var state = runner.View();
        Assert.AreEqual(TournamentStatus.Completed, state.Status, state.Error);
        Assert.AreEqual(callsBeforePlay, SetupVariant.Calls, "Each game must restore the fixed template rather than rerun setup.");
        var games = state.Pairings[0].Match.Games;
        CollectionAssert.AreEqual(games[0].Positions[0].Types, games[1].Positions[0].Types);
        CollectionAssert.AreEqual(games[0].Positions[0].Teams, games[1].Positions[0].Teams);
    }
    [TestMethod]
    public void RepeatedTiesKeepReducingTheClockAndKeepRealResults() {
        var runner = new TournamentRunner(Settings("T-MuchFaster", "T-Null")); runner.Run();
        var state = runner.View(); Assert.AreEqual(TournamentStatus.Completed, state.Status, state.Error);
        var pairing = state.Pairings[0]; Assert.AreEqual(0, pairing.Winner);
        CollectionAssert.AreEqual(new[] { 1000, 1000, 800, 800, 640, 640 }, pairing.Match.Games.Select(g => g.TimeLimitMilliseconds).ToArray());
        Assert.AreEqual(4.0, pairing.Statistics.ScoreA); Assert.AreEqual(2.0, pairing.Statistics.ScoreB);
        Assert.AreEqual(pairing.Match.Games[4].OpeningName, pairing.Match.Games[5].OpeningName);
    }
    [TestMethod]
    public void BattleRoyaleRemovingBothKingsIsADraw() {
        var match = new Match(new MatchSettings { Player1 = BotRegistry.Find("T-Legal"), Player2 = BotRegistry.Find("T-Legal"), Games = 1,
            Configuration = new() { Variants = new() { new() { Kind = "battleroyale", Parameter = 1, Strength = 100 } } } });
        match.Run(); Assert.IsNull(match.Error);
        Assert.AreEqual(GameResult.Draw, match.Games[0].Result); Assert.AreEqual("both kings removed", match.Games[0].ResultReason);
    }
    [TestMethod]
    public void ArchiveBackupAndReplayWorkWithoutOriginalBotOrVariant() {
        string directory = Path.Combine(Path.GetTempPath(), "dchess-tournament-" + Guid.NewGuid().ToString("N"));
        try {
            var store = new TournamentStore(directory); var runner = new TournamentRunner(Settings("T-Legal", "T-Null"), store);
            runner.Run(); runner.Flush(); var completed = store.Load(runner.View().Id);
            Assert.AreEqual(TournamentStatus.Completed, completed.Status);
            var summary = store.ListSummaries().Single();
            Assert.AreEqual(completed.Id, summary.Id); Assert.AreEqual(2, summary.Entrants);
            Assert.AreEqual(completed.EntrantName(completed.Champion.Value), summary.Champion);
            string smokeDirectory = Environment.GetEnvironmentVariable("DCHESS_TOURNAMENT_SMOKE_ARCHIVES");
            if (smokeDirectory != null) Assert.IsTrue(new TournamentStore(smokeDirectory).Save(completed));
            Assert.IsTrue(store.Save(completed)); Assert.IsTrue(store.Save(completed));
            File.WriteAllText(store.ArchivePath(completed.Id), "broken archive");
            var recovered = store.Load(completed.Id); Assert.IsNotNull(recovered); Assert.AreEqual(completed.Champion, recovered.Champion);
            var pairing = recovered.Pairings[0]; pairing.Match.Player1 = "Deleted bot";
            pairing.Match.Board.Variants.Add(new() { Kind = "Deleted variant" });
            var replay = pairing.Match.CreateReplay();
            Assert.IsTrue(replay.IsFinished); Assert.AreEqual(2, replay.Games.Count);
            Assert.ThrowsException<InvalidOperationException>(replay.Start);
        } finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [TestMethod]
    public void AnOlderAsyncSaveCannotOverwriteResumedTournamentProgress() {
        string directory = Path.Combine(Path.GetTempPath(), "dchess-tournament-" + Guid.NewGuid().ToString("N"));
        try {
            var runner = new TournamentRunner(Settings("T-Legal", "T-Null")); runner.Run();
            var completed = runner.View(); var oldPause = completed.Copy();
            oldPause.Revision--; oldPause.Status = TournamentStatus.Paused;
            var store = new TournamentStore(directory);
            Assert.IsTrue(store.Save(completed)); Assert.IsTrue(store.Save(oldPause));
            var saved = store.Load(completed.Id);
            Assert.AreEqual(TournamentStatus.Completed, saved.Status);
            Assert.AreEqual(completed.Revision, saved.Revision);
        } finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [TestMethod]
    public async Task PausedTournamentResumesWithoutCountingAnAbortedGame() {
        string directory = Path.Combine(Path.GetTempPath(), "dchess-tournament-" + Guid.NewGuid().ToString("N"));
        BlockingBot.Entered.Reset(); BlockingBot.Release.Reset();
        try {
            var settings = Settings("T-Blocking", "T-Null"); settings.GamesPerPairing = 4; settings.TimeLimitMilliseconds = 5000;
            var store = new TournamentStore(directory); var runner = new TournamentRunner(settings, store); var task = runner.Start();
            Assert.IsTrue(BlockingBot.Entered.Wait(TimeSpan.FromSeconds(5)));
            var match = runner.ActiveMatch;
            var viewer = new SceneArena(null, match, () => { }); viewer.Stop();
            Assert.IsFalse(match.IsCancelled, "Leaving the viewer must not cancel tournament calculation.");
            runner.Pause(); runner.Flush(); await task.WaitAsync(TimeSpan.FromSeconds(5));
            var paused = store.Load(runner.View().Id); Assert.AreEqual(TournamentStatus.Paused, paused.Status);
            Assert.AreEqual(GameResult.Ongoing, paused.Pairings[0].Match.Games[0].Result);
            BlockingBot.Release.Set();
            var resumed = new TournamentRunner(paused.Settings, store, paused); await resumed.Start().WaitAsync(TimeSpan.FromSeconds(10)); resumed.Flush();
            var result = resumed.View(); Assert.AreEqual(TournamentStatus.Completed, result.Status, result.Error);
            Assert.AreEqual(4, result.Pairings[0].Statistics.Games);
            Assert.IsTrue(result.Pairings[0].Match.Games.All(g => g.Result != GameResult.Aborted));
        } finally { BlockingBot.Release.Set(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [TestMethod]
    public void DrawsAwardHalfAPointAndBookMovesDoNotInflateThinkTimes() {
        var board = BoardSetup.CreateStandardBoard();
        var game = new GameRecord(1, "A", "B", 0, openingPlies: 2);
        game.AddPosition(new(board, null, null, 0));
        game.AddPosition(new(board, null, "book", 0)); game.AddPosition(new(board, null, "book", 0));
        game.AddPosition(new(board, null, "played", 20)); game.Finish(GameResult.Draw, "test draw");
        var stats = PairingStatistics.Calculate(new MatchState { Games = new() { SavedGame.Capture(game) } });
        Assert.AreEqual(.5, stats.ScoreA); Assert.AreEqual(.5, stats.ScoreB);
        Assert.AreEqual(1.0, stats.AveragePlies); Assert.AreEqual(20.0, stats.AverageThinkA);
    }
}
