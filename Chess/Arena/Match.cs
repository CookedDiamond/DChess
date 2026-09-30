using DChess.BotApi;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Persistence;
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
		private Board _currentBoard;
		private readonly MatchState _resume;
		private readonly Action<MatchState> _autosave;

		public MatchSettings Settings { get; }

		/// <summary>Display names of player 1 and 2 (numbered if both are the same bot).</summary>
		public string[] PlayerNames { get; }

		private volatile bool _isFinished;
		public bool IsFinished => _isFinished;
		public bool IsCancelled => _cancellation.IsCancellationRequested;

		/// <summary>Called on the match thread after every game.</summary>
		public event Action<GameRecord> GameFinished;

		public Match(MatchSettings settings, MatchState resume = null, Action<MatchState> autosave = null) {
			Settings = settings;
			_resume = resume;
			_autosave = autosave;
			string name1 = settings.Player1.Name;
			string name2 = settings.Player2.Name;
			PlayerNames = name1 == name2
				? new[] { $"{name1} (1)", $"{name2} (2)" }
				: new[] { name1, name2 };
			if (resume != null) {
				foreach (var savedGame in resume.Games) {
					var record = savedGame.Restore();
					_games.Add(record);
					if (record.Result == GameResult.WhiteWins) { _scores[record.WhitePlayerIndex]++; _wins[record.WhitePlayerIndex]++; }
					if (record.Result == GameResult.BlackWins) { _scores[1 - record.WhitePlayerIndex]++; _wins[1 - record.WhitePlayerIndex]++; }
					if (record.Result == GameResult.Draw) { _scores[0] += 0.5; _scores[1] += 0.5; _draws++; }
				}
				_currentBoard = resume.Board?.Restore();
			}
		}

		public MatchState CaptureState() {
			lock (_lock) return new MatchState {
				Player1 = Settings.Player1.Name, Player2 = Settings.Player2.Name, GameCount = Settings.Games,
				TimeLimitMilliseconds = Settings.TimeLimitMilliseconds, AlternateColors = Settings.AlternateColors,
				MaxPlies = Settings.MaxPlies, Board = _currentBoard == null ? null : BoardState.Capture(_currentBoard),
				Games = _games.Select(SavedGame.Capture).ToList()
			};
		}

		private void saveProgress() {
			if (!IsCancelled) _autosave?.Invoke(CaptureState());
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
				int start = _games.Count;
				if (start > 0 && !_games[^1].IsFinished) start--;
				for (int i = start; i < Settings.Games && !IsCancelled; i++) {
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
					saveProgress();
				}
			}
			catch (Exception e) {
				Console.WriteLine($"Match crashed: {e}");
			}
			finally {
				_isFinished = true;
			}
		}

		private GameRecord playGame(int gameIndex) {
			int whiteIndex = Settings.AlternateColors && gameIndex % 2 == 1 ? 1 : 0;
			BotInfo[] infos = whiteIndex == 0
				? new[] { Settings.Player1, Settings.Player2 }
				: new[] { Settings.Player2, Settings.Player1 };
			string[] names = { PlayerNames[whiteIndex], PlayerNames[1 - whiteIndex] };

			bool resuming = _resume != null && gameIndex < _games.Count && !_games[gameIndex].IsFinished;
			var record = resuming ? _games[gameIndex] : new GameRecord(gameIndex + 1, names[0], names[1], whiteIndex);
			Board board = resuming ? _currentBoard : BoardSetup.CreateStandardBoard();
			lock (_lock) {
				_currentBoard = board;
				if (!resuming) {
					record.AddPosition(new PositionSnapshot(board, null, null, 0));
					_games.Add(record);
				}
			}

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

			var repetitions = new Dictionary<string, int>();
			var replay = board.CloneBoard();
			while (true) {
				string replayKey = replay.GetPositionKey();
				repetitions[replayKey] = repetitions.GetValueOrDefault(replayKey) + 1;
				if (replay.GetMoveCount() == 0) break;
				replay.UndoLastMove();
			}
			int pliesWithoutProgress = board.GetMoveHistory().Reverse().TakeWhile(m => !m.IsCapture && m.MovingPiece.Type != PieceType.Pawn).Count();

			while (true) {
				if (IsCancelled) {
					record.Finish(GameResult.Aborted, "match cancelled");
					break;
				}

				// Re-evaluate terminal rules before asking the next player, also after a resume.
				TeamType winner = board.HasTeamWon();
				if (winner != TeamType.None) {
					record.Finish(winner == TeamType.White ? GameResult.WhiteWins : GameResult.BlackWins, "captured the king");
					break;
				}
				string drawReason = pliesWithoutProgress >= 100 ? "50 moves without capture or pawn move"
					: repetitions.GetValueOrDefault(board.GetPositionKey()) >= 3 ? "threefold repetition"
					: isInsufficientMaterial(board) ? "insufficient material"
					: board.GetMoveCount() >= Settings.MaxPlies ? $"move limit reached ({Settings.MaxPlies / 2} moves)"
					: null;
				if (drawReason != null) { record.Finish(GameResult.Draw, drawReason); break; }
				saveProgress();

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
				lock (_lock) {
					board.MakeMove(move);
					moveText += MoveNotation.Suffix(board);
					record.AddPosition(new PositionSnapshot(board, board.GetLastMove(), moveText, result.ElapsedMilliseconds));
				}

				pliesWithoutProgress = isProgress ? 0 : pliesWithoutProgress + 1;

				string key = board.GetPositionKey();
				repetitions[key] = repetitions.GetValueOrDefault(key) + 1;
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
