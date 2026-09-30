
using DChess;
using DChess.Chess.ChessAI;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Cli;
using DChess.Util;
using System;
using System.Diagnostics;

if (args.Length > 0 && (args[0] == "cli" || args[0] == "--cli")) {
	// This is a WinExe (GUI subsystem) — Windows doesn't attach a console
	// when launched from a terminal, so Console.* would silently no-op.
	// Reattach to the parent process's console (PowerShell/cmd) so the
	// REPL is actually visible and ReadLine actually blocks for input.
	CliConsole.AttachToParent();
	var rest = new string[args.Length - 1];
	System.Array.Copy(args, 1, rest, 0, rest.Length);
	System.Environment.Exit(CliRunner.Run(rest));
	return;
}

var board = new Board(new Vector2Int(8, 8));
board.Variants.Add(new VariantPawnQueenPromotion());
board.Variants.Add(new VariantCastling(2));
//board.Variants.Add(new VariantFriendlyFire());
//board.Variants.Add(new VariantBattleRoyale(2, 0.5f));

BoardManager boardManager = new(board, new BoardNetworking());
boardManager.Build8x8StandardBoard();

using var game = new Game1(boardManager, Array.IndexOf(args, "--smoke-test") >= 0 ? 10 : 0);
game.SwitchScene(SceneType.Board);
game.Run();
