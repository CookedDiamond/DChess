using DChess.UI;
using DChess.Util;
using System.Diagnostics;

namespace DChess.Chess {
	public class Square {
		private readonly Board _board;
		public Vector2Int position { get; set; }
		public TeamType team { get; set; }
		public Piece piece { get; private set; }
		private Button _button;

		public Square(Board board, Vector2Int position, TeamType team, ButtonManager buttonManager) {
			_board = board;
			this.position = position;
			this.team = team;
			_button = new BoardButton(position);
			_button.OnClickEvent += () => OnClick();
			buttonManager.AddButton(_button);
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
			if (piece == null) {
				return false;
			}
			if (team != piece.team) {
				return true;
			}
			return false;
		}

		public bool HasPiece() {
			return piece != null;
		}

		private void OnClick() {
			_board.SelectSquare(this);
		}

		public override string ToString() {
			return $"square: {position} piece: {piece}";
		}
	}
}
