using DChess.Chess.ChessAI;
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

		public BoardManager(Board board, BoardNetworking boardNetworking) {
			Board = board;
			BoardUI = new BoardUI(board, this);
			BoardNetworking = boardNetworking;	
		}

		public void AddComputerPlayer(TeamType team) {
			_computerPlayer = team;
		}

		public void MakeComputerMove(bool automatic = true) {
			if (automatic && unDidLastMove) return;
			if (!automatic && unDidLastMove) unDidLastMove = false;
			if (Board.HasTeamWon() != TeamType.None) return;
			var algo = new MinMaxRecursive();
			var move = algo.GetBestMove(Board);
			Board.MakeMove(move);
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

				if (i == 3) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceQueen(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceQueen(TeamType.Black, Board));
				}
				if (i == Board.Size.x - 4) {
					Board.PlacePiece(new Vector2Int(i, 0), new PieceKing(TeamType.White, Board));
					Board.PlacePiece(new Vector2Int(i, Board.Size.y - 1), new PieceKing(TeamType.Black, Board));
				}
			}
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
