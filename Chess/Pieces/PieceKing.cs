using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces {
	public class PieceKing : Piece {
		public PieceKing(TeamType team, Board board) : base(PieceType.King, team, board) {
		}

		public override List<Vector2Int> GetAllLegalMoves(Square fromSquare) {
			return MoveHelper.CombineMoves(getKingMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}

		private List<Vector2Int> getKingMoves(Square fromSquare) {
			List<Vector2Int> moves = new();

			for (int x = -1; x <= 1; x++) {
				for (int y = -1; y <= 1; y++) {
					if (x == 0 && y == 0) continue;
					Vector2Int currentMove = fromSquare.position + new Vector2Int(x, y);
					MoveType result = getMoveType(fromSquare.piece, currentMove);
					if (result == MoveType.Empty || result == MoveType.Enemy) {
						moves.Add(currentMove);
					}
				}
			}

			return moves;
		}

	}
}
