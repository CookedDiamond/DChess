using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Util;

namespace DChess.Chess.Arena {
	/// <summary>
	/// Creates the starting position used for games.
	/// </summary>
	public static class BoardSetup {
		public static Board CreateStandardBoard() {
			var board = new Board(new Vector2Int(8, 8));
			board.Variants.Add(new VariantPawnQueenPromotion());
			board.Variants.Add(new VariantCastling(2));
			PlaceStandardPieces(board);
			return board;
		}

		public static void PlaceStandardPieces(Board board) {
			int top = board.Size.y - 1;
			for (int i = 0; i < board.Size.x; i++) {
				board.PlacePiece(new Vector2Int(i, 1), new PiecePawn(TeamType.White, board));
				board.PlacePiece(new Vector2Int(i, top - 1), new PiecePawn(TeamType.Black, board));
				if (i == 0 || i == board.Size.x - 1) {
					board.PlacePiece(new Vector2Int(i, 0), new PieceRook(TeamType.White, board));
					board.PlacePiece(new Vector2Int(i, top), new PieceRook(TeamType.Black, board));
				}
				else if (i == 2 || i == board.Size.x - 3) {
					board.PlacePiece(new Vector2Int(i, 0), new PieceBishop(TeamType.White, board));
					board.PlacePiece(new Vector2Int(i, top), new PieceBishop(TeamType.Black, board));
				}
				else if (i == 1 || i == board.Size.x - 2) {
					board.PlacePiece(new Vector2Int(i, 0), new PieceKnight(TeamType.White, board));
					board.PlacePiece(new Vector2Int(i, top), new PieceKnight(TeamType.Black, board));
				}

				if (i == 3) {
					board.PlacePiece(new Vector2Int(i, 0), new PieceQueen(TeamType.White, board));
					board.PlacePiece(new Vector2Int(i, top), new PieceQueen(TeamType.Black, board));
				}
				if (i == board.Size.x - 4) {
					board.PlacePiece(new Vector2Int(i, 0), new PieceKing(TeamType.White, board));
					board.PlacePiece(new Vector2Int(i, top), new PieceKing(TeamType.Black, board));
				}
			}
		}
	}
}
