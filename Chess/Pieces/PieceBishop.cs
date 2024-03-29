﻿using DChess.Chess.ChessAI;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces
{
    public class PieceBishop : Piece {
		public PieceBishop(TeamType team, Board board) : base(PieceType.Bishop, team, board) {
		}

		public override List<Move> GetAllLegalMoves(Vector2Int fromPosition) {
			return ChessUtil.CombineLists(getBishopMoves(fromPosition), base.GetAllLegalMoves(fromPosition));
		}

		public override float GetPieceScore() {
			return 3f;
		}

		public override float GetPieceScore(Board board, Vector2Int position, TeamType team) {
			return EvaluationHelper.GetMinorScore(board, position, this);
		}

		private List<Move> getBishopMoves(Vector2Int fromPosition) {
			List<Move> moves = new();

			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(1, 1)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(1, -1)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(-1, -1)));
			moves.AddRange(getMovesInDirection(fromPosition, new Vector2Int(-1, 1)));

			return moves;
		}
	}
}
