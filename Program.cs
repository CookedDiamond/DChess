
using DChess;
using DChess.Chess;
using DChess.Chess.Variants;
using DChess.UI;
using DChess.Util;
using Microsoft.Xna.Framework;

var buttonManager = new ButtonManager();

var board = new Board(new Vector2Int(10, 10), buttonManager);
board.Build8x8StandardBoard();
board.Variants.Add(new VariantFriendlyFire());

using var game = new Game1(board, buttonManager);
game.Run();
