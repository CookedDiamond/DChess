using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces {
	public class PieceNull : Piece {
		public PieceNull() : base(PieceType.None, TeamType.None, null) {
		}

		public override List<Move> GetAllLegalMoves(Vector2Int fromPosition) => new();

		public override float GetPieceScore() {
			return 0;
		}

		public override float GetPieceScore(Board board, Vector2Int position, TeamType team) {
			return 0;
		}
	}
}
