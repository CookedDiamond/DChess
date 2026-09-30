using System;
using System.Diagnostics;
using System.Threading;

namespace DChess.BotApi {
	/// <summary>
	/// Time information for the current move. The clock starts when your bot is asked to move.
	/// If Think takes longer than the time limit your bot loses the game.
	/// </summary>
	public sealed class BotTimer {
		private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
		private readonly CancellationToken _cancellation;

		/// <summary>Maximum time for this move in milliseconds.</summary>
		public int TimeLimitMilliseconds { get; }

		/// <summary>Time spent on this move so far.</summary>
		public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

		/// <summary>Time left for this move.</summary>
		public long MillisecondsRemaining => _cancellation.IsCancellationRequested ? 0 : Math.Max(0, TimeLimitMilliseconds - ElapsedMilliseconds);

		public bool IsTimeUp => _cancellation.IsCancellationRequested || ElapsedMilliseconds >= TimeLimitMilliseconds;

		public BotTimer(int timeLimitMilliseconds, CancellationToken cancellation = default) {
			TimeLimitMilliseconds = timeLimitMilliseconds;
			_cancellation = cancellation;
		}
	}
}
