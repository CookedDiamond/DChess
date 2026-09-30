using DChess.Chess.ChessAI;
using DChess.Chess.Pieces;
using DChess.Chess.Variants;
using DChess.Multiplayer;
using DChess.UI;
using DChess.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Playground {
	public class Board {
		public static readonly TeamType START_TEAM = TeamType.White;
		public static readonly TeamType SECOND_TEAM = TeamType.Black;

		// TODO: Remove Dictionary -> performance
		public readonly Dictionary<Vector2Int, Piece> Pieces = new();

		public SquareType[,] SquareMap;
		public Vector2Int Size { get; set; }
		public bool IsWhitesTurn => _moveHistory.Count % 2 == (START_TEAM == TeamType.White ? 0 : 1);
		public List<Variant> Variants { get; set; }

		private readonly List<Move> _moveHistory = new();
		public long Revision { get; private set; }
		public IReadOnlyList<Move> GetMoveHistory() => _moveHistory.ToArray();
		internal void RestoreHistory(IEnumerable<Move> moves) => _moveHistory.AddRange(moves);

		public Board(Vector2Int size) {
			Size = size;
			Variants = new List<Variant>();
			SquareMap = new SquareType[size.x, size.y];
		}

		public void PlacePiece(Vector2Int position, Piece piece) {
			Revision++;
			if (piece == Piece.NULL_PIECE) Pieces.Remove(position);
			else Pieces[position] = piece;
		}

		public bool RemovePiece(Vector2Int position) {
			Revision++;
			return Pieces.Remove(position);
		}

		public void RemoveSquare(Vector2Int position) {
			Revision++;
			SquareMap[position.x, position.y] = SquareType.Disabled;
			Pieces.Remove(position);
		}

		public Vector2 GetCenter() {
			return new Vector2(Size.x / 2f, Size.y / 2f);
		}

		public bool MakeMove(Move move, bool doAfterTurnUpdate = true) {
			if (move == null) return false;
			if (move.Changes.Any(c => c.newPiece != Piece.NULL_PIECE && c.newPiece.Owner != this)) {
				// The move was created on another board (e.g. a clone): use the pieces of this board.
				var pieceMap = new Dictionary<Piece, Piece>();
				foreach (var change in move.Changes) {
					if (change.oldPiece != Piece.NULL_PIECE)
						pieceMap[change.oldPiece] = GetPiece(change.boardPosition);
				}
				move = move.CloneForBoard(this, pieceMap);
			}
			else {
				// Variants can add changes to the played move (e.g. promotion),
				// so work on a copy and never change the move object of the caller.
				move = move.Copy();
			}
			move.Apply(this);
			_moveHistory.Add(move);
			if (doAfterTurnUpdate) {
				afterTurnUpdate(move);
			}
			return true;
		}

		public void UndoLastMove() {
			if (_moveHistory.Count <= 0) return;
			var lastMove = GetLastMove();

			lastMove.Undo(this);
			_moveHistory.RemoveAt(_moveHistory.Count - 1);
		}

		/// <summary>
		/// Adds changes to the last move (used by variants, e.g. promotion or removed squares).
		/// The move is undone and applied again, so undo restores everything in one step.
		/// </summary>
		public void AddToLastMove(List<BoardChange> additionalChanges) {
			Move lastMove = GetLastMove();
			lastMove.Undo(this);
			foreach (var change in additionalChanges) {
				lastMove.AddChange(change);
			}
			lastMove.Apply(this);
		}

		private void afterTurnUpdate(Move lastMove) {
			foreach (var variant in Variants) {
				variant.AfterTurnUpdate(this, lastMove);
			}
		}

		public TeamType GetTurnTeamType() {
			return IsWhitesTurn ? START_TEAM : SECOND_TEAM;
		}

		public Piece GetPiece(Vector2Int position) {
			if (!IsValidPosition(position)) {
				return Piece.NULL_PIECE;
			}
			if (Pieces.TryGetValue(position, out Piece piece)) {
				return piece;
			}
			return Piece.NULL_PIECE;
		}

		public Dictionary<Vector2Int, Piece> GetPieceDictionary() {
			return Pieces;
		}

		public List<KeyValuePair<Vector2Int, Piece>> GetAllPiecesFromTeam(TeamType teamType) {
			List<KeyValuePair<Vector2Int, Piece>> result = new();
			foreach (var keyValuePair in Pieces) {
				if (keyValuePair.Value.Team == teamType) {
					result.Add(keyValuePair);
				}
			}

			return result;
		}

		public List<Move> GetAllLegalMovesForTeam(TeamType team) {
			List<KeyValuePair<Vector2Int, Piece>> teamPieces = GetAllPiecesFromTeam(team);
			List<Move> result = new();
			foreach (var pair in teamPieces) {
				result.AddRange(pair.Value.GetAllLegalMoves(pair.Key));
			}
			return result;
		}

		public List<KeyValuePair<Vector2Int, Piece>> GetPiecesFromTeamWithType(PieceType pieceType, TeamType teamType) {
			List<KeyValuePair<Vector2Int, Piece>> result = new();
			foreach (var pair in Pieces) {
				if (pair.Value != Piece.NULL_PIECE
					&& pair.Value.Type == pieceType
					&& pair.Value.Team == teamType) {
					result.Add(pair);
				}
			}
			return result;
		}

		public int GetMoveCount() {
			return _moveHistory.Count;
		}

		public Move GetLastMove() {
			return _moveHistory.Last();
		}
		public float GetEvaluaton() {
			return new Evaluation(this).GetEvaluation();
		}

		public int GetTotalPieceCount() {
			return Pieces.Count;
		}

		public bool IsValidPosition(Vector2Int vector) {
			bool inBoundsLocation = vector.x >= 0
									&& vector.y >= 0
									&& vector.x < Size.x
									&& vector.y < Size.y;
			if (inBoundsLocation && SquareMap[vector.x, vector.y] == SquareType.Disabled) return false;
			return inBoundsLocation;
		}
		public bool IsStartingPawnRow(TeamType team, int row) {
			if (team == TeamType.White && row == 1
				|| team == TeamType.Black && row == Size.y - 2) {
				return true;
			}
			return false;
		}

		public TeamType HasTeamWon() {
			var blackKingList = GetPiecesFromTeamWithType(PieceType.King, TeamType.Black);
			var whiteKingList = GetPiecesFromTeamWithType(PieceType.King, TeamType.White);

			if (blackKingList.Count == 0) {
				return TeamType.White;
			}
			if (whiteKingList.Count == 0) {
				return TeamType.Black;
			}
			return TeamType.None;
		}

		/// <summary>
		/// Creates a completely independent copy of the board.
		/// Every piece (also the captured ones in the move history) is cloned and bound to the new board,
		/// so moves can be made and undone on the copy without touching this board.
		/// </summary>
		public Board CloneBoard() {
			Board returnBoard = new (Size);
			var pieceMap = new Dictionary<Piece, Piece>();

			for (int x = 0; x < Size.x; x++) {
				for (int y = 0; y < Size.y; y++) {
					returnBoard.SquareMap[x, y] = SquareMap[x, y];
				}
			}

			foreach (var pair in Pieces) {
				var oldPiece = pair.Value;
				var clonedPiece = oldPiece.ClonePiece(returnBoard);
				pieceMap[oldPiece] = clonedPiece;
				returnBoard.PlacePiece(pair.Key, clonedPiece);
			}

			foreach (var move in _moveHistory) {
				returnBoard._moveHistory.Add(move.CloneForBoard(returnBoard, pieceMap));
			}

			foreach (var variant in Variants) {
				returnBoard.Variants.Add(variant.Clone());
			}

			return returnBoard;
		}

		/// <summary>
		/// Finds the legal move on this board that does the same as the given move.
		/// The given move may come from a cloned board (e.g. from a bot).
		/// </summary>
		/// <returns>The matching legal move of this board or null if the move is not legal here.</returns>
		public Move FindEquivalentLegalMove(Move move) {
			if (move == null) return null;
			foreach (var legalMove in GetAllLegalMovesForTeam(GetTurnTeamType())) {
				if (legalMove.IsEquivalentTo(move)) {
					return legalMove;
				}
			}
			return null;
		}

		/// <summary>
		/// True if a piece of the attacker team could capture on the square (standard piece movement).
		/// Also works for empty squares.
		/// </summary>
		public bool IsSquareAttackedBy(Vector2Int square, TeamType attacker) {
			// Pawns.
			Vector2Int pawnOrigin = square - GetTeamDirection(attacker);
			if (isPieceOf(pawnOrigin + Vector2Int.LEFT, attacker, PieceType.Pawn)
				|| isPieceOf(pawnOrigin + Vector2Int.RIGHT, attacker, PieceType.Pawn)) {
				return true;
			}

			// Knights and king.
			foreach (var offset in KNIGHT_OFFSETS) {
				if (isPieceOf(square + offset, attacker, PieceType.Knight)) return true;
			}
			foreach (var offset in KING_OFFSETS) {
				if (isPieceOf(square + offset, attacker, PieceType.King)) return true;
			}

			// Sliding pieces.
			foreach (var direction in KING_OFFSETS) {
				bool diagonal = direction.x != 0 && direction.y != 0;
				Vector2Int current = square + direction;
				while (IsValidPosition(current)) {
					Piece piece = GetPiece(current);
					if (piece != Piece.NULL_PIECE) {
						if (piece.Team == attacker
							&& (piece.Type == PieceType.Queen
								|| (diagonal && piece.Type == PieceType.Bishop)
								|| (!diagonal && piece.Type == PieceType.Rook))) {
							return true;
						}
						break;
					}
					current += direction;
				}
			}
			return false;
		}

		private bool isPieceOf(Vector2Int position, TeamType team, PieceType type) {
			Piece piece = GetPiece(position);
			return piece.Team == team && piece.Type == type;
		}

		private static readonly Vector2Int[] KNIGHT_OFFSETS = {
			new(2, 1), new(2, -1), new(1, 2), new(-1, 2), new(-2, 1), new(-2, -1), new(1, -2), new(-1, -2)
		};

		private static readonly Vector2Int[] KING_OFFSETS = {
			new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
		};

		/// <summary>
		/// A string that is identical for identical positions (pieces, disabled squares, castling pieces, turn).
		/// Used to detect repetitions.
		/// </summary>
		public string GetPositionKey() {
			StringBuilder key = new(Size.x * Size.y + 16);
			for (int y = 0; y < Size.y; y++) {
				for (int x = 0; x < Size.x; x++) {
					if (SquareMap[x, y] == SquareType.Disabled) {
						key.Append('#');
						continue;
					}
					Piece piece = GetPiece(new Vector2Int(x, y));
					if (piece == Piece.NULL_PIECE) {
						key.Append('.');
						continue;
					}
					char pieceChar = Piece.TypeAsChar(piece);
					// Unmoved kings and rooks can still castle, so they are a different position.
					bool unmovedCastlingPiece = piece.MoveCount == 0 && (piece.Type == PieceType.King || piece.Type == PieceType.Rook);
					if (unmovedCastlingPiece) pieceChar = pieceChar == 'k' ? 'x' : 'y';
					key.Append(piece.Team == TeamType.White ? char.ToUpper(pieceChar) : pieceChar);
				}
			}
			key.Append(IsWhitesTurn ? 'w' : 'b');
			return key.ToString();
		}

		public static Vector2Int GetTeamDirection(TeamType team) {
			return team switch {
				TeamType.White => Vector2Int.UP,
				TeamType.Black => Vector2Int.DOWN,
				_ => throw new NotImplementedException()
			};
		}

		public override string ToString() {
			string result = "";
			for (int y = 0; y < Size.y; y++) {
				for (int x = 0; x < Size.x; x++) {
					Vector2Int position = new(x, y);
					if (GetPiece(position) == Piece.NULL_PIECE) {
						result += ".";
					}
					else {
						result += Piece.TypeAsChar(GetPiece(position));
					}
				}
				result += "\n";
			}

			return result;
		}

	}

	public enum SquareType {
		Normal,
		Disabled
	}

	public enum TeamType {
		White,
		Black,
		None
	}
}
