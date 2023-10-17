using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces
{
    public class PieceKing : Piece {
		public PieceKing(TeamType team, Board board) : base(PieceType.King, team, board) {
		}

		public override List<Move> GetAllLegalMoves(Vector2Int fromPosition) {
			return ChessUtil.CombineLists(getKingMoves(fromPosition), base.GetAllLegalMoves(fromPosition));
		}

		private List<Move> getKingMoves(Vector2Int fromPosition) {
			List<Vector2Int> moves = new();

			for (int x = -1; x <= 1; x++) {
				for (int y = -1; y <= 1; y++) {
					if (x == 0 && y == 0) continue;
					Vector2Int currentMove = fromPosition + new Vector2Int(x, y);
					MoveType result = getMoveType(currentMove);
					if (result == MoveType.Empty || result == MoveType.Enemy) {
						moves.Add(currentMove);
					}
				}
			}

			return ChessUtil.CreateMoveListFromVectorList(moves, fromPosition, _board);
		}

	}
}
