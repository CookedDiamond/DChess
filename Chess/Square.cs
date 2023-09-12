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

		public bool SetPiece(Piece piece) {
			if (this.piece != null) {
				return false;
			}
			this.piece = piece;
			return true;
		}

		public bool RemovePiece() {
			if (piece == null) {
				return false;
			}
			piece = null;
			return true;
		}

		private void OnClick() {
			_board.SelectSquare(this);
		}

		public override string ToString() {
			return $"square: {position} piece: {piece}";
		}
	}
}
