using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Arena {
	public enum GameResult {
		Ongoing,
		WhiteWins,
		BlackWins,
		Draw,
		Aborted
	}

	/// <summary>
	/// A frozen copy of a position, so a game can be replayed without the rules engine.
	/// </summary>
	public class PositionSnapshot {
		public int Width { get; }
		public int Height { get; }
		public PieceType[,] Types { get; }
		public TeamType[,] Teams { get; }
		public bool[,] Disabled { get; }
		public TeamType SideToMove { get; }

		/// <summary>Squares changed by the move that led to this position.</summary>
		public Vector2Int[] ChangedSquares { get; }

		/// <summary>Notation of the move that led to this position (null for the start position).</summary>
		public string MoveText { get; }

		/// <summary>How long the player thought about the move that led to this position.</summary>
		public long ThinkMilliseconds { get; }

		public PositionSnapshot(Board board, Move lastMove, string moveText, long thinkMilliseconds,
			TeamType? sideToMove = null, Vector2Int[] changedSquares = null) {
			Width = board.Size.x;
			Height = board.Size.y;
			Types = new PieceType[Width, Height];
			Teams = new TeamType[Width, Height];
			Disabled = new bool[Width, Height];
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					Piece piece = board.GetPiece(new Vector2Int(x, y));
					Types[x, y] = piece.Type;
					Teams[x, y] = piece.Team;
					Disabled[x, y] = board.SquareMap[x, y] == SquareType.Disabled;
				}
			}
			SideToMove = sideToMove ?? board.GetTurnTeamType();
			ChangedSquares = changedSquares ?? lastMove?.Changes.Select(c => c.boardPosition).Distinct().ToArray() ?? new Vector2Int[0];
			MoveText = moveText;
			ThinkMilliseconds = thinkMilliseconds;
		}
	}

	/// <summary>
	/// One game of a match: all positions, the players and the result.
	/// Written by the match thread and read by the UI, so all access is locked.
	/// </summary>
	public class GameRecord {
		private readonly object _lock = new();
		private readonly List<PositionSnapshot> _positions = new();

		public int Number { get; }
		public string WhiteName { get; }
		public string BlackName { get; }

		/// <summary>Index (0 or 1) of the match player that plays white.</summary>
		public int WhitePlayerIndex { get; }
        public GameConfiguration Configuration { get; }
        public string OpeningName { get; }
        public int OpeningPlies { get; }
        public int TimeLimitMilliseconds { get; }

		private volatile GameResult _result = GameResult.Ongoing;
		public GameResult Result => _result;
		public string ResultReason { get; private set; } = "";

		public bool IsFinished => Result != GameResult.Ongoing;

		public GameRecord(int number, string whiteName, string blackName, int whitePlayerIndex, GameConfiguration configuration = null,
            string openingName = "Start position", int openingPlies = 0, int timeLimitMilliseconds = 0) {
            Configuration = configuration ?? new();
            OpeningName = openingName; OpeningPlies = openingPlies; TimeLimitMilliseconds = timeLimitMilliseconds;
			Number = number;
			WhiteName = whiteName;
			BlackName = blackName;
			WhitePlayerIndex = whitePlayerIndex;
		}

		public int PositionCount {
			get { lock (_lock) return _positions.Count; }
		}

		public PositionSnapshot GetPosition(int index) {
			lock (_lock) return _positions[index];
		}

		public void AddPosition(PositionSnapshot snapshot) {
			lock (_lock) _positions.Add(snapshot);
		}

		public void Finish(GameResult result, string reason) {
			lock (_lock) {
				ResultReason = reason;
				_result = result;
			}
		}

		public string ResultScore => Result switch {
			GameResult.WhiteWins => "1-0",
			GameResult.BlackWins => "0-1",
			GameResult.Draw => "1/2-1/2",
			GameResult.Aborted => "aborted",
			_ => "*"
		};

		public string ResultText => Result switch {
			GameResult.WhiteWins => $"White ({WhiteName}) wins: {ResultReason}",
			GameResult.BlackWins => $"Black ({BlackName}) wins: {ResultReason}",
			GameResult.Draw => $"Draw: {ResultReason}",
			GameResult.Aborted => "Game aborted",
			_ => "In progress"
		};

		/// <summary>All moves in one line, e.g. "1. e4 e5 2. Nf3 ...".</summary>
		public string GetMoveListText() {
			lock (_lock) {
				var parts = new List<string>();
				for (int i = 1; i < _positions.Count; i++) {
					if (i % 2 == 1) parts.Add($"{(i + 1) / 2}.");
					parts.Add(_positions[i].MoveText);
				}
				return string.Join(" ", parts);
			}
		}
	}
}
