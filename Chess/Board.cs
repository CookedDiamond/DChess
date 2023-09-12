using DChess.UI;
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
		private readonly ButtonManager _buttonManager;

		private Square[,] _squares;
		public Vector2Int _size { get; set; }

		// Button interacton
		private Square _selectedSquare = null;

		public Board(Vector2Int size, ButtonManager buttonManager) {
			_squares = new Square[size.x, size.y];
			_size = size;
			_buttonManager = buttonManager;
			Initialize();
		}

		public void Initialize() {
			for (int x = 0; x < _size.x; x++) {
				for (int y = 0; y < _size.y; y++) {
					byte b = (byte)((byte)(x + 9 * y) % 2);
					TeamType teamType = (b == 0) ? TeamType.White : TeamType.Black;
					_squares[x, y] = new Square(this, new Vector2Int(x, y), teamType, _buttonManager);
				}
			}
		}

		public bool PlacePiece(Vector2Int position, Piece piece) {
			return getSquare(position).SetPiece(piece);
		}

		public bool RemovePiece(Vector2Int position) {
			return getSquare(position).RemovePiece();
		}

		public void SelectSquare(Square square) {
			if (_selectedSquare == null && square.piece != null) {
				_selectedSquare = square;
			}
			else if(_selectedSquare != null) {
				if (PlacePiece(square.position, _selectedSquare.piece)) {
					RemovePiece(_selectedSquare.position);
					_selectedSquare = null;
				}
			}
		}

		private Square getSquare(Vector2Int position) {
			return _squares[position.x, position.y];
		}

		public Square[,] GetSquares() {
			return _squares;
		}

		public void Build8x8StandardBoard() {
			for (int i = 0; i < 8; i++) {
				PlacePiece(new Vector2Int(i, 1), new Piece(PieceType.Pawn, TeamType.White));
				PlacePiece(new Vector2Int(i, 6), new Piece(PieceType.Pawn, TeamType.Black));
				if (i == 0 || i == 7) {
					PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.Rook, TeamType.White));
					PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.Rook, TeamType.Black));
				}
				else if (i == 2 || i == 5) {
					PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.Bishop, TeamType.White));
					PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.Bishop, TeamType.Black));
				}
				else if (i == 1 || i == 6) {
					PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.Knight, TeamType.White));
					PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.Knight, TeamType.Black));
				}
				else if (i == 3) {
					PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.Queen, TeamType.White));
					PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.Queen, TeamType.Black));
				}
				else if (i == 4) {
					PlacePiece(new Vector2Int(i, 0), new Piece(PieceType.King, TeamType.White));
					PlacePiece(new Vector2Int(i, 7), new Piece(PieceType.King, TeamType.Black));
				}
			}
		}
	}

	public enum TeamType {
		White,
		Black
	}
}
