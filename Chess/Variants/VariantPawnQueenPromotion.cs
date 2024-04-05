using DChess.Chess.Playground;
using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Variants {
	public class VariantPawnQueenPromotion : Variant {

		public override void AfterTurnUpdate(Board board, Move move) {
			foreach (var change in move.Changes) {
				if (change.newPiece.Type == PieceType.Pawn) {
					tryToPromotePawn(change.newPiece, change.boardPosition, board);
					break;
				}
			}
		}

		private void tryToPromotePawn(Piece pawn, Vector2Int pawnPosition, Board board) {
			List<BoardChange> changes = new();
			TeamType team = pawn.Team;
			Vector2Int direction = Board.GetTeamDirection(team);
			Vector2Int nextSquare = pawnPosition + direction;
			if (board.IsValidPosition(nextSquare)) return;

			changes.Add(new BoardChange(pawnPosition, pawn, new PieceQueen(team, board)));

			board.AddToLastMove(changes);
		}
	}
}
