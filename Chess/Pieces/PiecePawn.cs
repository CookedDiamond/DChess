using DChess.Chess.ChessAI;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces
{
    public class PiecePawn : Piece {
		public PiecePawn(TeamType team, Board board) : base(PieceType.Pawn, team, board) {
		}

		public override List<Move> GetAllLegalMoves(Vector2Int fromPosition) {
			List<Move> moves = getPawnMoves(fromPosition);
			List<Move> baseMoves = base.GetAllLegalMoves(fromPosition);

			return ChessUtil.CombineLists(moves, baseMoves);
		}

		public override float GetPieceScore() {
			return 1;
		}

		public override float GetPieceScore(Board board, Vector2Int position, TeamType team) {
			return EvaluationHelper.GetPawnScore(board, position, this);
		}

		private List<Move> getPawnMoves(Vector2Int fromPosition) {
			List<Vector2Int> moves = new();
			// Move straight.
			Vector2Int forward = fromPosition + Board.GetTeamDirection(Team);
			Vector2Int twoForward = forward + Board.GetTeamDirection(Team);
			if (getMoveType(forward) == MoveType.Empty) {
				moves.Add(forward);

				if (getMoveType(twoForward) == MoveType.Empty
					&& _board.IsStartingPawnRow(Team, fromPosition.y)) {
					moves.Add(twoForward);
				}
			}


			// Capture sideways.
			Vector2Int leftCapture = forward + Vector2Int.LEFT;
			Vector2Int rightCapture = forward + Vector2Int.RIGHT;
			var leftCapturePiece = _board.GetPiece(leftCapture);
			var rightCapturePiece = _board.GetPiece(rightCapture);

			if (leftCapturePiece != NULL_PIECE
				&& leftCapturePiece.IsEnemyTeam(Team)) {
				moves.Add(leftCapture);
			}
			if (rightCapturePiece != NULL_PIECE
				&& rightCapturePiece.IsEnemyTeam(Team)) {
				moves.Add(rightCapture);
			}

			return ChessUtil.CreateMoveListFromVectorList(moves, fromPosition, _board);
		}
	}
}
