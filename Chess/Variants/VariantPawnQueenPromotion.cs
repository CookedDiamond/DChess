using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Variants {
	public class VariantPawnQueenPromotion : Variant {

		public override void AfterTurnUpdate(Board board) {
			List<Square> promotionSquares = new();
			for (int i = 0; i < board.Size.x; i++) {
				promotionSquares.Add(board.GetSquare(new Vector2Int(i, 0)));
				promotionSquares.Add(board.GetSquare(new Vector2Int(i, board.Size.y - 1)));
			}

			foreach (var square in promotionSquares) {
				if (square.Piece != null && square.Piece.Type == PieceType.Pawn) {
					board.PlacePiece(square.Position, new PieceQueen(square.Piece.Team, board));
				}
			}
		}
	}
}
