using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces {
	public class PieceRook : Piece {
		public PieceRook(TeamType team, Board board) : base(PieceType.Rook, team, board) {
		}

		public override List<Vector2Int> GetAllLegalMoves(Square fromSquare) {
			return MoveHelper.CombineMoves(getRookMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}



		private List<Vector2Int> getRookMoves(Square fromSquare) {
			List<Vector2Int> moves = new();

			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(-1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(0, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(0, -1)));

			return moves;
		}
	}
}
