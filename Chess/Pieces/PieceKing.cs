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

		public override List<Move> GetAllLegalMoves(Square fromSquare) {
			return ChessUtil.CombineLists(getKingMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}

		private List<Move> getKingMoves(Square fromSquare) {
			List<Vector2Int> moves = new();

			for (int x = -1; x <= 1; x++) {
				for (int y = -1; y <= 1; y++) {
					if (x == 0 && y == 0) continue;
					Vector2Int currentMove = fromSquare.Position + new Vector2Int(x, y);
					MoveType result = getMoveType(fromSquare.Piece, currentMove);
					if (result == MoveType.Empty || result == MoveType.Enemy) {
						moves.Add(currentMove);
					}
				}
			}

			return ChessUtil.CreateMoveListFromVectorList(moves, fromSquare.Position, _board);
		}

	}
}
