using DChess.Chess.Playground;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.BotApi {
    public class MoveRequestResult {
        public Move Move { get; init; }
        public string Error { get; init; }
        public long ElapsedMilliseconds { get; init; }
        public bool Cancelled { get; init; }
    }

    /// <summary>Runs bot code on an isolated board and validates its answer.</summary>
    public static class BotRunner {
        // Blocking callers (CLI and the match worker) share the same request implementation.
        public static MoveRequestResult RequestMove(IChessBot bot, Board realBoard, int timeLimitMilliseconds, CancellationToken cancellationToken) =>
            RequestMoveAsync(bot, realBoard, timeLimitMilliseconds, cancellationToken).GetAwaiter().GetResult();

        /// <summary>Awaiting a bot never occupies the GUI thread or a waiting worker thread.</summary>
        public static async Task<MoveRequestResult> RequestMoveAsync(IChessBot bot, Board realBoard, int timeLimitMilliseconds, CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested)
                return new MoveRequestResult { Cancelled = true, Error = "cancelled" };
            bool isHuman = bot is HumanPlayer;
            var botBoard = new BotBoard(realBoard.CloneBoard(), realBoard.GetTurnTeamType());
            var timer = new BotTimer(isHuman ? int.MaxValue : timeLimitMilliseconds, cancellationToken);
            var stopwatch = Stopwatch.StartNew();

            // User bot code may ignore the timer; isolate it on its own background thread.
            var thinkTask = Task.Factory.StartNew(() => bot.Think(botBoard, timer),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _ = thinkTask.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Move chosenMove;
            try {
                if (isHuman) chosenMove = await thinkTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                else {
                    int grace = Math.Max(100, timeLimitMilliseconds / 10);
                    chosenMove = await thinkTask.WaitAsync(TimeSpan.FromMilliseconds((long)timeLimitMilliseconds + grace), cancellationToken).ConfigureAwait(false);
                }
            } catch (TimeoutException) {
                return Fail($"ran out of time (limit {timeLimitMilliseconds} ms)", stopwatch);
            } catch (OperationCanceledException) {
                return new MoveRequestResult { Cancelled = true, Error = "cancelled", ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };
            } catch (Exception ex) {
                Console.WriteLine($"[{bot.Name}] crashed:{Environment.NewLine}{ex}");
                return Fail($"crashed: {ex.GetType().Name}: {ex.Message}", stopwatch);
            }

            if (chosenMove == null) return Fail("returned no move (null)", stopwatch);
            Move realMove = realBoard.FindEquivalentLegalMove(chosenMove);
            if (realMove == null) return Fail($"returned an illegal move ({chosenMove})", stopwatch);
            return new MoveRequestResult { Move = realMove, ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };
        }

        private static MoveRequestResult Fail(string error, Stopwatch stopwatch) =>
            new() { Error = error, ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };
    }
}
