using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DChess.BotApi {
	/// <summary>
	/// A selectable player: a bot class or a human.
	/// </summary>
	public class BotInfo {
		public string Name { get; }
		public Type Type { get; }
		public bool IsHuman => Type == typeof(HumanPlayer);

		public BotInfo(string name, Type type) {
			Name = name;
			Type = type;
		}

		/// <summary>Creates a fresh instance of the bot (one per game).</summary>
		public IChessBot Create() {
			return (IChessBot)Activator.CreateInstance(Type);
		}

		public override string ToString() => Name;
	}

	/// <summary>
	/// Finds all bots in the project: every non-abstract class implementing IChessBot with a parameterless constructor.
	/// </summary>
	public static class BotRegistry {
		private static List<BotInfo> _bots;

		public static readonly BotInfo Human = new("Human", typeof(HumanPlayer));

		/// <summary>All bots, sorted by name.</summary>
		public static IReadOnlyList<BotInfo> Bots => _bots ??= discoverBots();

		/// <summary>Human first, then all bots.</summary>
		public static IReadOnlyList<BotInfo> AllPlayers => new[] { Human }.Concat(Bots).ToList();

		/// <summary>Finds a bot by its name or class name (case insensitive), or null.</summary>
		public static BotInfo Find(string name) {
			return AllPlayers.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase))
				?? AllPlayers.FirstOrDefault(b => string.Equals(b.Type.Name, name, StringComparison.OrdinalIgnoreCase));
		}

		private static List<BotInfo> discoverBots() {
			var types = Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => typeof(IChessBot).IsAssignableFrom(t)
					&& t.IsClass
					&& !t.IsAbstract
					&& t != typeof(HumanPlayer)
					&& t.GetConstructor(Type.EmptyTypes) != null);

			List<BotInfo> bots = new();
			foreach (var type in types) {
				string name;
				try {
					name = ((IChessBot)Activator.CreateInstance(type)).Name;
				}
				catch (Exception e) {
					Console.WriteLine($"Bot {type.Name}: constructor failed: {e.InnerException?.Message ?? e.Message}");
					name = null;
				}
				if (string.IsNullOrWhiteSpace(name)) name = type.Name;
				if (bots.Any(b => b.Name == name) || name == Human.Name) name = $"{name} ({type.Name})";
				bots.Add(new BotInfo(name, type));
			}
			return bots.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
		}
	}
}
