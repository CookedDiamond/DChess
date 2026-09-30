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

		public BoardManager(Board board, BoardNetworking boardNetworking) {
			Board = board;
			BoardUI = new BoardUI(board, this);
			BoardNetworking = boardNetworking;	
		}

		public void AddComputerPlayer(TeamType team) {
			_computerPlayer = team;
		}

		/// <summary>Sets the bot that makes the computer moves.</summary>
		public void SetComputerBot(IChessBot bot, int timeLimitMilliseconds) {
			_computerBot = bot;
			_computerTimeLimitMilliseconds = timeLimitMilliseconds;
		}

		public string ComputerBotName => _computerBot?.Name;

		public void MakeComputerMove(bool automatic = true) {
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
				//TODO: fix with online update.
				BoardNetworking.MakeMove(move);
			}
		}

		/// <summary>
		/// Difference to Board.UndoLastMove is that it now disables the automatic AI response.
		/// Else you undo the AI move and then the AI redoes it instantly.
		/// </summary>
		public void UndoLastMove() {
			Board.UndoLastMove();
			unDidLastMove = true;
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
