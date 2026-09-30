
using DChess;
using DChess.Chess.ChessAI;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Cli;
using DChess.Util;

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
//board.Variants.Add(new VariantFriendlyFire());
//board.Variants.Add(new VariantBattleRoyale(15, 1f));

BoardManager boardManager = new(board, new BoardNetworking());
boardManager.Build8x8StandardBoard();
//boardManager.AddComputerPlayer(TeamType.Black);

//board.PlacePiece(new Vector2Int(1,1), new PieceQueen(TeamType.White, board));
//board.PlacePiece(new Vector2Int(1,0), new PieceRook(TeamType.White, board));
//board.PlacePiece(new Vector2Int(0,0), new PieceKing(TeamType.White, board));

//board.PlacePiece(new Vector2Int(1,7), new PieceKing(TeamType.Black, board));
//board.PlacePiece(new Vector2Int(1,6), new PiecePawn(TeamType.Black, board));


board.LastEval = new Evaluation(board).GetEvaluation();

var game = new Game1(boardManager);
game.SwitchScene(SceneType.Board);
game.Run();
