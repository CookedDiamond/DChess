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

		public override List<Move> GetAllLegalMoves(Square fromSquare) {
			return ChessUtil.CombineLists(getBishopMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}

		private List<Move> getBishopMoves(Square fromSquare) {
			List<Move> moves = new();

			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(1, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(-1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(-1, 1)));

			return moves;
		}
	}
}
