
using DChess;
using DChess.Chess;
using DChess.Chess.Variants;
using DChess.UI;
using DChess.UI.Scenes;
using DChess.Util;
using Microsoft.Xna.Framework;
using System;

Console.Read();

var board = new Board(new Vector2Int(6, 6));
board.Build8x8StandardBoard();
board.Variants.Add(new VariantPawnQueenPromotion());
// board.MakeComputerMove();

var game = new Game1(board);
game.SwitchScene(SceneType.Board);
game.Run();

