using DChess.Chess.Pieces;
using DChess.UI;
using DChess.Util;
using System.Diagnostics;

namespace DChess.Chess
{
    public class Square {
		private readonly Board _board;
		public Vector2Int Position { get; set; }
		public TeamType TeamColor { get; set; }
		public Piece Piece { get; private set; }

		public Square(Board board, Vector2Int position, TeamType team) {
			_board = board;
			this.Position = position;
			TeamColor = team;
		}

		public void SetPiece(Piece piece) {
			this.Piece = piece;
		}

		public bool RemovePiece() {
			if (Piece == null) {
				return false;
			}
			Piece = null;
			return true;
		}

		public bool IsPieceEnemyTeam(TeamType team) {
			bool result;
			if (Piece == null || team == Piece.Team) {
				result = false;
			}
			else {
				result = true;
			}
			foreach (var variant in _board.Variants) {
				result = variant.IsPieceEnemyTeam(result, Piece);
			}
			return result;
		}

		public bool HasPiece() {
			return Piece != null;
		}

		public void OnClick() {
			_board.SelectSquare(this);
		}

		public override string ToString() {
			return $"square: {Position} piece: {Piece}";
		}
	}
}
