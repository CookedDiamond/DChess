using DChess.Chess.Playground;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.BotApi {
	/// <summary>
	/// A "bot" that waits for a human to click a move in the arena.
	/// Think blocks until the UI calls SubmitMove (or the game is cancelled).
	/// </summary>
	public sealed class HumanPlayer : IChessBot {
		private TaskCompletionSource<Move> _pendingMove;
		private volatile bool _cancelled;

		public string Name => "Human";

		/// <summary>The position the human has to move in, or null while it is not the human's turn.</summary>
		public BotBoard PendingBoard { get; private set; }

		public Move Think(BotBoard board, BotTimer timer) {
			var pendingMove = new TaskCompletionSource<Move>(TaskCreationOptions.RunContinuationsAsynchronously);
			_pendingMove = pendingMove;
			if (_cancelled) pendingMove.TrySetCanceled();
			PendingBoard = board;
			try {
				return pendingMove.Task.GetAwaiter().GetResult();
			}
			finally {
				PendingBoard = null;
			}
		}

		public void SubmitMove(Move move) {
			_pendingMove?.TrySetResult(move);
		}

		public void Cancel() {
			_cancelled = true;
			_pendingMove?.TrySetCanceled();
		}
	}
}
