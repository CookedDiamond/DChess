using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess {
	public class Square {
		public Vector2Int position { get; set; }
		public TeamType team { get; set; }
		public Piece piece { get; private set; }

		public Square(Vector2Int position, TeamType team) {
			this.position = position;
			this.team = team;
		}

		public bool SetPiece(Piece piece) {
			if (this.piece != null) {
				return false;
			}
			this.piece = piece;
			return true;
		}

		public override string ToString() {
			return $"square: {position} piece: {piece}";
		}
	}
}
