using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess {
	public class Move {

		private Board _board;
		public Vector2Int origin {get; private set;}
		public Vector2Int destination { get; private set; }

		public Move(Board board, Vector2Int origin, Vector2Int destination) {
			_board = board;
			this.origin = origin;
			this.destination = destination;
		}

		public override string ToString() {
			return $"Move: from {origin}, to {destination}";
		}

		public void ApplyMove(bool networkMove = true) {
			_board.MakeMove(this, networkMove);
		}
	}
}
