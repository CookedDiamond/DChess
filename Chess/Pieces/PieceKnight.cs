using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces
{
    internal class PieceKnight : Piece {
		public PieceKnight(TeamType team, Board board) : base(PieceType.Knight, team, board) {
		}

		public override List<Move> GetAllLegalMoves(Vector2Int fromPosition) {
			return ChessUtil.CombineLists(getKnightMoves(fromPosition), base.GetAllLegalMoves(fromPosition));
		}

		private List<Move> getKnightMoves(Vector2Int fromPosition) {
			List<Vector2Int> moves = new();

			for (int i = 0; i < 8; i++) {
				Vector2Int offset = i switch {
					0 => new Vector2Int(2, 1),
					1 => new Vector2Int(2, -1),
					2 => new Vector2Int(1, 2),
					3 => new Vector2Int(-1, 2),
					4 => new Vector2Int(-2, 1),
					5 => new Vector2Int(-2, -1),
					6 => new Vector2Int(1, -2),
					7 => new Vector2Int(-1, -2),
					_ => throw new NotImplementedException()
				};

				Vector2Int currentMove = fromPosition + offset;

				MoveType result = getMoveType(currentMove);
				if (result == MoveType.Empty || result == MoveType.Enemy) {
					moves.Add(currentMove);
				}
			}

			return ChessUtil.CreateMoveListFromVectorList(moves, fromPosition, _board);
		}
	}
}
