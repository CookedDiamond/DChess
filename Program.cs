
using DChess.Chess;
using DChess.Util;

var board = new Board(new Vector2Int(8, 8));
board.PlacePiece(new Vector2Int(0, 0), new Piece(PieceType.Pawn, TeamType.White));
board.PlacePiece(new Vector2Int(1, 0), new Piece(PieceType.Queen, TeamType.White));
board.PlacePiece(new Vector2Int(1, 1), new Piece(PieceType.King, TeamType.White));
board.PlacePiece(new Vector2Int(1, 2), new Piece(PieceType.Bishop, TeamType.White));
board.PlacePiece(new Vector2Int(1, 3), new Piece(PieceType.Rook, TeamType.White));
board.PlacePiece(new Vector2Int(5, 3), new Piece(PieceType.Rook, TeamType.White));
board.PlacePiece(new Vector2Int(7, 3), new Piece(PieceType.Knight, TeamType.White));
using var game = new DChess.Game1(board);
game.Run();
