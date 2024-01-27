using DChess.Chess.Playground;
using DChess.Chess.Pieces;
using System.Collections.Generic;
using DChess.Util;

namespace DChess.Chess.Variants
{
    public abstract class Variant {

		public virtual bool IsPieceEnemyTeam(bool normalResult, Piece piece) {
			return normalResult;
		}

		public virtual void AfterTurnUpdate(Board board) {

		}

		public virtual List<Move> AdditionalMoves(Board board, Piece piece, Vector2Int position) {
			return null;
		}

		public virtual Variant Clone() {
			return this;
		}
	}
}
