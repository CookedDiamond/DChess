using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.Chess.Playground;
using DChess.UI.Scenes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class ResponsiveBotTests {
        public sealed class BlockingBot : IChessBot {
            public static ManualResetEventSlim Entered = new(false);
            public static ManualResetEventSlim Release = new(false);
            public string Name => "BlockingBot";
            public Move Think(BotBoard board, BotTimer timer) {
                Entered.Set();
                Release.Wait(TimeSpan.FromSeconds(10));
                return board.GetLegalMoves()[0];
            }
        }

        [TestInitialize]
        public void ResetGate() { BlockingBot.Entered.Reset(); BlockingBot.Release.Reset(); }

        [TestCleanup]
        public void ReleaseBot() { BlockingBot.Release.Set(); }

        private static BoardManager Manager() {
            var manager = new BoardManager(BoardSetup.CreateStandardBoard(), new BoardNetworking());
            manager.SetComputerBot(new BlockingBot(), 10000);
            return manager;
        }

        [TestMethod]
        public void Bot_start_and_UI_updates_return_without_waiting_and_commit_on_UI_thread() {
            var manager = Manager();
            try {
                var clock = Stopwatch.StartNew();
                Assert.IsTrue(manager.BeginComputerMove(false));
                Assert.IsTrue(clock.ElapsedMilliseconds < 500, "Starting the bot blocked the caller.");
                Assert.IsTrue(BlockingBot.Entered.Wait(TimeSpan.FromSeconds(5)));
                Assert.IsTrue(manager.HasPendingComputerMove);
                Assert.IsFalse(manager.BeginComputerMove(false), "A second request must not overlap the first.");
                for (int i = 0; i < 100; i++) manager.UpdateComputerMove();
                Assert.AreEqual(0, manager.Board.GetMoveCount());
                int callerThread = Environment.CurrentManagedThreadId;
                int commitThread = 0;
                manager.BoardChanged += () => commitThread = Environment.CurrentManagedThreadId;
                BlockingBot.Release.Set();
                Assert.IsTrue(SpinWait.SpinUntil(() => { manager.UpdateComputerMove(); return !manager.IsThinking; }, TimeSpan.FromSeconds(5)));
                Assert.AreEqual(1, manager.Board.GetMoveCount());
                Assert.AreEqual(callerThread, commitThread);
            } finally { manager.CancelComputerMove(); }
        }

        [TestMethod]
        public void Undo_and_subsequent_moves_discard_stale_bot_results() {
            var manager = Manager();
            manager.MakeMove(FindMove(manager.Board, new(4, 1), new(4, 3)));
            try {
                manager.BeginComputerMove(false);
                Assert.IsTrue(BlockingBot.Entered.Wait(TimeSpan.FromSeconds(5)));
                manager.UndoLastMove();
                Assert.IsFalse(manager.HasPendingComputerMove);
                manager.MakeMove(FindMove(manager.Board, new(3, 1), new(3, 3)));
                string expected = manager.Board.GetPositionKey();
                BlockingBot.Release.Set();
                Assert.IsTrue(SpinWait.SpinUntil(() => { manager.UpdateComputerMove(); return !manager.IsThinking; }, TimeSpan.FromSeconds(5)));
                Assert.AreEqual(expected, manager.Board.GetPositionKey());
                Assert.AreEqual(1, manager.Board.GetMoveCount());
            } finally { manager.CancelComputerMove(); }
        }

        [TestMethod]
        public void Arena_shows_human_move_while_opponent_is_still_thinking() {
            var settings = new MatchSettings { Player1 = BotRegistry.Human, Player2 = new BotInfo("BlockingBot", typeof(BlockingBot)), Games = 1, TimeLimitMilliseconds = 10000 };
            var scene = new SceneArena(null, settings);
            var match = (Match)typeof(SceneArena).GetField("_match", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scene);
            try {
                Assert.IsTrue(SpinWait.SpinUntil(() => match.GetWaitingHuman()?.PendingBoard != null, TimeSpan.FromSeconds(5)));
                scene.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)));
                var human = match.GetWaitingHuman();
                human.SubmitMove(human.PendingBoard.GetLegalMovesFrom(new(4, 1))[0]);
                Assert.IsTrue(BlockingBot.Entered.Wait(TimeSpan.FromSeconds(5)));
                var clock = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++) scene.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)));
                Assert.IsTrue(clock.ElapsedMilliseconds < 500, "Arena UI updates blocked on the bot.");
                int displayedPly = (int)typeof(SceneArena).GetField("_ply", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scene);
                Assert.AreEqual(1, displayedPly, "The human move must be visible before the bot replies.");
                Assert.AreEqual(2, match.Games[0].PositionCount);
                Assert.IsFalse(BlockingBot.Release.IsSet);
            } finally { scene.Stop(); BlockingBot.Release.Set(); }
        }
    }
}
