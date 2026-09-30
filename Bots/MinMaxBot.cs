using DChess.BotApi;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using System;
using System.Collections.Generic;

namespace DChess.Bots {
	/// <summary>
	/// Revamped version of the old MinMaxRecursive AI.
	///  - Negamax search (min-max written from the view of the side to move) with alpha-beta pruning.
	///  - Makes and undoes moves on one board instead of cloning the board for every position.
	///  - Iterative deepening: searches depth 1, 2, 3, ... until the time for the move is used up,
	///    so it automatically searches deeper when there are fewer pieces.
	///  - Quiescence search: at the end of the search it keeps following captures,
	///    so it never stops in the middle of a trade.
	///  - Move ordering: best move of the previous depth first, then captures (most valuable victim first).
	/// The evaluation is based on the old Evaluation/EvaluationHelper (material, pawn progress, minor piece placement).
	/// </summary>
	public class MinMaxBot : IChessBot {
		/// <summary>Set to true to print depth, evaluation and speed of every move to the console.</summary>
		public static bool LogSearchInfo = false;

		private const int WIN_SCORE = 1_000_000;
		private const int INFINITY = 2_000_000;
		private const int MAX_DEPTH = 30;
		private const int MAX_QUIESCENCE_DEPTH = 8;

		private readonly Random _random = new();
		private BotBoard _board;
		private BotTimer _timer;
		private long _stopAtMilliseconds;
		private bool _outOfTime;
		private long _nodes;

		public string Name => "MinMaxBot";

		public Move Think(BotBoard board, BotTimer timer) {
			_board = board;
			_timer = timer;
			_nodes = 0;
			_outOfTime = false;
			// Stop a bit before the limit to be safe.
			_stopAtMilliseconds = timer.TimeLimitMilliseconds - Math.Max(10, timer.TimeLimitMilliseconds / 10);

			List<Move> rootMoves = board.GetLegalMoves();
			if (rootMoves.Count == 0) return null;
			foreach (Move move in rootMoves) {
				if (move.CapturedPiece.Type == PieceType.King) return move;
			}

			// Shuffle first, so equally good moves are chosen randomly and games are not always the same.
			shuffle(rootMoves);
			orderMoves(rootMoves);

			Move bestMove = rootMoves[0];
			int bestScore = 0;
			int completedDepth = 0;

			for (int depth = 1; depth <= MAX_DEPTH; depth++) {
				int alpha = -INFINITY;
				Move iterationBestMove = null;
				int iterationBestScore = -INFINITY;

				foreach (Move move in rootMoves) {
					_board.MakeMove(move);
					int score = -search(depth - 1, -INFINITY, -alpha, 1);
					_board.UndoMove();

					if (_outOfTime) break;
					if (score > iterationBestScore) {
						iterationBestScore = score;
						iterationBestMove = move;
					}
					alpha = Math.Max(alpha, score);
				}

				if (_outOfTime) {
					// The previous best move is searched first, so a move found in the
					// unfinished iteration is at least as good as it.
					if (iterationBestMove != null) {
						bestMove = iterationBestMove;
						bestScore = iterationBestScore;
					}
					break;
				}

				bestMove = iterationBestMove;
				bestScore = iterationBestScore;
				completedDepth = depth;

				// Search the best move first in the next iteration.
				rootMoves.Remove(bestMove);
				rootMoves.Insert(0, bestMove);

				// A forced king capture (for either side) was found, deeper search will not change that.
				if (Math.Abs(bestScore) >= WIN_SCORE - MAX_DEPTH * 2) break;
				// The next depth takes a lot longer, don't start it if it can not finish.
				if (_timer.ElapsedMilliseconds > _stopAtMilliseconds / 3) break;
			}

			if (LogSearchInfo) {
				Console.WriteLine($"{Name} ({board.MyTeam}): {bestMove}, depth {completedDepth}, eval {bestScore / 100.0:+0.00;-0.00}, " +
					$"{_nodes} positions in {timer.ElapsedMilliseconds} ms");
			}
			return bestMove;
		}

		private int search(int depth, int alpha, int beta, int ply) {
			if (isOutOfTime()) return 0;

			TeamType winner = _board.Winner;
			if (winner != TeamType.None) {
				// Prefer fast wins and slow losses.
				return winner == _board.SideToMove ? WIN_SCORE - ply : -(WIN_SCORE - ply);
			}
			if (depth <= 0) {
				return quiescence(alpha, beta, ply, 0);
			}

			List<Move> moves = _board.GetLegalMoves();
			if (moves.Count == 0) return 0;
			orderMoves(moves);

			foreach (Move move in moves) {
				_board.MakeMove(move);
				int score = -search(depth - 1, -beta, -alpha, ply + 1);
				_board.UndoMove();

				if (_outOfTime) return 0;
				if (score >= beta) return beta;
				if (score > alpha) alpha = score;
			}
			return alpha;
		}

