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

		public override List<Move> GetAllLegalMoves(Square fromSquare) {
			return ChessUtil.CombineLists(getRookMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}



		private List<Move> getRookMoves(Square fromSquare) {
			List<Move> moves = new();

			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(-1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(0, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(0, -1)));

			return moves;
		}
	}
}
