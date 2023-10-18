using DChess.Chess.Playground;
using DChess.Chess.Pieces;

namespace DChess.Chess.Variants
{
    public abstract class Variant {

		public virtual bool IsPieceEnemyTeam(bool normalResult, Piece piece) {
			return normalResult;
		}

		public virtual void AfterTurnUpdate(Board board) {

		}

		public virtual Variant Clone() {
			return this;
		}
	}
}
