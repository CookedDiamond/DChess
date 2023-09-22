using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces {
	public class PieceQueen : Piece {
		public PieceQueen(TeamType team, Board board) : base(PieceType.Queen, team, board) {
		}

		public override List<Vector2Int> GetAllLegalMoves(Square fromSquare) {
			return MoveHelper.CombineMoves(getQueenMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}

		private List<Vector2Int> getQueenMoves(Square fromSquare) {
			List<Vector2Int> moves = new();

			// Bishop moves.
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(1, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(-1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(-1, 1)));

			// Rook moves.
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(-1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(0, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(0, -1)));

			return MoveHelper.CombineMoves(moves, base.GetAllLegalMoves(fromSquare));
		}

	}
}
