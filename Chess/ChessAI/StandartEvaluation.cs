using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI {
	public class StandartEvaluation : Evaluation {

		private readonly float pawnScore = 1;
		private readonly float minorScore = 3;
		private readonly float rookScore = 5;
		private readonly float queenScore = 9;

		public StandartEvaluation(Board board) : base(board) {
		}

		protected override float getPieceScore(Square square) {
			return square.Piece.Type switch {
				Pieces.PieceType.Pawn => getPawnScore(square),
				Pieces.PieceType.Knight => minorScore,
				Pieces.PieceType.Bishop => minorScore,
				Pieces.PieceType.Rook => rookScore,
				Pieces.PieceType.Queen => queenScore,
				Pieces.PieceType.King => 0,
				_ => throw new NotImplementedException(),
			};
		}

		private float getPawnScore(Square square) {
			float result = pawnScore;
			var teamDir = Board.GetTeamDirection(square.Piece.Team);
			if (Math.Abs(teamDir.y) > Math.Abs(teamDir.x)) {
				// How close to promoting from 0 to 1.
				float progress = (float)(_board.Size.y - square.Position.y + 1) / _board.Size.y;
				if (teamDir.y < 0) {
					result += progress;
				}
				else {
					result += 1- progress;
				}
				
			}
			else {
				throw new NotImplementedException();
			}
			
			return  result;
		}
	}
}
