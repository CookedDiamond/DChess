using DChess.Chess.Pieces;
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
    public abstract class Evaluation {

		protected Board _board;

		protected Evaluation(Board board) {
			_board = board;
		}

		// Positive -> white is ahead, negative -> black is ahead.
		public float GetEvaluation() {
			float evalDiff = getEvaluation(TeamType.White) - getEvaluation(TeamType.Black);
			float randomOffset = ((float) new Random().NextDouble() - 0.5f) * 0.25f;
			return evalDiff + randomOffset;
		}

		private float getEvaluation(TeamType team) {

			// Min value if no king is existant.
			var kingsCount = _board.GetPiecesFromTeamWithType(PieceType.King, team).Count;
			if (kingsCount <= 0) {
				return float.MinValue;
			}

			var teamPiecesPair = _board.GetAllPiecesFromTeam(team);
			float result = 0;
			foreach (var pair in teamPiecesPair) {
				float pieceScore = getPieceScore(pair.Value, pair.Key);
				result += pieceScore;
				// Debug.WriteLine($"Current result: {result} Current Piece: {square.Piece} Current Piece Score: {pieceScore}");
			}
			return result;
		}

		protected abstract float getPieceScore(Piece piece, Vector2Int position);

	}
}
