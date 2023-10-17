using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI
{
    public class StandartEvaluation : Evaluation {

		private readonly float pawnScore = 1;
		private readonly float minorScore = 3;
		private readonly float rookScore = 5;
		private readonly float queenScore = 9;

		public StandartEvaluation(Board board) : base(board) {
		}

		protected override float getPieceScore(Piece piece, Vector2Int position) {
			return piece.Type switch {
				Pieces.PieceType.Pawn => getPawnScore(position, piece.Team),
				Pieces.PieceType.Knight => minorScore,
				Pieces.PieceType.Bishop => minorScore,
				Pieces.PieceType.Rook => rookScore,
				Pieces.PieceType.Queen => queenScore,
				Pieces.PieceType.King => 0,
				_ => throw new NotImplementedException(),
			};
		}

		private float getPawnScore(Vector2Int position, TeamType team) {
			float result = pawnScore;
			var teamDir = Board.GetTeamDirection(team);
			int yPos = position.y;
			int boardHeight = _board.Size.y - 1;
			float progress = (float)yPos / boardHeight;
			// Reverse progress for black.
			if (teamDir.y < 0) {
				progress = 1 - progress;
			}

			return result + (progress * progress);
		}
	}
}

