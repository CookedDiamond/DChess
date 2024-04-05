using DChess.Chess.Playground;
using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Variants {
	public class VariantPawnQueenPromotion : Variant {

		public override void AfterTurnUpdate(Board board, Move move) {
			bool pawnMove = false;
			foreach (var change in move.Changes) {
				if (change.newPiece.Type == PieceType.Pawn) {
					pawnMove = true;
				}
			}

			if (!pawnMove) return;

			var dictionary = board.GetPieceDictionary();
			List<BoardChange> changes = new ();

			foreach (var entry in dictionary) {
				if (entry.Value.Type != PieceType.Pawn) continue;
				TeamType team = entry.Value.Team;
				Vector2Int direction = Board.GetTeamDirection(team);
				Vector2Int position = entry.Key;
				Vector2Int nextSquare = position + direction;
				if (board.IsValidPosition(nextSquare)) continue;

				changes.Add(new BoardChange(position, entry.Value, new PieceQueen(team, board)));
			}
			board.AddToLastMove(changes);
		}
	}
}
