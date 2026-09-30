using DChess.BotApi;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DChess.Chess.Arena {
	public class MatchSettings {
		public BotInfo Player1 { get; set; }
		public BotInfo Player2 { get; set; }
		public int Games { get; set; } = 2;
		public int TimeLimitMilliseconds { get; set; } = 1000;

		/// <summary>Player 1 plays white in game 1, black in game 2, ...</summary>
		public bool AlternateColors { get; set; } = true;

		/// <summary>Games are a draw after this many half-moves.</summary>
		public int MaxPlies { get; set; } = 500;
	}

	/// <summary>
	/// Plays a series of games between two players (bots or humans) and records them.
	/// Runs on its own thread (Start) or synchronously (Run).
	///
	/// Rules: you win by capturing the enemy king (there is no check or checkmate).
	/// Draws: no legal moves, 50 moves without capture or pawn move, threefold repetition,
	/// insufficient material and the move limit.
	/// </summary>
	public class Match {
		private readonly object _lock = new();
		private readonly List<GameRecord> _games = new();
		private readonly CancellationTokenSource _cancellation = new();
		private readonly double[] _scores = new double[2];
		private readonly int[] _wins = new int[2];
		private int _draws;
		private volatile IChessBot[] _currentPlayers = new IChessBot[0];

		public MatchSettings Settings { get; }

		/// <summary>Display names of player 1 and 2 (numbered if both are the same bot).</summary>
		public string[] PlayerNames { get; }

		public bool IsFinished { get; private set; }
		public bool IsCancelled => _cancellation.IsCancellationRequested;

		/// <summary>Called on the match thread after every game.</summary>
		public event Action<GameRecord> GameFinished;

		public Match(MatchSettings settings) {
			Settings = settings;
			string name1 = settings.Player1.Name;
			string name2 = settings.Player2.Name;
			PlayerNames = name1 == name2
				? new[] { $"{name1} (1)", $"{name2} (2)" }
				: new[] { name1, name2 };
		}

		public List<GameRecord> Games {
			get { lock (_lock) return _games.ToList(); }
		}

		public double GetScore(int playerIndex) {
			lock (_lock) return _scores[playerIndex];
		}

		public int GetWins(int playerIndex) {
			lock (_lock) return _wins[playerIndex];
		}

		public int Draws {
			get { lock (_lock) return _draws; }
		}

		/// <summary>The human player that currently has to make a move, or null.</summary>
		public HumanPlayer GetWaitingHuman() {
			foreach (var player in _currentPlayers) {
				if (player is HumanPlayer human && human.PendingBoard != null) return human;
			}
			return null;
		}

		public void Start() {
			var thread = new Thread(Run) { IsBackground = true, Name = "Match" };
			thread.Start();
		}

		public void Cancel() {
			_cancellation.Cancel();
			foreach (var player in _currentPlayers) {
				(player as HumanPlayer)?.Cancel();
			}
		}

		public void Run() {
			try {
				for (int i = 0; i < Settings.Games && !IsCancelled; i++) {
					GameRecord record = playGame(i);
					lock (_lock) {
						int whiteIndex = record.WhitePlayerIndex;
						int blackIndex = 1 - whiteIndex;
						switch (record.Result) {
							case GameResult.WhiteWins:
								_scores[whiteIndex] += 1;
								_wins[whiteIndex]++;
								break;
							case GameResult.BlackWins:
								_scores[blackIndex] += 1;
								_wins[blackIndex]++;
								break;
							case GameResult.Draw:
								_scores[0] += 0.5;
								_scores[1] += 0.5;
								_draws++;
								break;
						}
					}
					GameFinished?.Invoke(record);
				}
			}
			catch (Exception e) {
				Console.WriteLine($"Match crashed: {e}");
			}
			finally {
				IsFinished = true;
			}
		}

		private GameRecord playGame(int gameIndex) {
			int whiteIndex = Settings.AlternateColors && gameIndex % 2 == 1 ? 1 : 0;
			BotInfo[] infos = whiteIndex == 0
				? new[] { Settings.Player1, Settings.Player2 }
				: new[] { Settings.Player2, Settings.Player1 };
			string[] names = { PlayerNames[whiteIndex], PlayerNames[1 - whiteIndex] };

			var record = new GameRecord(gameIndex + 1, names[0], names[1], whiteIndex);
			Board board = BoardSetup.CreateStandardBoard();
			record.AddPosition(new PositionSnapshot(board, null, null, 0));
			lock (_lock) _games.Add(record);

			// Index 0 = white, 1 = black.
			var players = new IChessBot[2];
			for (int i = 0; i < 2; i++) {
				try {
					players[i] = infos[i].Create();
				}
				catch (Exception e) {
					Console.WriteLine($"[{names[i]}] constructor crashed:{Environment.NewLine}{e}");
					record.Finish(i == 0 ? GameResult.BlackWins : GameResult.WhiteWins,
						$"{names[i]} crashed while being created ({(e.InnerException ?? e).Message})");
					return record;
				}
			}
			_currentPlayers = players;
			if (IsCancelled) Cancel();

			var repetitions = new Dictionary<string, int> { [board.GetPositionKey()] = 1 };
			int pliesWithoutProgress = 0;

			while (true) {
				if (IsCancelled) {
					record.Finish(GameResult.Aborted, "match cancelled");
					break;
				}

				TeamType team = board.GetTurnTeamType();
				int teamIndex = team == TeamType.White ? 0 : 1;
				List<Move> legalMoves = board.GetAllLegalMovesForTeam(team);
				if (legalMoves.Count == 0) {
					record.Finish(GameResult.Draw, $"{names[teamIndex]} has no legal moves");
					break;
				}

				MoveRequestResult result = BotRunner.RequestMove(players[teamIndex], board, Settings.TimeLimitMilliseconds, _cancellation.Token);
				if (result.Cancelled) {
					record.Finish(GameResult.Aborted, "match cancelled");
					break;
				}
				if (result.Error != null) {
					record.Finish(team == TeamType.White ? GameResult.BlackWins : GameResult.WhiteWins, $"{names[teamIndex]} {result.Error}");
					break;
				}

				Move move = result.Move;
				bool isProgress = move.IsCapture || move.MovingPiece.Type == PieceType.Pawn;
				string moveText = MoveNotation.Describe(move, legalMoves);
				board.MakeMove(move);
				moveText += MoveNotation.Suffix(board);
				record.AddPosition(new PositionSnapshot(board, board.GetLastMove(), moveText, result.ElapsedMilliseconds));

				TeamType winner = board.HasTeamWon();
				if (winner != TeamType.None) {
					record.Finish(winner == TeamType.White ? GameResult.WhiteWins : GameResult.BlackWins, "captured the king");
					break;
				}

				pliesWithoutProgress = isProgress ? 0 : pliesWithoutProgress + 1;
				if (pliesWithoutProgress >= 100) {
					record.Finish(GameResult.Draw, "50 moves without capture or pawn move");
					break;
				}

				string key = board.GetPositionKey();
				repetitions[key] = repetitions.GetValueOrDefault(key) + 1;
				if (repetitions[key] >= 3) {
					record.Finish(GameResult.Draw, "threefold repetition");
					break;
				}

				if (isInsufficientMaterial(board)) {
					record.Finish(GameResult.Draw, "insufficient material");
					break;
				}

				if (board.GetMoveCount() >= Settings.MaxPlies) {
					record.Finish(GameResult.Draw, $"move limit reached ({Settings.MaxPlies / 2} moves)");
					break;
				}
			}

			_currentPlayers = new IChessBot[0];
			return record;
		}

		/// <summary>Only kings and at most one knight or bishop left.</summary>
		private static bool isInsufficientMaterial(Board board) {
			int minorPieces = 0;
			foreach (var piece in board.GetPieceDictionary().Values) {
				switch (piece.Type) {
					case PieceType.King:
						break;
					case PieceType.Knight:
					case PieceType.Bishop:
						minorPieces++;
						break;
					default:
						return false;
				}
			}
			return minorPieces <= 1;
		}
	}
}
