using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace DChess.BotApi {
	/// <summary>
	/// A piece together with the square it stands on.
	/// </summary>
	public readonly record struct PieceOnSquare(Vector2Int Square, Piece Piece) {
		public PieceType Type => Piece.Type;
		public TeamType Team => Piece.Team;
	}

	/// <summary>
	/// The view of the game a bot gets when it has to move.
	///
	/// Coordinates: squares are Vector2Int(x, y) with x = file (0 = a ... 7 = h) and y = rank (0 = rank 1 ... 7 = rank 8).
	/// White starts on y = 0 and 1 and moves up (+y), black starts on y = 6 and 7 and moves down (-y).
	///
	/// This board is a private copy of the real game, so MakeMove/UndoMove can be used freely for searching.
	/// </summary>
	public sealed class BotBoard {
		private readonly Board _board;
		private int _movesMade;

		/// <summary>The team your bot plays. Stays the same while you make/undo moves.</summary>
		public TeamType MyTeam { get; }

		public TeamType OpponentTeam => GetOpponent(MyTeam);

		/// <summary>The team whose turn it is in the current position (changes with MakeMove/UndoMove).</summary>
		public TeamType SideToMove => _board.GetTurnTeamType();

		public bool IsWhiteToMove => _board.IsWhitesTurn;

		/// <summary>Number of files (columns).</summary>
		public int Width => _board.Size.x;

		/// <summary>Number of ranks (rows).</summary>
		public int Height => _board.Size.y;

		/// <summary>Half-moves played in the game so far, including moves you made on this board.</summary>
		public int PlyCount => _board.GetMoveCount();

		/// <summary>How many of your own MakeMove calls have not been undone yet.</summary>
		public int MovesMadeOnThisBoard => _movesMade;

		/// <summary>True if a king was captured (the game is over).</summary>
		public bool IsGameOver => _board.HasTeamWon() != TeamType.None;

		/// <summary>The team that captured the enemy king, or TeamType.None.</summary>
		public TeamType Winner => _board.HasTeamWon();

		internal BotBoard(Board board, TeamType myTeam) {
			_board = board;
			MyTeam = myTeam;
		}

		public static TeamType GetOpponent(TeamType team) {
			return team switch {
				TeamType.White => TeamType.Black,
				TeamType.Black => TeamType.White,
				_ => TeamType.None
			};
		}

		// ------------------------------------------------------------------------------------
		// Moves
		// ------------------------------------------------------------------------------------

		/// <summary>All legal moves of the side to move.</summary>
		public List<Move> GetLegalMoves() {
			return _board.GetAllLegalMovesForTeam(SideToMove);
		}

		/// <summary>All moves the given team could make if it was their turn (useful for mobility).</summary>
		public List<Move> GetLegalMoves(TeamType team) {
			return _board.GetAllLegalMovesForTeam(team);
		}

		/// <summary>All legal moves of the piece on the square (empty list if there is no piece).</summary>
		public List<Move> GetLegalMovesFrom(Vector2Int square) {
			Piece piece = _board.GetPiece(square);
			if (piece == Piece.NULL_PIECE) return new List<Move>();
			return piece.GetAllLegalMoves(square);
		}

		/// <summary>All legal moves of the side to move that capture something.</summary>
		public List<Move> GetCaptureMoves() {
			List<Move> captures = new();
			foreach (var move in GetLegalMoves()) {
				if (move.IsCapture) captures.Add(move);
			}
			return captures;
		}

		/// <summary>Finds the legal move from one square to another, or null.</summary>
		public Move FindMove(Vector2Int from, Vector2Int to) {
			foreach (var move in GetLegalMoves()) {
				if (move.From == from && move.To == to) return move;
			}
			return null;
		}

		/// <summary>Finds a legal move by its name, e.g. "e2e4" (castling: king move, e.g. "e1g1"). Returns null if not legal.</summary>
		public Move FindMove(string moveName) {
			if (moveName == null || moveName.Length < 4) return null;
			return FindMove(Vector2Int.FromSquareName(moveName.Substring(0, 2)), Vector2Int.FromSquareName(moveName.Substring(2, 2)));
		}

		/// <summary>
		/// Plays a move on this board. Only pass moves from GetLegalMoves() of the current position.
		/// </summary>
		public void MakeMove(Move move) {
			if (move == null) throw new ArgumentNullException(nameof(move));
			_board.MakeMove(move);
			_movesMade++;
		}

		/// <summary>Takes back the last move made with MakeMove.</summary>
		public void UndoMove() {
			if (_movesMade <= 0) {
				throw new InvalidOperationException("UndoMove: there is no move made with MakeMove left to undo.");
			}
			_board.UndoLastMove();
			_movesMade--;
		}

		// ------------------------------------------------------------------------------------
		// Pieces
		// ------------------------------------------------------------------------------------

		/// <summary>The piece on the square. Empty squares return Piece.NULL_PIECE (Type == PieceType.None).</summary>
		public Piece GetPiece(Vector2Int square) {
			return _board.GetPiece(square);
		}

		public Piece GetPiece(int x, int y) {
			return _board.GetPiece(new Vector2Int(x, y));
		}

		public bool IsEmpty(Vector2Int square) {
			return _board.GetPiece(square) == Piece.NULL_PIECE;
		}

		/// <summary>True if the square exists (inside the board and not removed by a variant).</summary>
		public bool IsOnBoard(Vector2Int square) {
			return _board.IsValidPosition(square);
		}

		/// <summary>All pieces of a team.</summary>
		public List<PieceOnSquare> GetPieces(TeamType team) {
			List<PieceOnSquare> result = new();
			foreach (var pair in _board.GetPieceDictionary()) {
				if (pair.Value.Team == team) result.Add(new PieceOnSquare(pair.Key, pair.Value));
			}
			return result;
		}

		/// <summary>All pieces on the board.</summary>
		public List<PieceOnSquare> GetAllPieces() {
			List<PieceOnSquare> result = new();
			foreach (var pair in _board.GetPieceDictionary()) {
				result.Add(new PieceOnSquare(pair.Key, pair.Value));
			}
			return result;
		}

		public int CountPieces(TeamType team, PieceType type) {
			int count = 0;
			foreach (var piece in _board.GetPieceDictionary().Values) {
				if (piece.Team == team && piece.Type == type) count++;
			}
			return count;
		}

		/// <summary>The square of the team's king, or null if it was captured.</summary>
		public Vector2Int? FindKing(TeamType team) {
			foreach (var pair in _board.GetPieceDictionary()) {
				if (pair.Value.Team == team && pair.Value.Type == PieceType.King) return pair.Key;
			}
			return null;
		}

		/// <summary>True if a piece of the attacker team could capture on this square (works for empty squares too).</summary>
		public bool IsSquareAttackedBy(Vector2Int square, TeamType attacker) {
			return _board.IsSquareAttackedBy(square, attacker);
		}

		/// <summary>True if the team's king can be captured by the opponent right now ("check").</summary>
		public bool IsKingAttacked(TeamType team) {
			Vector2Int? king = FindKing(team);
			return king.HasValue && IsSquareAttackedBy(king.Value, GetOpponent(team));
		}

		/// <summary>
		/// The board as text (white = uppercase, black = lowercase), rank 8 at the top.
		/// Handy for Console.WriteLine debugging.
		/// </summary>
		public override string ToString() {
			StringBuilder text = new();
			for (int y = Height - 1; y >= 0; y--) {
				text.Append(y + 1).Append(' ');
				for (int x = 0; x < Width; x++) {
					Vector2Int square = new(x, y);
					Piece piece = GetPiece(square);
					char c = !IsOnBoard(square) ? '#' : piece == Piece.NULL_PIECE ? '.' : Piece.TypeAsChar(piece);
					text.Append(piece.Team == TeamType.White ? char.ToUpper(c) : c).Append(' ');
				}
				text.AppendLine();
			}
			text.Append("  ");
			for (int x = 0; x < Width; x++) text.Append((char)('a' + x)).Append(' ');
			text.AppendLine().Append(SideToMove).Append(" to move");
			return text.ToString();
		}
	}
}
