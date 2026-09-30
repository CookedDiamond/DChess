using DChess.Chess.Playground;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.BotApi {
	public class MoveRequestResult {
		/// <summary>The chosen move as a legal move of the real board, or null on error.</summary>
		public Move Move { get; init; }

		/// <summary>Why the bot failed to deliver a legal move (null if everything is fine).</summary>
		public string Error { get; init; }

		public long ElapsedMilliseconds { get; init; }

		public bool Cancelled { get; init; }
	}

	/// <summary>
	/// Asks a bot for a move. The bot works on a private copy of the board,
	/// and its answer is checked against the legal moves of the real board.
	/// </summary>
	public static class BotRunner {
		public static MoveRequestResult RequestMove(IChessBot bot, Board realBoard, int timeLimitMilliseconds, CancellationToken cancellationToken) {
			bool isHuman = bot is HumanPlayer;
			var botBoard = new BotBoard(realBoard.CloneBoard(), realBoard.GetTurnTeamType());
			var timer = new BotTimer(isHuman ? int.MaxValue : timeLimitMilliseconds);
			var stopwatch = Stopwatch.StartNew();

			// A dedicated thread, so a bot that never returns can not block the game.
			var thinkTask = Task.Factory.StartNew(() => bot.Think(botBoard, timer),
				CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

			Move chosenMove;
			try {
				if (isHuman) {
					thinkTask.Wait(cancellationToken);
				}
				else {
					// A little grace time for timer inaccuracy.
					int grace = Math.Max(100, timeLimitMilliseconds / 10);
					if (!thinkTask.Wait(timeLimitMilliseconds + grace, cancellationToken)) {
						return fail($"ran out of time (limit {timeLimitMilliseconds} ms)", stopwatch);
					}
				}
				chosenMove = thinkTask.Result;
			}
			catch (OperationCanceledException) {
				return new MoveRequestResult { Cancelled = true, Error = "cancelled", ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };
			}
			catch (AggregateException e) {
				Exception inner = e.InnerException ?? e;
				if (inner is TaskCanceledException) {
					return new MoveRequestResult { Cancelled = true, Error = "cancelled", ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };
				}
				Console.WriteLine($"[{bot.Name}] crashed:{Environment.NewLine}{inner}");
				return fail($"crashed: {inner.GetType().Name}: {inner.Message}", stopwatch);
			}

			if (chosenMove == null) {
				return fail("returned no move (null)", stopwatch);
			}

			Move realMove = realBoard.FindEquivalentLegalMove(chosenMove);
			if (realMove == null) {
				return fail($"returned an illegal move ({chosenMove})", stopwatch);
			}

			return new MoveRequestResult { Move = realMove, ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };
		}

		private static MoveRequestResult fail(string error, Stopwatch stopwatch) {
			return new MoveRequestResult { Error = error, ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };
		}
	}
}
