using DChess.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI {
	public class MinMaxRecursive {

		private long posAnalysed = 0;
		private List<MoveEvalPair> nextMoves = new();
		private const int maxDepth = 5;

		private float evalOfPos(Board board, int depth, float alpha, float beta, bool isWhite) {
			posAnalysed++;
			if (posAnalysed % 10000 == 0) {
				Debug.WriteLine($"Positions analysed: {posAnalysed}.");
			}
			if (depth == 0 || board.HasTeamWon != null) {
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
					if (beta <= alpha) {
						break;
					}

					if (depth == maxDepth - 1) {
						nextMoves.Add(new MoveEvalPair(move, eval));
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
					minEval = Math.Max(minEval, eval);
					beta = Math.Max(beta, eval);
					if (beta <= alpha) {
						break;
					}
				}
				return minEval;
			}
		}

		public Move GetBestMove(Board board) {
			var currPos = evalOfPos(board, maxDepth, float.MinValue, float.MaxValue, board.IsWhitesTurn);
			MoveEvalPair best = nextMoves.MaxBy(t => t.Evaluation);

			return best.Move;
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
