using DChess.Chess.Pieces;
using DChess.UI;
using DChess.Util;
using System.Diagnostics;

namespace DChess.Chess
{
    public class Square {
		private readonly Board _board;
		public Vector2Int position { get; set; }
		public TeamType team { get; set; }
		public Piece piece { get; private set; }

		public Square(Board board, Vector2Int position, TeamType team) {
			_board = board;
			this.position = position;
			this.team = team;
		}

		public void SetPiece(Piece piece) {
			this.piece = piece;
		}

		public bool RemovePiece() {
			if (piece == null) {
				return false;
			}
			piece = null;
			return true;
		}

		public bool IsPieceEnemyTeam(TeamType team) {
			bool result;
			if (piece == null || team == piece.team) {
				result = false;
			}
			else {
				result = true;
			}
			foreach (var variant in _board.Variants) {
				result = variant.IsPieceEnemyTeam(result, piece);
			}
			return result;
		}

		public bool HasPiece() {
			return piece != null;
		}

		public void OnClick() {
			_board.SelectSquare(this);
		}

		public override string ToString() {
			return $"square: {position} piece: {piece}";
		}
	}
}
