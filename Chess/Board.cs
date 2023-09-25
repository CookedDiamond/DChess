using DChess.Chess.Pieces;
using DChess.Chess.Variants;
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
		private Square[,] _squares;
		public Vector2Int Size { get; set; }

		// Button interacton
		private Square _selectedSquare = null;
		public List<Vector2Int> legalMovesWithSelected { get; private set; }

		private bool _isWhitesTurn = true;

		public List<Variant> Variants { get; set; }

		public Board(Vector2Int size) {
			_squares = new Square[size.x, size.y];
			Size = size;
			legalMovesWithSelected = new List<Vector2Int>();
			Variants = new List<Variant>();
			Initialize();
		}

		public void Initialize() {
			for (int x = 0; x < Size.x; x++) {
				for (int y = 0; y < Size.y; y++) {
					byte b = (byte)((byte)(x + 9 * y) % 2);
					TeamType teamType = (b == 0) ? TeamType.White : TeamType.Black;
					_squares[x, y] = new Square(this, new Vector2Int(x, y), teamType);
				}
			}
		}

		public void PlacePiece(Vector2Int position, Piece piece) {
			GetSquare(position).SetPiece(piece);
		}

		public bool RemovePiece(Vector2Int position) {
			return GetSquare(position).RemovePiece();
		}

		public void SelectSquare(Square square) {
			if (_selectedSquare == null
				&& square.piece != null
				&& square.piece.team == getTurnTeamType()
				&& square.piece.GetAllLegalMoves(square).Count > 0) {
				legalMovesWithSelected = square.piece.GetAllLegalMoves(square);
				_selectedSquare = square;
			}
			else if (_selectedSquare != null) {
				makeMove(_selectedSquare, square);
				legalMovesWithSelected = new List<Vector2Int>();
				_selectedSquare = null;
			}
		}

		private void makeMove(Square from, Square to) {
			;
			if (legalMovesWithSelected != null) {
				if (!legalMovesWithSelected.Contains(to.position)) {
					return;
				}
			}
			PlacePiece(to.position, _selectedSquare.piece);
			if (hasTeamWon() != null) {
				Debug.WriteLine($"Team {hasTeamWon()} has won!");
			}
			RemovePiece(_selectedSquare.position);
			_isWhitesTurn = !_isWhitesTurn;
		}

		private TeamType getTurnTeamType() {
			return _isWhitesTurn ? TeamType.White : TeamType.Black;
		}

		public Square GetSquare(Vector2Int position) {
			if (!IsInBounds(position)) {
				return null;
			}
			return _squares[position.x, position.y];
		}

		public Square[,] GetSquares() {
			return _squares;
		}

		public bool IsInBounds(Vector2Int vector) {
			return vector.x >= 0
				&& vector.y >= 0
				&& vector.x < Size.x
				&& vector.y < Size.y;
		}
		public bool IsStartingPawnRow(TeamType team, int row) {
			if (team == TeamType.White && row == 1
				|| team == TeamType.Black && row == Size.y - 2) {
				return true;
			}
			return false;
		}

		private List<Vector2Int> getPiecesFromType(PieceType pieceType, TeamType teamType) {
			List<Vector2Int> result = new();
			foreach (var square in _squares) {
				if (square.piece != null
					&& square.piece.type == pieceType
					&& square.piece.team == teamType) {
					result.Add(square.position);
				}
			}
			return result;
		}

		private TeamType? hasTeamWon() {
			var blackKingList = getPiecesFromType(PieceType.King, TeamType.Black);
			var whiteKingList = getPiecesFromType(PieceType.King, TeamType.White);

			if (blackKingList.Count == 0) {
				return TeamType.White;
			}
			if (blackKingList.Count == 0) {
				return TeamType.Black;
			}
			return null;
		}

		public void Build8x8StandardBoard() {
			for (int i = 0; i < Size.x; i++) {
				PlacePiece(new Vector2Int(i, 1), new PiecePawn(TeamType.White, this));
				PlacePiece(new Vector2Int(i, Size.x - 2), new PiecePawn(TeamType.Black, this));
				if (i == 0 || i == Size.x - 1) {
					PlacePiece(new Vector2Int(i, 0), new PieceRook(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.x - 1), new PieceRook(TeamType.Black, this));
				}
				else if (i == 2 || i == Size.x - 3) {
					PlacePiece(new Vector2Int(i, 0), new PieceBishop(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.x - 1), new PieceBishop(TeamType.Black, this));
				}
				else if (i == 1 || i == Size.x - 2) {
					PlacePiece(new Vector2Int(i, 0), new PieceKnight(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.x - 1), new PieceKnight(TeamType.Black, this));
				}
				else if (i == 3) {
					PlacePiece(new Vector2Int(i, 0), new PieceQueen(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.x - 1), new PieceQueen(TeamType.Black, this));
				}
				else if (i == Size.x - 4) {
					PlacePiece(new Vector2Int(i, 0), new PieceKing(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.x - 1), new PieceKing(TeamType.Black, this));
				}
			}
		}

		public static Vector2Int GetTeamDirection(TeamType team) {
			return team switch {
				TeamType.White => Vector2Int.UP,
				TeamType.Black => Vector2Int.DOWN,
				_ => throw new NotImplementedException()
			};
		}

	}



	public enum TeamType {
		White,
		Black
	}
}
