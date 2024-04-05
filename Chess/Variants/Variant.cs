using DChess.Chess.Playground;
using DChess.Chess.Pieces;
using System.Collections.Generic;
using DChess.Util;

namespace DChess.Chess.Variants {
	/// <summary>
	/// Abstraction for a Variant.
	/// Variants are additions to the game which can alter/ enhance the gameplay.
	/// Variants should never be called directly. 
	/// Instead it should loop over the Variants list and execute the function for all of them.
	/// This approach improves code clarity, especially when multiple Variants are active.
	/// 
	/// Subclasses inheriting from this class should override at least one of the provided functions.
	/// If a subclass includes non-constant attributes, it is mandatory to implement the Clone() function 
	/// to ensure proper board cloning for AI purposes.
	/// </summary>
	public abstract class Variant {

		public virtual bool IsPieceEnemyTeam(bool normalResult, Piece piece) {
			return normalResult;
		}

		public virtual void AfterTurnUpdate(Board board, Move move) {

		}

		public virtual List<Move> AdditionalMoves(Board board, Piece piece, Vector2Int position) {
			return null;
		}

		public virtual Variant Clone() {
			return this;
		}
	}
}
