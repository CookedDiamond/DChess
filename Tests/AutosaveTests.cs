using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class AutosaveTests {
        private static Board RoundTrip(Board board) => JsonSerializer.Deserialize<BoardState>(JsonSerializer.Serialize(BoardState.Capture(board))).Restore();

        [TestMethod]
        public void Saved_game_restores_turn_castling_rights_and_complete_undo_history() {
            var board = BoardSetup.CreateStandardBoard();
            board.MakeMove(FindMove(board, new(4, 1), new(4, 3)));
            board.MakeMove(FindMove(board, new(4, 6), new(4, 4)));
            board.MakeMove(FindMove(board, new(4, 0), new(4, 1)));
            var restored = RoundTrip(board);
            Assert.AreEqual(board.GetPositionKey(), restored.GetPositionKey());
            Assert.AreEqual(3, restored.GetMoveCount());
            Assert.AreEqual(1, restored.GetPiece(new(4, 1)).MoveCount);
            Assert.AreEqual(board.Variants.Count, restored.Variants.Count);
            while (board.GetMoveCount() > 0) {
                board.UndoLastMove(); restored.UndoLastMove();
                Assert.AreEqual(board.GetPositionKey(), restored.GetPositionKey());
            }
            Assert.IsNotNull(FindMove(restored, new(4, 1), new(4, 3)));
        }

        [TestMethod]
        public void Promotion_capture_survives_reload_and_undo() {
            var board = EmptyBoard();
            board.Variants.Add(new VariantPawnQueenPromotion());
            Place(board, 0, 0, PieceType.King, TeamType.White);
            Place(board, 7, 7, PieceType.King, TeamType.Black);
            Place(board, 4, 6, PieceType.Pawn, TeamType.White);
            Place(board, 5, 7, PieceType.Rook, TeamType.Black);
            board.MakeMove(FindMove(board, new(4, 6), new(5, 7)));
            var restored = RoundTrip(board);
            Assert.AreEqual(PieceType.Queen, restored.GetPiece(new(5, 7)).Type);
            restored.UndoLastMove();
            Assert.AreEqual(PieceType.Pawn, restored.GetPiece(new(4, 6)).Type);
            Assert.AreEqual(PieceType.Rook, restored.GetPiece(new(5, 7)).Type);
            Assert.AreEqual(0, restored.GetPiece(new(4, 6)).MoveCount);
        }

        [TestMethod]
        public void Disabled_squares_and_variant_parameters_survive_reload_and_undo() {
            var board = CastleReadyBoard();
            board.Variants.Add(new VariantBattleRoyale(1, 1));
            board.MakeMove(FindMove(board, new(4, 0), new(4, 1)));
            var restored = RoundTrip(board);
            Assert.IsFalse(restored.IsValidPosition(new(0, 0)));
            restored.UndoLastMove();
            Assert.IsTrue(restored.IsValidPosition(new(0, 0)));
            Assert.AreEqual(PieceType.Rook, restored.GetPiece(new(0, 0)).Type);
            restored.MakeMove(FindMove(restored, new(4, 0), new(4, 1)));
            Assert.IsFalse(restored.IsValidPosition(new(0, 0)));
        }

        [TestMethod]
        public void File_store_recovers_previous_save_after_corruption_and_ignores_stale_writers() {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DChess-test-" + Guid.NewGuid() + ".json");
            try {
                var store = new AutosaveStore(path);
                var board = BoardSetup.CreateStandardBoard();
                SessionState State() => new() { Mode = "sandbox", Board = BoardState.Capture(board), HelperBot = "MinMaxBot", BotMovePending = true };
                Assert.IsTrue(store.Save(State()));
                board.MakeMove(FindMove(board, new(4, 1), new(4, 3)));
                Assert.IsTrue(store.Save(State()));
                Assert.AreEqual(1, new AutosaveStore(path).Load().Board.Restore().GetMoveCount());
                Assert.IsTrue(new AutosaveStore(path).Load().BotMovePending);
                Assert.IsFalse(store.Save(State(), () => false));
                File.WriteAllText(path, "broken json");
                Assert.AreEqual(0, store.Load().Board.Restore().GetMoveCount());
                StringAssert.Contains(store.LastError, "Recovered");
                File.WriteAllText(path + ".bak", "also broken");
                Assert.IsNull(store.Load());
                Assert.IsNotNull(store.LastError);
            } finally {
                foreach (string suffix in new[] { "", ".bak", ".tmp" }) File.Delete(path + suffix);
            }
        }

        [TestMethod]
        public void Human_match_resumes_current_turn_and_keeps_replay_and_scores() {
            var settings = new MatchSettings { Player1 = BotRegistry.Human, Player2 = BotRegistry.Human, Games = 1, MaxPlies = 2 };
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DChess-match-test-" + Guid.NewGuid() + ".json");
            var store = new AutosaveStore(path);
            void Save(MatchState saved) => Assert.IsTrue(store.Save(new SessionState { Mode = "match", Match = saved }));
            var match = new Match(settings, autosave: Save);
            match.Start();
            Match resumed = null;
            try {
                WaitFor(() => match.GetWaitingHuman()?.PendingBoard?.SideToMove == TeamType.White);
                var human = match.GetWaitingHuman();
                human.SubmitMove(human.PendingBoard.GetLegalMovesFrom(new(4, 1)).First(m => m.To == new DChess.Util.Vector2Int(4, 3)));
                WaitFor(() => match.GetWaitingHuman()?.PendingBoard?.SideToMove == TeamType.Black);
                var state = store.Load().Match;
                match.Cancel();
                resumed = new Match(state.CreateSettings(), state, Save);
                resumed.Start();
                WaitFor(() => resumed.GetWaitingHuman()?.PendingBoard?.SideToMove == TeamType.Black);
                Assert.AreEqual(2, resumed.Games[0].PositionCount);
                Assert.AreEqual(PieceType.Pawn, resumed.Games[0].GetPosition(1).Types[4, 3]);
                var black = resumed.GetWaitingHuman();
                black.SubmitMove(black.PendingBoard.GetLegalMovesFrom(new(4, 6)).First(m => m.To == new DChess.Util.Vector2Int(4, 4)));
                WaitFor(() => resumed.IsFinished);
                Assert.AreEqual(GameResult.Draw, resumed.Games[0].Result);
                Assert.AreEqual(3, resumed.Games[0].PositionCount);
                Assert.AreEqual(0.5, resumed.GetScore(0));
                Assert.AreEqual(GameResult.Draw, store.Load().Match.Games[0].Result);
                var finished = new Match(state.CreateSettings(), resumed.CaptureState());
                finished.Run();
                Assert.AreEqual(1, finished.Games.Count);
                Assert.AreEqual(0.5, finished.GetScore(0));
            } finally {
                match.Cancel(); resumed?.Cancel();
                foreach (string suffix in new[] { "", ".bak", ".tmp" }) File.Delete(path + suffix);
            }
        }

        [TestMethod]
        public void Resumed_match_detects_repetition_from_saved_history_before_requesting_a_move() {
            var board = BoardSetup.CreateStandardBoard();
            var record = new GameRecord(1, "Human (1)", "Human (2)", 0);
            record.AddPosition(new PositionSnapshot(board, null, null, 0));
            foreach (var (from, to) in new[] { ("g1", "f3"), ("g8", "f6"), ("f3", "g1"), ("f6", "g8"),
                ("g1", "f3"), ("g8", "f6"), ("f3", "g1"), ("f6", "g8") }) {
                board.MakeMove(FindMove(board, DChess.Util.Vector2Int.FromSquareName(from), DChess.Util.Vector2Int.FromSquareName(to)));
                record.AddPosition(new PositionSnapshot(board, board.GetLastMove(), from + to, 0));
            }
            var state = new MatchState { Player1 = "Human", Player2 = "Human", GameCount = 1,
                MaxPlies = 500, TimeLimitMilliseconds = 1000, Board = BoardState.Capture(board), Games = new() { SavedGame.Capture(record) } };
            var match = new Match(state.CreateSettings(), state);
            match.Start();
            try {
                WaitFor(() => match.IsFinished);
                Assert.AreEqual(GameResult.Draw, match.Games[0].Result);
                Assert.AreEqual("threefold repetition", match.Games[0].ResultReason);
                Assert.AreEqual(9, match.Games[0].PositionCount);
            } finally { match.Cancel(); }
        }

        private static void WaitFor(Func<bool> condition) => Assert.IsTrue(SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(5)), "Timed out waiting for the match.");
    }
}
