
using DChess;
using DChess.Chess;
using DChess.Chess.Pieces;
using DChess.Chess.Variants;
using DChess.UI;
using DChess.UI.Scenes;
using DChess.Util;
using Microsoft.Xna.Framework;
using System;

Console.Read();

var board = new Board(new Vector2Int(7, 7));
board.Variants.Add(new VariantPawnQueenPromotion());
board.Variants.Add(new VariantFriendlyFire());
board.Variants.Add(new VariantBattleRoyale(5, 0.8f));
board.BuildSmallBoard();

//board.PlacePiece(new Vector2Int(1,0), new PieceQueen(TeamType.White, board));
//board.PlacePiece(new Vector2Int(0,0), new PieceKing(TeamType.White, board));
//board.PlacePiece(new Vector2Int(2,3), new PieceKing(TeamType.Black, board));

var game = new Game1(board);
game.SwitchScene(SceneType.Board);
game.Run();

