using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI
{
    public class MinMaxRecursive {

		private long _posAnalysed = 0;
		private List<MoveEvalPair> _nextMoves = new();
		private int _maxDepth = 4;

		private float evalOfPos(Board board, int depth, float alpha, float beta, bool isWhite) {
			_posAnalysed++;
			if (_posAnalysed % 25000 == 0) {
				Debug.WriteLine($"Positions analysed: {_posAnalysed}.");
			}
			if (depth == 0 || board.HasTeamWon() != null) {
				return board.GetEvaluaton();
			}

			if (isWhite) {
				float maxEval = float.MinValue;
				List<Move> legalMoves = board.GetAllLegalMovesForTeam(TeamType.White);
				foreach (Move move in legalMoves) {
					Board clonedBoard = ChessUtil.CloneBoard(board);
					clonedBoard.MakeMove(move);
					float eval = evalOfPos(clonedBoard, depth - 1, alpha, beta, !isWhite);
					maxEval = Math.Max(maxEval, eval);
					alpha = Math.Max(alpha, eval);

					if (depth == _maxDepth) {
						_nextMoves.Add(new MoveEvalPair(move, eval));
					}

					if (beta <= alpha) {
						break;
					}
				}
				
				return maxEval;
			}

			else {
				float minEval = float.MaxValue;
				List<Move> legalMoves = board.GetAllLegalMovesForTeam(TeamType.Black);
				foreach (Move move in legalMoves) {
					Board clonedBoard = ChessUtil.CloneBoard(board);
					clonedBoard.MakeMove(move);
					float eval = evalOfPos(clonedBoard, depth - 1, alpha, beta, !isWhite);
					minEval = Math.Min(minEval, eval);
					beta = Math.Min(beta, eval);

					if (depth == _maxDepth) {
						_nextMoves.Add(new MoveEvalPair(move, eval));
					}

					if (beta <= alpha) {
						break;
					}
				}
				return minEval;
			}
		}

		public Move GetBestMove(Board board) {
			if (board.GetTotalPieceCount() <= 4) _maxDepth += 1;
			if (board.GetTotalPieceCount() <= 7) _maxDepth += 1;
			bool isWhite = board.IsWhitesTurn;
			var currPos = evalOfPos(board, _maxDepth, float.MinValue, float.MaxValue, isWhite);
			Debug.WriteLine($"Current Eval: {currPos}");
			if (isWhite) {
				MoveEvalPair best = _nextMoves.MaxBy(t => t.Evaluation);
				return best.Move;
			}
			else {
				MoveEvalPair best = _nextMoves.MinBy(t => t.Evaluation);
				return best.Move;
			}
			
		}

	}

	public struct MoveEvalPair {
		public Move Move;
		public float Evaluation;

		public MoveEvalPair(Move move, float eval) {
			Move = move;
			Evaluation = eval;
		}
	}
}
