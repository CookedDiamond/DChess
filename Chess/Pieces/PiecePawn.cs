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

		public override List<Move> GetAllLegalMoves(Square fromSquare) {
			List<Move> moves = getPawnMoves(fromSquare);
			List<Move> baseMoves = base.GetAllLegalMoves(fromSquare);

			return ChessUtil.CombineLists(moves, baseMoves);
		}

		private List<Move> getPawnMoves(Square fromSquare) {
			List<Vector2Int> moves = new();
			// Move straight.
			Vector2Int forward = fromSquare.Position + Board.GetTeamDirection(Team);
			Vector2Int twoForward = forward + Board.GetTeamDirection(Team);
			Square forwardSquare = _board.GetSquare(forward);
			Square twoForwardSquare = _board.GetSquare(twoForward);
			if (forwardSquare != null && !forwardSquare.HasPiece()) {
				moves.Add(forward);
			}
			if (twoForwardSquare != null
				&& _board.IsStartingPawnRow(Team, fromSquare.Position.y)
				&& !twoForwardSquare.HasPiece()) {
				moves.Add(twoForward);
			}

			// Capture sideways.
			Vector2Int leftCapture = forward + Vector2Int.LEFT;
			Vector2Int rightCapture = forward + Vector2Int.RIGHT;
			Square leftCaptureSquare = _board.GetSquare(leftCapture);
			Square rightCaptureSquare = _board.GetSquare(rightCapture);

			if (leftCaptureSquare != null
				&& leftCaptureSquare.IsPieceEnemyTeam(Team)) {
				moves.Add(leftCapture);
			}
			if (rightCaptureSquare != null
				&& rightCaptureSquare.IsPieceEnemyTeam(Team)) {
				moves.Add(rightCapture);
			}

			return ChessUtil.CreateMoveListFromVectorList(moves, fromSquare.Position, _board);
		}
	}
}
