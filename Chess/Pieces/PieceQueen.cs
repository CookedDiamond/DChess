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

		public override List<Move> GetAllLegalMoves(Square fromSquare) {
			return ChessUtil.CombineLists(getQueenMoves(fromSquare), base.GetAllLegalMoves(fromSquare));
		}

		private List<Move> getQueenMoves(Square fromSquare) {
			List<Move> moves = new();

			// Bishop moves.
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(1, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(-1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(-1, 1)));

			// Rook moves.
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(-1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(0, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, new Vector2Int(0, -1)));

			return ChessUtil.CombineLists(moves, base.GetAllLegalMoves(fromSquare));
		}

	}
}
