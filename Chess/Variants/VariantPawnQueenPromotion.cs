using DChess.Chess.Playground;
using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Variants
{
    public class VariantPawnQueenPromotion : Variant {

		public override void AfterTurnUpdate(Board board) {
			List<Vector2Int> promotionPositions = new();
			for (int i = 0; i < board.Size.x; i++) {
				promotionPositions.Add(new Vector2Int(i, 0));
				promotionPositions.Add(new Vector2Int(i, board.Size.y - 1));
			}

			foreach (var position in promotionPositions) {
				var piece = board.GetPiece(position);
				if (piece.Type == PieceType.Pawn) {
					board.PlacePiece(position, new PieceQueen(piece.Team, board));
				}
			}
		}
	}
}
