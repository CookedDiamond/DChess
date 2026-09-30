using DChess.BotApi;
using DChess.Bots;
using DChess.Chess.Arena;
using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.Chess.Playground {
	public class BoardManager {

		public Board Board { get; private set; }
		public BoardUI BoardUI { get; private set; }
		public readonly BoardNetworking BoardNetworking;

		private TeamType? _computerPlayer = null;
		private bool unDidLastMove = false;
		private IChessBot _computerBot;
		private int _computerTimeLimitMilliseconds = 1000;
		private Func<IChessBot> _computerBotFactory = () => new MinMaxBot();
		private bool _replaceComputerBot;
		private Task<MoveRequestResult> _pendingMove;
		private CancellationTokenSource _thinkingCancellation;
		private long _thinkingRevision;
		public bool IsThinking => _pendingMove != null;
		public bool HasPendingComputerMove => IsThinking && _thinkingCancellation?.IsCancellationRequested == false;
		public string BotError { get; private set; }
		public event Action BoardChanged;
		public int ComputerTimeLimitMilliseconds => _computerTimeLimitMilliseconds;

		/// <summary>Starts computation on a private position; never waits on the UI thread.</summary>
		public bool BeginComputerMove(bool automatic = true) {
			if (IsThinking || (automatic && unDidLastMove) || Board.HasTeamWon() != TeamType.None) return false;
			if (Board.GetAllLegalMovesForTeam(Board.GetTurnTeamType()).Count == 0) return false;
			unDidLastMove = false;
			BotError = null;
			_computerBot ??= new MinMaxBot();
			if (_replaceComputerBot && _computerBotFactory != null) _computerBot = _computerBotFactory();
			_replaceComputerBot = false;
			var snapshot = Board.CloneBoard();
			var bot = _computerBot;
			int limit = _computerTimeLimitMilliseconds;
			_thinkingRevision = Board.Revision;
			_thinkingCancellation = new CancellationTokenSource();
			var token = _thinkingCancellation.Token;
			_pendingMove = BotRunner.RequestMoveAsync(bot, snapshot, limit, token);
			return true;
		}

		/// <summary>Poll on the UI thread; only this thread may commit the computed move.</summary>
		public void UpdateComputerMove() {
			if (_pendingMove == null || !_pendingMove.IsCompleted) return;
			var task = _pendingMove;
			_pendingMove = null;
			bool cancelled = _thinkingCancellation.IsCancellationRequested;
			_thinkingCancellation.Dispose();
			_thinkingCancellation = null;
			if (task.IsFaulted) { BotError = task.Exception.GetBaseException().Message; _replaceComputerBot = true; return; }
			if (cancelled || task.IsCanceled || Board.Revision != _thinkingRevision) { _replaceComputerBot = true; return; }
			var result = task.Result;
			BotError = result.Error;
			if (result.Error != null) _replaceComputerBot = true;
			if (result.Cancelled || result.Move == null) return;
			var legalMove = Board.FindEquivalentLegalMove(result.Move);
			if (legalMove != null) MakeMove(legalMove);
		}

		public void CancelComputerMove() {
			_thinkingCancellation?.Cancel();
		}

		public BoardManager(Board board, BoardNetworking boardNetworking) {
			Board = board;
			BoardUI = new BoardUI(board, this);
			BoardNetworking = boardNetworking;	
		}

		public void AddComputerPlayer(TeamType team) {
			_computerPlayer = team;
		}

		/// <summary>Sets the bot that makes the computer moves.</summary>
		public void SetComputerBot(IChessBot bot, int timeLimitMilliseconds, Func<IChessBot> factory = null) {
			CancelComputerMove();
			_computerBot = bot;
			_computerBotFactory = factory;
			_computerTimeLimitMilliseconds = timeLimitMilliseconds;
		}

		public string ComputerBotName => _computerBot?.Name;

		public void MakeComputerMove(bool automatic = true) {
			if (IsThinking) return;
			if (automatic && unDidLastMove) return;
			if (!automatic && unDidLastMove) unDidLastMove = false;
			if (Board.HasTeamWon() != TeamType.None) return;
			// Without a chosen bot (e.g. the CLI "ai" command) the MinMaxBot plays.
			_computerBot ??= new MinMaxBot();

			var result = BotRunner.RequestMove(_computerBot, Board, _computerTimeLimitMilliseconds, CancellationToken.None);
			if (result.Move == null) {
				Console.WriteLine($"{_computerBot.Name} could not move: {result.Error}");
				return;
			}
			MakeMove(result.Move);
		}

		public void MakeMove(Move move) {
			if (Board.MakeMove(move)) {
				CancelComputerMove();
				//TODO: fix with online update.
				BoardNetworking.MakeMove(move);
				BoardChanged?.Invoke();
			}
		}

		/// <summary>
		/// Difference to Board.UndoLastMove is that it now disables the automatic AI response.
		/// Else you undo the AI move and then the AI redoes it instantly.
		/// </summary>
		public void UndoLastMove() {
			CancelComputerMove();
			Board.UndoLastMove();
			unDidLastMove = true;
			BoardChanged?.Invoke();
		}

		public TeamType? GetComputerPlayerTeamType() {
			return _computerPlayer;
		}

		public void Build8x8StandardBoard() {
			BoardSetup.PlaceStandardPieces(Board);
		}

		public void BuildSmallBoard() {
			int center = Board.Size.x / 2;

			for (int i = 0; i < Board.Size.x; i++) {
				Board.PlacePiece(new Vector2Int(i, 1), new PiecePawn(TeamType.White, Board));
				Board.PlacePiece(new Vector2Int(i, Board.Size.y - 2), new PiecePawn(TeamType.Black, Board));
				if (i == 0 || i == Board.Size.x - 1) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceRook(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceRook(TeamType.Black, Board));
				}
				else if (i == 2 || i == Board.Size.x - 3) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceBishop(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceBishop(TeamType.Black, Board));
				}
				else if (i == 1 || i == Board.Size.x - 2) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceKnight(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceKnight(TeamType.Black, Board));
				}

				if (i == center + 1) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceKnight(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceKnight(TeamType.Black, Board));
				}
				if (i == center - 2) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceBishop(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceBishop(TeamType.Black, Board));
				}

				if (i == center - 1) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceQueen(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceQueen(TeamType.Black, Board));
				}
				if (i == center) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceKing(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceKing(TeamType.Black, Board));
				}
			}
		}
	}
}
