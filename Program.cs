using DChess;
using DChess.Chess.Arena;
using DChess.Cli;
using System;

// Headless text interface to the engine, e.g. "dotnet run -- cli".
if (args.Length > 0 && (args[0] == "cli" || args[0] == "--cli")) {
	// Needed if the project is built as WinExe (no console is attached then); harmless for Exe.
	CliConsole.AttachToParent();
	var rest = new string[args.Length - 1];
	Array.Copy(args, 1, rest, 0, rest.Length);
	return CliRunner.Run(rest);
}

// Command line bot matches without a window, e.g. "dotnet run -- --arena MinMaxBot GreedyBot --games 10".
if (ConsoleArena.IsArenaCommand(args)) {
	return ConsoleArena.Run(args);
}

// "--smoke-test" draws ten frames and exits (checks that content loads and rendering works).
using var game = new Game1(Array.IndexOf(args, "--smoke-test") >= 0 ? 10 : 0);
game.Run();
return 0;
