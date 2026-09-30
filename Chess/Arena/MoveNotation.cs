using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Arena {
	/// <summary>
	/// Short algebraic notation (Nf3, exd5, O-O, e8=Q, Qxe8#) for display.
	/// "#" marks a king capture, "+" a king that can be captured next move.
	/// </summary>
	public static class MoveNotation {
		/// <param name="move">The move, before it is made.</param>
		/// <param name="legalMoves">All legal moves in the position (for disambiguation like Nbd2).</param>
		public static string Describe(Move move, List<Move> legalMoves) {
			if (move.IsCastling) {
				return move.To.x > move.From.x ? "O-O" : "O-O-O";
			}

			Piece piece = move.MovingPiece;
			string destination = move.To.ToSquareName();
			string capture = move.IsCapture ? "x" : "";

			if (piece.Type == PieceType.Pawn) {
				string pawnText = move.IsCapture ? $"{fileName(move.From.x)}x{destination}" : destination;
				return move.IsPromotion ? pawnText + "=Q" : pawnText;
			}

			// Another piece of the same kind that can go to the same square?
			var ambiguous = legalMoves.Where(other => other.To == move.To
				&& other.From != move.From
				&& other.MovingPiece.Type == piece.Type
				&& other.MovingPiece.Team == piece.Team
				&& !other.IsCastling).ToList();

			string disambiguation = "";
			if (ambiguous.Count > 0) {
				if (ambiguous.All(other => other.From.x != move.From.x)) {
					disambiguation = fileName(move.From.x).ToString();
				}
				else if (ambiguous.All(other => other.From.y != move.From.y)) {
					disambiguation = (move.From.y + 1).ToString();
				}
				else {
					disambiguation = move.From.ToSquareName();
				}
			}

			return $"{char.ToUpper(Piece.TypeAsChar(piece))}{disambiguation}{capture}{destination}";
		}

		/// <summary>Suffix after the move was made: "#" if a king was captured, "+" if the opponent's king is attacked.</summary>
		public static string Suffix(Board boardAfterMove) {
			if (boardAfterMove.HasTeamWon() != TeamType.None) return "#";

			TeamType toMove = boardAfterMove.GetTurnTeamType();
			TeamType opponent = toMove == TeamType.White ? TeamType.Black : TeamType.White;
			foreach (var king in boardAfterMove.GetPiecesFromTeamWithType(PieceType.King, toMove)) {
				if (boardAfterMove.IsSquareAttackedBy(king.Key, opponent)) return "+";
			}
			return "";
		}

		private static char fileName(int x) => (char)('a' + x);
	}
}
