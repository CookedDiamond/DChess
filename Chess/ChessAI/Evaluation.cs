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
    public class Evaluation {

		protected Board _board;

		public Evaluation(Board board) {
			_board = board;
		}

		// Positive -> white is ahead, negative -> black is ahead.
		public float GetEvaluation() {
			float evalDiff = getEvaluation(TeamType.White) - getEvaluation(TeamType.Black);
			float randomOffset = ((float) new Random().NextDouble() - 0.5f) * 0.0001f;
			return evalDiff + randomOffset;
		}

		private float getEvaluation(TeamType team) {

			var teamPiecesPair = _board.GetAllPiecesFromTeam(team);
			float result = 0;
			foreach (var pair in teamPiecesPair) {
				float pieceScore = pair.Value.GetPieceScore(_board,position: pair.Key, team);
				result += pieceScore;
				// Debug.WriteLine($"Current result: {result} Current Piece: {square.Piece} Current Piece Score: {pieceScore}");
			}
			return result;
		}

	}
}
