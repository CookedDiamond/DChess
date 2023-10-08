using DChess.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.ChessAI {
	public abstract class Evaluation {

		protected Board _board;

		protected Evaluation(Board board) {
			_board = board;
		}

		// Positive -> white is ahead, negative -> black is ahead.
		public float GetEvaluation() {
			return getEvaluation(TeamType.White) - getEvaluation(TeamType.Black);
		}

		private float getEvaluation(TeamType team) {

			// Min value if no king is existant.
			var kingsCount = _board.GetPieceSquares(Pieces.PieceType.King, team).Count;
			if (kingsCount <= 0) {
				return float.MinValue;
			}

			var squaresWithTeamPieces = _board.GetAllSquaresWithTeamPieces(team);
			float result = 0;
			foreach (var square in squaresWithTeamPieces) {
				float pieceScore = getPieceScore(square);
				result += pieceScore;
				// Debug.WriteLine($"Current result: {result} Current Piece: {square.Piece} Current Piece Score: {pieceScore}");
			}
			return result;
		}

		protected abstract float getPieceScore(Square square);

	}
}
