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
    public class EvaluationHelper {
		private static readonly float MINOR_BOARDER_PANALTY = 0.02f;
		private static readonly float MINOR_SPACE_REWARD = 0.008f;

		private static readonly float PAWN_PROGRESS_MULTIPLIER = 0.4f;

		public static float GetMinorScore(Board board, Vector2Int position, Piece piece, bool boarderPanalty = true) {
			float value = piece.GetPieceScore();
			float boarderPenalty = 0;
			if (boarderPanalty) {
				if (position.x == 0
					|| position.y == 0
					|| position.x == board.Size.x - 1
					|| position.y == board.Size.y - 1) {
					boarderPenalty = MINOR_BOARDER_PANALTY;
				}
			}
			var legalMoves = piece.GetAllLegalMoves(position);
			float spaceReward = legalMoves.Count * MINOR_SPACE_REWARD;

			return value - boarderPenalty + spaceReward;
		}


		public static float GetPawnScore(Board board, Vector2Int position, Piece piece) {
			float result = piece.GetPieceScore();
			var teamDir = Board.GetTeamDirection(piece.Team);
			int yPos = position.y;
			int boardHeight = board.Size.y - 1;
			float progress = (float)yPos / boardHeight;
			// Reverse progress for black.
			if (teamDir.y < 0) {
				progress = 1 - progress;
			}

			return result + (progress * progress) * PAWN_PROGRESS_MULTIPLIER;
		}

	}
}