		/// <summary>Only looks at captures until the position is quiet.</summary>
		private int quiescence(int alpha, int beta, int ply, int quiescenceDepth) {
			if (isOutOfTime()) return 0;

			TeamType winner = _board.Winner;
			if (winner != TeamType.None) {
				return winner == _board.SideToMove ? WIN_SCORE - ply : -(WIN_SCORE - ply);
			}

			// "Stand pat": the side to move does not have to capture.
			int standPat = evaluate();
			if (standPat >= beta) return beta;
			if (standPat > alpha) alpha = standPat;
			if (quiescenceDepth >= MAX_QUIESCENCE_DEPTH) return alpha;

			List<Move> captures = _board.GetCaptureMoves();
			orderMoves(captures);

			foreach (Move move in captures) {
				_board.MakeMove(move);
				int score = -quiescence(-beta, -alpha, ply + 1, quiescenceDepth + 1);
				_board.UndoMove();

				if (_outOfTime) return 0;
				if (score >= beta) return beta;
				if (score > alpha) alpha = score;
			}
			return alpha;
		}

		private bool isOutOfTime() {
			_nodes++;
			if ((_nodes & 255) == 0 && (_timer.IsTimeUp || _timer.ElapsedMilliseconds >= _stopAtMilliseconds)) {
				_outOfTime = true;
			}
			return _outOfTime;
		}

		// ------------------------------------------------------------------------------------
		// Evaluation (in centipawns: 100 = one pawn)
		// ------------------------------------------------------------------------------------

		/// <summary>Positive = good for the side to move.</summary>
		private int evaluate() {
			int whiteScore = 0;
			foreach (PieceOnSquare piece in _board.GetAllPieces()) {
				int value = pieceScore(piece);
				whiteScore += piece.Team == TeamType.White ? value : -value;
			}
			return _board.SideToMove == TeamType.White ? whiteScore : -whiteScore;
		}

		private int pieceScore(PieceOnSquare piece) {
			int x = piece.Square.x;
			int y = piece.Square.y;
			int width = _board.Width;
			int height = _board.Height;

			switch (piece.Type) {
				case PieceType.Pawn:
					// Pawns get more valuable the closer they are to promotion.
					float progress = (float)y / (height - 1);
					if (piece.Team == TeamType.Black) progress = 1 - progress;
					return 100 + (int)(progress * progress * progress * 60);

				case PieceType.Knight:
				case PieceType.Bishop:
					// Minor pieces like the center and dislike the border.
					int value = piece.Type == PieceType.Knight ? 300 : 310;
					bool onBorder = x == 0 || y == 0 || x == width - 1 || y == height - 1;
					if (onBorder) value -= 10;
					float centerDistance = Math.Max(Math.Abs(x - (width - 1) / 2f), Math.Abs(y - (height - 1) / 2f));
					value += (int)((height / 2f - centerDistance) * 6);
					return value;

				case PieceType.Rook:
					return 500;
				case PieceType.Queen:
					return 900;
				default:
					// The king is not counted: losing it ends the game (handled in the search).
					return 0;
			}
		}

		// ------------------------------------------------------------------------------------
		// Move ordering: good moves first make alpha-beta prune much more.
		// ------------------------------------------------------------------------------------

		private static void orderMoves(List<Move> moves) {
			Move[] sortedMoves = moves.ToArray();
			int[] keys = new int[sortedMoves.Length];
			for (int i = 0; i < sortedMoves.Length; i++) {
				keys[i] = -moveOrderScore(sortedMoves[i]);
			}
			Array.Sort(keys, sortedMoves);
			moves.Clear();
			moves.AddRange(sortedMoves);
		}

		private static int moveOrderScore(Move move) {
			int score = 0;
			PieceType captured = move.CapturedPiece.Type;
			if (captured != PieceType.None) {
				// Most valuable victim, least valuable attacker.
				score += 10 * orderValue(captured) - orderValue(move.MovingPiece.Type);
			}
			if (move.IsPromotion) score += 80;
			return score;
		}

		private static int orderValue(PieceType type) {
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

		private void shuffle(List<Move> moves) {
			for (int i = moves.Count - 1; i > 0; i--) {
				int j = _random.Next(i + 1);
				(moves[i], moves[j]) = (moves[j], moves[i]);
			}
		}
	}
}
