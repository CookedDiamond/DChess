using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Playground
{
	/// <summary>
	/// A move is a list of square changes (old piece -> new piece).
	/// A normal move has two changes: the origin square is emptied and the destination gets the piece.
	/// Castling has four changes, promotion adds one more change after the move was made.
	/// </summary>
	public class Move
	{
		public readonly List<BoardChange> Changes = new();

		public void AddChange(BoardChange change)
		{
			Changes.Add(change);
		}

		public void AddChange(Vector2Int position, Piece oldPiece, Piece newPiece)
		{
			Changes.Add(new BoardChange(position, oldPiece, newPiece));
		}

		public Move Copy()
		{
			Move copy = new();
			copy.Changes.AddRange(Changes);
			return copy;
		}

		public void Apply(Board board)
		{
			foreach (var change in Changes)
			{
				applyChange(board, change);
			}
		}

		/// <summary>
		/// Adds a change to this (already applied) move and applies it to the board.
		/// </summary>
		public void AddAndApplyChange(Board board, BoardChange change)
		{
			Changes.Add(change);
			applyChange(board, change);
		}

		public void Undo(Board board)
		{
			foreach (var change in Changes.Reverse<BoardChange>())
			{
				board.PlacePiece(change.boardPosition, change.oldPiece);

				if (leavesSquare(change))
				{
					change.oldPiece.MoveCount -= 1;
				}
			}
		}

		private static void applyChange(Board board, BoardChange change)
		{
			board.PlacePiece(change.boardPosition, change.newPiece);

			if (leavesSquare(change))
			{
				change.oldPiece.MoveCount += 1;
			}
		}

		// A piece counts as moved when it leaves its square. Apply and Undo must use the same rule.
		private static bool leavesSquare(BoardChange change)
		{
			return change.oldPiece != Piece.NULL_PIECE && change.newPiece == Piece.NULL_PIECE;
		}

		// ------------------------------------------------------------------------------------
		// Convenience information (mainly for bots and move notation).
		// ------------------------------------------------------------------------------------

		/// <summary>The square the moving piece starts on.</summary>
		public Vector2Int From => fromChange()?.boardPosition ?? Vector2Int.ZERO;

		/// <summary>The square the moving piece ends on (for castling: the king's destination).</summary>
		public Vector2Int To => toChange()?.boardPosition ?? Vector2Int.ZERO;

		/// <summary>The piece that moves (for castling: the king).</summary>
		public Piece MovingPiece => fromChange()?.oldPiece ?? Piece.NULL_PIECE;

		/// <summary>The piece that gets captured, or Piece.NULL_PIECE (Type == PieceType.None).</summary>
		public Piece CapturedPiece => toChange()?.oldPiece ?? Piece.NULL_PIECE;

		public bool IsCapture => CapturedPiece != Piece.NULL_PIECE;

		/// <summary>True when more than one piece leaves its square (king + rook).</summary>
		public bool IsCastling => Changes.Count(leavesSquare) > 1;

		/// <summary>True when a pawn reaches the last rank with this move (it becomes a queen).</summary>
		public bool IsPromotion => MovingPiece is PiecePawn pawn && pawn.PromotesOn(To);

		private BoardChange fromChange()
		{
			foreach (var change in Changes)
			{
				if (leavesSquare(change)) return change;
			}
			return null;
		}

		private BoardChange toChange()
		{
			Piece moving = MovingPiece;
			if (moving == Piece.NULL_PIECE) return null;
			foreach (var change in Changes)
			{
				if (change.newPiece == moving) return change;
			}
			return null;
		}

		/// <returns>The value of the attacked Piece.</returns>
		public float AttackScore()
		{
			float maxAttack = 0;
			foreach (var change in Changes)
			{
				if (change.newPiece != Piece.NULL_PIECE && change.oldPiece != Piece.NULL_PIECE)
				{
					float pieceScore = change.oldPiece.GetPieceScore();
					if (pieceScore < maxAttack) continue;

					maxAttack = pieceScore;
				}
			}
			return maxAttack;
		}

		/// <summary>
		/// True if both moves do the same thing on the board (same squares, same piece types and teams).
		/// Unlike Equals this also works for moves that were created on different (cloned) boards.
		/// </summary>
		public bool IsEquivalentTo(Move other)
		{
			if (other == null || other.Changes.Count != Changes.Count) return false;

			bool[] used = new bool[other.Changes.Count];
			foreach (var change in Changes)
			{
				bool found = false;
				for (int i = 0; i < other.Changes.Count; i++)
				{
					if (used[i]) continue;
					var otherChange = other.Changes[i];
					if (otherChange.boardPosition == change.boardPosition
						&& samePieceKind(otherChange.oldPiece, change.oldPiece)
						&& samePieceKind(otherChange.newPiece, change.newPiece))
					{
						used[i] = true;
						found = true;
						break;
					}
				}
				if (!found) return false;
			}
			return true;
		}

		private static bool samePieceKind(Piece a, Piece b)
		{
			return a.Type == b.Type && a.Team == b.Team;
		}

		public override bool Equals(object obj)
		{
			if (obj is not Move move || move.Changes.Count != Changes.Count)
			{
				return false;
			}

			foreach (var change in Changes) {
				if (!move.Changes.Contains(change))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			int hash = 0;
			foreach (var change in Changes)
			{
				hash ^= change.GetHashCode();
			}
			return hash;
		}

		/// <summary>Move in "from-to" notation, e.g. "e2e4". Promotions end with "q".</summary>
		public override string ToString()
		{
			if (Changes.Count == 0) return "(empty move)";
			string result = From.ToSquareName() + To.ToSquareName();
			if (IsPromotion)
			{
				result += "q";
			}
			return result;
		}
	}
}
