using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI
{
	public class MinMaxRecursive
	{
        private static readonly bool USE_MULTI_THREADING = true;
        private static readonly bool USE_LESS_PIECE_EXTENSIONS = true;

        private long _posAnalysed = 0;
		private long _skippedBeta = 0;
		private List<MoveEvalPair> _nextMoves = new();
		private int _maxDepth = 3;
		

		private float evalOfPos(Board board, int depth, float alpha, float beta, bool isWhite)
		{
			_posAnalysed++;
			if (_posAnalysed % 50000 == 0)
			{
				Debug.WriteLine($"Positions analysed: {_posAnalysed}.");
			}
			if (depth == 0 || board.HasTeamWon() != TeamType.None)
			{
				float eval = board.GetEvaluaton();
                return eval;
			}

			if (isWhite)
			{
				float maxEval = float.MinValue;
				List<Move> legalMoves = board.GetAllLegalMovesForTeam(TeamType.White);
				ChessUtil.SortMovesByPotential(legalMoves);
				foreach (Move move in legalMoves)
				{
					var clonedBoard = board.CloneBoard();
					clonedBoard.MakeMove(move);
					float eval = evalOfPos(clonedBoard, depth - 1, alpha, beta, !isWhite);
					maxEval = Math.Max(maxEval, eval);
					alpha = Math.Max(alpha, eval);

					if (beta <= alpha)
					{
						_skippedBeta++;
						break;
					}
				}

				return maxEval;
			}

			else
			{
				float minEval = float.MaxValue;
				List<Move> legalMoves = board.GetAllLegalMovesForTeam(TeamType.Black);
				ChessUtil.SortMovesByPotential(legalMoves);
				foreach (Move move in legalMoves)
				{
					var clonedBoard = board.CloneBoard();
					clonedBoard.MakeMove(move);
					float eval = evalOfPos(clonedBoard, depth - 1, alpha, beta, !isWhite);
					minEval = Math.Min(minEval, eval);
					beta = Math.Min(beta, eval);

					if (beta <= alpha)
					{
						_skippedBeta++;
						break;
					}
				}
				return minEval;
			}
		}

		private void StartRecursion(Board board, bool isWhite)
		{
			List<Move> firstMoves;
			if (isWhite)
			{
				firstMoves = board.GetAllLegalMovesForTeam(TeamType.White);
			}
			else
			{
				firstMoves = board.GetAllLegalMovesForTeam(TeamType.Black);
			}
            firstMoves = ChessUtil.SortMovesByPotential(firstMoves);
            Move[] arrayMoves = firstMoves.ToArray();

			if (USE_MULTI_THREADING)
			{
				Parallel.ForEach(arrayMoves, (move) =>
				{
					StartRecursiveTree(board, move);
				});
			}
			else
			{
				foreach (Move move in arrayMoves)
				{
					StartRecursiveTree(board, move);
				}
			}
		}

		private void StartRecursiveTree(Board board, Move move) {
            var clonedBoard = board.CloneBoard();
            clonedBoard.MakeMove(move);
            float eval = evalOfPos(clonedBoard, _maxDepth, float.MinValue, float.MaxValue, clonedBoard.IsWhitesTurn);
            _nextMoves.Add(new MoveEvalPair(move, eval));
			Debug.WriteLine($"Added Move {move} with eval {eval}.");
        }

		public Move GetBestMove(Board board)
		{
			float bestEval;
			bool isWhite = board.IsWhitesTurn;
			if (USE_LESS_PIECE_EXTENSIONS)
			{
				if (board.GetTotalPieceCount() <= 4) _maxDepth += 1;
				if (board.GetTotalPieceCount() <= 12) _maxDepth += 1;
			}

			var watch = new Stopwatch();
			watch.Start();
			StartRecursion(board, isWhite);
			watch.Stop();

			
			Move returnMove;

			if (isWhite)
			{
				MoveEvalPair best = _nextMoves.MaxBy(t => t.Evaluation);
				bestEval = best.Evaluation;
				returnMove = best.Move;
			}
			else
			{
				MoveEvalPair best = _nextMoves.MinBy(t => t.Evaluation);
				bestEval = best.Evaluation;
				returnMove = best.Move;
			}

			Debug.WriteLine($"Positions analysed: {_posAnalysed}, {_skippedBeta} (beta) trees skipped.");
			Debug.WriteLine($"Eval: {bestEval}, Time: {Math.Round(watch.Elapsed.TotalSeconds, 2)}s, PpS: {Math.Round(_posAnalysed / watch.Elapsed.TotalSeconds)}");

			return returnMove;
		}

	}

	public struct MoveEvalPair
	{
		public Move Move;
		public float Evaluation;

		public MoveEvalPair(Move move, float eval)
		{
			Move = move;
			Evaluation = eval;
		}
	}
}
