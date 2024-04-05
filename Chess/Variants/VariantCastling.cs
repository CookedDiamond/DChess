using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Variants {
	public class VariantCastling: Variant {
		private readonly int castlingDistance;

		public VariantCastling(int castlingDistance)
		{
			this.castlingDistance = castlingDistance;
		}

		public override List<Move> AdditionalMoves(Board board, Piece kingPiece, Vector2Int position) {
			if (kingPiece.Type != PieceType.King) return null;
			if (kingPiece.MoveCount > 0) return null;
			if (position.x < castlingDistance || position.x > board.Size.x - castlingDistance) return null;
			List<Move> resultMoves = new ();
			int xPos = position.x;
			
			// - direction
			for (int x = xPos - 1; x >= 0; x--) {
				var sidePiece = board.GetPiece(new Vector2Int(x, position.y));

				if (sidePiece.Type == PieceType.Rook
					&& sidePiece.MoveCount == 0
					&& sidePiece.Team == kingPiece.Team) {
					resultMoves.Add(castlingMove(position, board, kingPiece, sidePiece, x, -1));
				}
				if (sidePiece != Piece.NULL_PIECE) {
					break;
				}
			}
			// + direction
			for (int x = xPos + 1; x < board.Size.x; x++) { 
				var sidePiece = board.GetPiece(new Vector2Int(x, position.y));

				if (sidePiece.Type == PieceType.Rook
					&& sidePiece.MoveCount == 0
					&& sidePiece.Team == kingPiece.Team) {
					resultMoves.Add(castlingMove(position, board, kingPiece, sidePiece, x, 1));
				}
				if (sidePiece != Piece.NULL_PIECE) {
					break;
				}
			}

			return resultMoves;
		}

		private Move castlingMove(Vector2Int position, Board board, Piece kingPiece, Piece sidePiece, int x, int direction)
		{
			Vector2Int castlingResultPosition = new(position.x + direction * castlingDistance, position.y);
			Vector2Int originalRookPosition = new(x, position.y);
			Vector2Int newRookPosition = new(position.x + direction, position.y);

			Move castlingMove = new();
			// Remove King.
			castlingMove.AddChange(new BoardChange(position, kingPiece, Piece.NULL_PIECE));
			// Remove Rook.
			castlingMove.AddChange(new BoardChange(originalRookPosition, sidePiece, Piece.NULL_PIECE));
			// Place King.
			castlingMove.AddChange(new BoardChange(castlingResultPosition, Piece.NULL_PIECE, kingPiece));
			// Place Rook
			castlingMove.AddChange(new BoardChange(newRookPosition, Piece.NULL_PIECE, sidePiece));

			return castlingMove;
		}

	}
}
