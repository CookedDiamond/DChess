using DChess.BotApi;
using System;
using System.Globalization;
using System.Linq;

namespace DChess.Chess.Arena {
	/// <summary>
	/// Plays bot matches without a window, e.g.:
	///   dotnet run -- --arena MinMaxBot GreedyBot --games 10 --time 500
	/// </summary>
	public static class ConsoleArena {
		public static bool IsArenaCommand(string[] args) {
			return args.Length > 0 && (args[0] == "--arena" || args[0] == "--list");
		}

		public static int Run(string[] args) {
			if (args[0] == "--list") {
				Console.WriteLine("Available bots:");
				foreach (var bot in BotRegistry.Bots) {
					Console.WriteLine($"  {bot.Name}  ({bot.Type.FullName})");
				}
				return 0;
			}

			if (args.Length < 3) {
				printUsage();
				return 1;
			}

			var settings = new MatchSettings {
				Player1 = BotRegistry.Find(args[1]),
				Player2 = BotRegistry.Find(args[2]),
			};
			bool printMoves = false;
			try {
				for (int i = 3; i < args.Length; i++) {
					switch (args[i]) {
						case "--games":
							settings.Games = int.Parse(args[++i]);
							break;
						case "--time":
							settings.TimeLimitMilliseconds = int.Parse(args[++i]);
							break;
						case "--max-moves":
							settings.MaxPlies = int.Parse(args[++i]) * 2;
							break;
						case "--moves":
							printMoves = true;
							break;
						default:
							throw new ArgumentException($"Unknown option {args[i]}");
					}
				}
			}
			catch (Exception e) when (e is FormatException || e is IndexOutOfRangeException || e is ArgumentException) {
				Console.WriteLine(e.Message);
				printUsage();
				return 1;
			}

			foreach (var (player, name) in new[] { (settings.Player1, args[1]), (settings.Player2, args[2]) }) {
				if (player == null || player.IsHuman) {
					Console.WriteLine($"Unknown bot '{name}'. Available: {string.Join(", ", BotRegistry.Bots.Select(b => b.Name))}");
					return 1;
				}
			}

			var match = new Match(settings);
			Console.WriteLine($"{match.PlayerNames[0]} vs {match.PlayerNames[1]}: {settings.Games} games, {settings.TimeLimitMilliseconds} ms per move");
			match.GameFinished += record => {
				Console.WriteLine($"Game {record.Number}: {record.WhiteName} (white) vs {record.BlackName} (black): " +
					$"{record.ResultScore}  {record.ResultText}  [{record.PositionCount - 1} half-moves]");
				if (printMoves) {
					Console.WriteLine("  " + record.GetMoveListText());
				}
				Console.WriteLine($"  Score: {match.PlayerNames[0]} {score(match, 0)} - {score(match, 1)} {match.PlayerNames[1]}");
			};
			match.Run();

			Console.WriteLine();
			Console.WriteLine($"Final score: {match.PlayerNames[0]} {score(match, 0)} - {score(match, 1)} {match.PlayerNames[1]}");
			Console.WriteLine($"  {match.PlayerNames[0]} won {match.GetWins(0)}, {match.PlayerNames[1]} won {match.GetWins(1)}, {match.Draws} draws");
			return 0;
		}

		private static string score(Match match, int player) {
			return match.GetScore(player).ToString("0.#", CultureInfo.InvariantCulture);
		}

		private static void printUsage() {
			Console.WriteLine("Usage:");
			Console.WriteLine("  DChess --list");
			Console.WriteLine("  DChess --arena <bot1> <bot2> [--games N] [--time MS_PER_MOVE] [--max-moves N] [--moves]");
		}
	}
}
