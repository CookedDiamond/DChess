
using DChess;
using DChess.Chess.ChessAI;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Util;
using System;
using System.Diagnostics;

var board = new Board(new Vector2Int(6, 6));
board.Variants.Add(new VariantPawnQueenPromotion());
board.Variants.Add(new VariantCastling(2));
//board.Variants.Add(new VariantFriendlyFire());
board.Variants.Add(new VariantBattleRoyale(2, 0.5f));

BoardManager boardManager = new(board, new BoardNetworking());
//boardManager.Build8x8StandardBoard();
//boardManager.AddComputerPlayer(TeamType.Black);

//board.PlacePiece(new Vector2Int(3,0), new PieceQueen(TeamType.White, board));
board.PlacePiece(new Vector2Int(1,0), new PieceKing(TeamType.White, board));
//board.PlacePiece(new Vector2Int(0,0), new PieceRook(TeamType.White, board));
board.PlacePiece(new Vector2Int(0,0), new PiecePawn(TeamType.White, board));

board.PlacePiece(new Vector2Int(3,3), new PieceKing(TeamType.Black, board));
//board.PlacePiece(new Vector2Int(0, 7), new PieceRook(TeamType.Black, board));
//board.PlacePiece(new Vector2Int(7, 7), new PieceRook(TeamType.Black, board));
//board.PlacePiece(new Vector2Int(1,3), new PiecePawn(TeamType.Black, board));

var game = new Game1(boardManager);
game.SwitchScene(SceneType.Board);
game.Run();
