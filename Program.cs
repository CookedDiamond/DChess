using DChess;
using DChess.Chess.Arena;

// Command line bot matches without a window, e.g. "DChess --arena MinMaxBot GreedyBot --games 10".
if (ConsoleArena.IsArenaCommand(args)) {
	return ConsoleArena.Run(args);
}

using var game = new Game1();
game.Run();
return 0;
