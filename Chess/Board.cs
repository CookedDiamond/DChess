using DChess.Util;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess {
	public class Board {
		private Square[,] _squares;
		public Vector2Int _size { get; set; }

		public Board(Vector2Int size) {
			_squares = new Square[size.x, size.y];
			_size = size;
		}

		public void Initialize() {
			for (int x = 0; x < _size.x; x++) {
				for (int y = 0; y < _size.y; y++) {
					byte b = (byte)((byte)(x + 9 * y) % 2);
					TeamType teamType = (b == 0) ? TeamType.White : TeamType.Black;
					_squares[x, y] = new Square(new Vector2Int(x, y), teamType);
				}
			}
		}

		public bool PlacePiece(Vector2Int position, Piece piece) {
			return getSquare(position).SetPiece(piece);
		}

		private Square getSquare(Vector2Int position) {
			return _squares[position.x, position.y];
		}

		public Square[,] GetSquares() {
			return _squares;
		}
	}

	public enum TeamType {
		White,
		Black
	}
}
