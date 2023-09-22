using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces {
	public class PiecePawn : Piece {
		public PiecePawn(TeamType team, Board board) : base(PieceType.Pawn, team, board) {
		}

		public override List<Vector2Int> GetAllLegalMoves(Square fromSquare) {
			List<Vector2Int> moves = getPawnMoves(fromSquare);
			List<Vector2Int> baseMoves = base.GetAllLegalMoves(fromSquare);

			return MoveHelper.CombineMoves(moves, baseMoves);
		}

		private List<Vector2Int> getPawnMoves(Square fromSquare) {
			List<Vector2Int> moves = new();
			// Move straight.
			Vector2Int forward = fromSquare.position + Board.GetTeamDirection(team);
			Vector2Int twoForward = forward + Board.GetTeamDirection(team);
			Square forwardSquare = _board.GetSquare(forward);
			Square twoForwardSquare = _board.GetSquare(twoForward);
			if (forwardSquare != null && !forwardSquare.HasPiece()) {
				moves.Add(forward);
			}
			if (twoForwardSquare != null
				&& _board.IsStartingPawnRow(team, fromSquare.position.y)
				&& !twoForwardSquare.HasPiece()) {
				moves.Add(twoForward);
			}

			// Capture sideways.
			Vector2Int leftCapture = forward + Vector2Int.LEFT;
			Vector2Int rightCapture = forward + Vector2Int.RIGHT;
			Square leftCaptureSquare = _board.GetSquare(leftCapture);
			Square rightCaptureSquare = _board.GetSquare(rightCapture);

			if (leftCaptureSquare != null
				&& leftCaptureSquare.IsPieceEnemyTeam(team)) {
				moves.Add(leftCapture);
			}
			if (rightCaptureSquare != null
				&& rightCaptureSquare.IsPieceEnemyTeam(team)) {
				moves.Add(rightCapture);
			}

			return moves;
		}
	}
}
