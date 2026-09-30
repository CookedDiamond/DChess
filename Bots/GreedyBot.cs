using DChess.BotApi;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using System;

namespace DChess.Bots {
	/// <summary>
	/// A simple bot that only looks one move ahead:
	/// it grabs the most valuable piece it can, but avoids leaving its own pieces or king attacked.
	/// A good starting point for your own bot.
	/// </summary>
	public class GreedyBot : IChessBot {
		private readonly Random _random = new();

		public string Name => "GreedyBot";

		public Move Think(BotBoard board, BotTimer timer) {
			Move bestMove = null;
			double bestScore = double.MinValue;

			foreach (Move move in board.GetLegalMoves()) {
				// Capturing the king wins the game.
				if (move.CapturedPiece.Type == PieceType.King) return move;

				double score = PieceValue(move.CapturedPiece.Type);
				if (move.IsPromotion) score += PieceValue(PieceType.Queen) - PieceValue(PieceType.Pawn);

				// Try the move and look at the position afterwards.
				board.MakeMove(move);
				if (board.IsKingAttacked(board.MyTeam)) {
					// The opponent could capture our king next move.
					score -= 1000;
				}
				else {
					// Assume the opponent takes our most valuable attacked piece.
					score -= MostValuableAttackedPiece(board);
				}
				board.UndoMove();

				// Small random bonus so equal moves are picked randomly.
				score += _random.NextDouble() * 0.1;

				if (score > bestScore) {
					bestScore = score;
					bestMove = move;
				}
			}

			return bestMove;
		}

		private static double MostValuableAttackedPiece(BotBoard board) {
			double worst = 0;
			foreach (PieceOnSquare piece in board.GetPieces(board.MyTeam)) {
				if (board.IsSquareAttackedBy(piece.Square, board.OpponentTeam)) {
					worst = Math.Max(worst, PieceValue(piece.Type));
				}
			}
			return worst;
		}

		private static double PieceValue(PieceType type) {
			return type switch {
				PieceType.Pawn => 1,
				PieceType.Knight => 3,
				PieceType.Bishop => 3,
				PieceType.Rook => 5,
				PieceType.Queen => 9,
				PieceType.King => 100,
				_ => 0
			};
		}
	}
}
