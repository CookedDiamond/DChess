
using DChess;
using DChess.Chess;
using DChess.Chess.Variants;
using DChess.UI;
using DChess.UI.Scenes;
using DChess.Util;
using Microsoft.Xna.Framework;

var buttonManager = new ButtonManager();

var board = new Board(new Vector2Int(9, 9), buttonManager);
board.Build8x8StandardBoard();
board.Variants.Add(new VariantFriendlyFire());

var game = new Game1(board, buttonManager);
game.SwitchScene(GameScenes.Menu);
game.Run();

