using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces {
	public class PieceBishop : Piece {
		public PieceBishop(TeamType team, Board board) : base(PieceType.Bishop, team, board) {
		}

		public override List<Vector2Int> GetAllLegalMoves(Square fromSquare) {
			return MoveHelper.CombineMoves(getBishopMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}

		private List<Vector2Int> getBishopMoves(Square fromSquare) {
			List<Vector2Int> moves = new();

			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(1, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(-1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, _board, new Vector2Int(-1, 1)));

			return moves;
		}
	}
}
