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

        private long _posAnalysed = 0;
        private long _skipped = 0;
        private long _skippedBeta = 0;
        private List<MoveEvalPair> _nextMoves = new();
        private int _maxDepth = 4;
        private readonly float lastEvalBoundry = 3.5f;
        private readonly float lastEval;

        public MinMaxRecursive(float lastEval)
        {
            this.lastEval = lastEval;
        }

        private float evalOfPos(Board board, int depth, float alpha, float beta, bool isWhite)
        {
            _posAnalysed++;
            float boardEval = board.GetEvaluaton();
            if (_posAnalysed % 50000 == 0)
            {
                Debug.WriteLine($"Positions analysed: {_posAnalysed} and {_skipped} trees skipped.");
            }
            if (Math.Abs(lastEval) + lastEvalBoundry < Math.Abs(boardEval))
            {
                _skipped++;
                return boardEval;
            }
            if (depth == 0 || board.HasTeamWon() != TeamType.None)
            {
                return boardEval;
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

        private void startRecursion(Board board, bool isWhite)
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
            Move[] arrayMoves = firstMoves.ToArray();

            Parallel.ForEach(arrayMoves, (move) =>
            {
                var clonedBoard = board.CloneBoard();
                clonedBoard.MakeMove(move);
                float eval = evalOfPos(board, _maxDepth, float.MinValue, float.MaxValue, isWhite);
                _nextMoves.Add(new MoveEvalPair(move, eval));
            });
        }

        public Move GetBestMove(Board board, out float lastEval)
        {
            bool isWhite = board.IsWhitesTurn;
            if (board.GetTotalPieceCount() <= 7) _maxDepth += 1;
            if (board.GetTotalPieceCount() <= 13) _maxDepth += 1;

            var watch = new Stopwatch();
            watch.Start();
            startRecursion(board, isWhite);
            watch.Stop();

            
            Move returnMove;

            if (isWhite)
            {
                MoveEvalPair best = _nextMoves.MaxBy(t => t.Evaluation);
                lastEval = best.Evaluation;
                returnMove = best.Move;
            }
            else
            {
                MoveEvalPair best = _nextMoves.MinBy(t => t.Evaluation);
                lastEval = best.Evaluation;
                returnMove = best.Move;
            }

            Debug.WriteLine($"Positions analysed: {_posAnalysed} and {_skipped} (eval) {_skippedBeta} (beta) trees skipped.");
            Debug.WriteLine($"Eval: {lastEval}, Time: {Math.Round(watch.Elapsed.TotalSeconds, 2)}s, PpS: {Math.Round(_posAnalysed / watch.Elapsed.TotalSeconds)}");

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
