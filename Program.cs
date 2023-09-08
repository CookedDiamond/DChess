
using DChess.Chess;
using DChess.Util;

var board = new Board(new Vector2Int(8, 8));
for (int i = 0; i < 8; i++) {
	board.PlacePiece(new Vector2Int(i, 1), new Piece(PieceType.Pawn, TeamType.White));
	board.PlacePiece(new Vector2Int(i, 6), new Piece(PieceType.Pawn, TeamType.Black));
	if (i == 0 || i == 7) {
		board.PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.Rook, TeamType.White));
		board.PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.Rook, TeamType.Black));
	}
	else if (i == 1 || i == 6) {
		board.PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.Bishop, TeamType.White));
		board.PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.Bishop, TeamType.Black));
	}
	else if (i == 2 || i == 5) {
		board.PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.Knight, TeamType.White));
		board.PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.Knight, TeamType.Black));
	}
	else if (i == 3) {
		board.PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.Queen, TeamType.White));
		board.PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.Queen, TeamType.Black));
	}
	else if (i == 4) {
		board.PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.King, TeamType.White));
		board.PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.King, TeamType.Black));
	}
}
using var game = new DChess.Game1(board);
game.Run();
