
using DChess;
using DChess.Chess;
using DChess.UI;
using DChess.Util;
using Microsoft.Xna.Framework;

var buttonManager = new ButtonManager();

var board = new Board(new Vector2Int(10, 10), buttonManager);
board.Build8x8StandardBoard();

using var game = new Game1(board, buttonManager);
game.Run();
