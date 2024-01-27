using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces
{
    public class PieceQueen : Piece {
		public PieceQueen(TeamType team, Board board) : base(PieceType.Queen, team, board) {
		}

		public override List<Move> GetAllLegalMoves(Vector2Int fromPosition) {
			return ChessUtil.CombineLists(getQueenMoves(fromPosition), base.GetAllLegalMoves(fromPosition));
		}

		public override float GetPieceScore() {
			return 9;
		}

		public override float GetPieceScore(Board board, Vector2Int position, TeamType team) {
			return GetPieceScore();
		}

		private List<Move> getQueenMoves(Vector2Int fromPosition) {
			List<Move> moves = new();

			// Bishop moves.
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(1, 1)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(1, -1)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(-1, -1)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(-1, 1)));

			// Rook moves.
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(1, 0)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(-1, 0)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(0, 1)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(0, -1)));

			return ChessUtil.CombineLists(moves, base.GetAllLegalMoves(fromPosition));
		}

	}
}
