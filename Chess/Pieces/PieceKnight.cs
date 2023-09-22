using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces {
	internal class PieceKnight : Piece {
		public PieceKnight(TeamType team, Board board) : base(PieceType.Knight, team, board) {
		}

		public override List<Vector2Int> GetAllLegalMoves(Square fromSquare) {
			return MoveHelper.CombineMoves(getKnightMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}

		private List<Vector2Int> getKnightMoves(Square fromSquare) {
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

				Vector2Int currentMove = fromSquare.position + offset;

				MoveType result = getMoveType(fromSquare.piece, currentMove);
				if (result == MoveType.Empty || result == MoveType.Enemy) {
					moves.Add(currentMove);
				}
			}

			return moves;
		}
	}
}
