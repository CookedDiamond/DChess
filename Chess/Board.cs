using DChess.Chess.ChessAI;
using DChess.Chess.Pieces;
using DChess.Chess.Variants;
using DChess.Multiplayer;
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
		public bool IsWhitesTurn = true;
		public List<Variant> Variants { get; set; }

		// UI interacton
		private Square _selectedSquare = null;
		public List<Vector2Int> legalMovesWithSelected { get; private set; }

		// Networking
		public ChessClient ChessClient { get; set; }

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
				&& square.Piece != null
				&& square.Piece.Team == GetTurnTeamType()
				&& square.Piece.GetAllLegalMoves(square).Count > 0) {
				legalMovesWithSelected = ChessUtil.CreateDestinationsListFromMoveList(square.Piece.GetAllLegalMoves(square));
				_selectedSquare = square;
			}
			else if (_selectedSquare != null) {
				makeMove(_selectedSquare, square);
				legalMovesWithSelected = new List<Vector2Int>();
				_selectedSquare = null;
			}
		}

		private void makeMove(Square from, Square to, bool networkMove = true) {
			MakeMove(new Move(from.Position, to.Position), networkMove);
		}

		public void MakeMove(Move move, bool networkMove = true) {
			Square from = GetSquare(move.origin);

			if (from.Piece == null) {
				Debug.WriteLine("Illegal move!");
				return;
			}
			List<Move> legalMoves = from.Piece.GetAllLegalMoves(from);
			if (!legalMoves.Contains(move)) {
				Debug.WriteLine("Illegal move!");
				return;
			}
			PlacePiece(move.destination, from.Piece);
			RemovePiece(move.origin);
			IsWhitesTurn = !IsWhitesTurn;
			afterTurnUpdate();
      
			if (networkMove) {
				ChessClient?.SendMove(move);
			}
		}

		// No sideways support.
		private void afterTurnUpdate() {
			foreach (var variant in Variants) {
				variant.AfterTurnUpdate(this);
			}
		}

		public void MakeComputerMove() {
			var algo = new MinMaxRecursive();
			var move = algo.GetBestMove(this);
			MakeMove(move);
		}

		public TeamType GetTurnTeamType() {
			return IsWhitesTurn ? TeamType.White : TeamType.Black;
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

		public List<Square> GetAllSquaresWithTeamPieces(TeamType team) {
			List<Square> result = new ();
			foreach (Square square in _squares) {
				if (square.Piece != null && square.Piece.Team == team) {
					result.Add(square);
				}
			}
			return result;
		}

		public List<Move> GetAllLegalMovesForTeam(TeamType team) {
			List<Square> teamPieces = GetAllSquaresWithTeamPieces(team);
			List<Move> result = new ();	
			foreach (var square in teamPieces) {
				result.AddRange(square.Piece.GetAllLegalMoves(square));
			}
			return result;
		}

		public float GetEvaluaton() {
			return new StandartEvaluation(this).GetEvaluation();
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

		public List<Square> GetPieceSquares(PieceType pieceType, TeamType teamType) {
			List<Square> result = new();
			foreach (var square in _squares) {
				if (square.Piece != null
					&& square.Piece.Type == pieceType
					&& square.Piece.Team == teamType) {
					result.Add(square);
				}
			}
			return result;
		}

		public TeamType? HasTeamWon() {
			var blackKingList = GetPieceSquares(PieceType.King, TeamType.Black);
			var whiteKingList = GetPieceSquares(PieceType.King, TeamType.White);

			if (blackKingList.Count == 0) {
				return TeamType.White;
			}
			if (whiteKingList.Count == 0) {
				return TeamType.Black;
			}
			return null;
		}

		public void Build8x8StandardBoard() {
			for (int i = 0; i < Size.x; i++) {
				PlacePiece(new Vector2Int(i, 1), new PiecePawn(TeamType.White, this));
				PlacePiece(new Vector2Int(i, Size.y - 2), new PiecePawn(TeamType.Black, this));
				if (i == 0 || i == Size.x - 1) {
					PlacePiece(new Vector2Int(i, 0), new PieceRook(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.y - 1), new PieceRook(TeamType.Black, this));
				}
				else if (i == 2 || i == Size.x - 3) {
					PlacePiece(new Vector2Int(i, 0), new PieceBishop(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.y - 1), new PieceBishop(TeamType.Black, this));
				}
				else if (i == 1 || i == Size.x - 2) {
					PlacePiece(new Vector2Int(i, 0), new PieceKnight(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.y - 1), new PieceKnight(TeamType.Black, this));
				}

				if (i == 3) {
					PlacePiece(new Vector2Int(i, 0), new PieceQueen(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.y - 1), new PieceQueen(TeamType.Black, this));
				}
				if (i == Size.x - 4) {
					PlacePiece(new Vector2Int(i, 0), new PieceKing(TeamType.White, this));
					PlacePiece(new Vector2Int(i, Size.y - 1), new PieceKing(TeamType.Black, this));
				}
			}
		}

		public void BuildSmallBoard()
		{
			int center = Size.x / 2;

            for (int i = 0; i < Size.x; i++)
            {
                PlacePiece(new Vector2Int(i, 1), new PiecePawn(TeamType.White, this));
                PlacePiece(new Vector2Int(i, Size.y - 2), new PiecePawn(TeamType.Black, this));
                if (i == 0 || i == Size.x - 1)
                {
                    PlacePiece(new Vector2Int(i, 0), new PieceRook(TeamType.White, this));
                    PlacePiece(new Vector2Int(i, Size.y - 1), new PieceRook(TeamType.Black, this));
                }
                else if (i == 2 || i == Size.x - 3)
                {
                    PlacePiece(new Vector2Int(i, 0), new PieceBishop(TeamType.White, this));
                    PlacePiece(new Vector2Int(i, Size.y - 1), new PieceBishop(TeamType.Black, this));
                }
                else if (i == 1 || i == Size.x - 2)
                {
                    PlacePiece(new Vector2Int(i, 0), new PieceKnight(TeamType.White, this));
                    PlacePiece(new Vector2Int(i, Size.y - 1), new PieceKnight(TeamType.Black, this));
                }

				if (i == center + 1)
				{
                    PlacePiece(new Vector2Int(i, 0), new PieceKnight(TeamType.White, this));
                    PlacePiece(new Vector2Int(i, Size.y - 1), new PieceKnight(TeamType.Black, this));
                }
                if (i == center - 2)
                {
                    PlacePiece(new Vector2Int(i, 0), new PieceBishop(TeamType.White, this));
                    PlacePiece(new Vector2Int(i, Size.y - 1), new PieceBishop(TeamType.Black, this));
                }

                if (i == center - 1)
                {
                    PlacePiece(new Vector2Int(i, 0), new PieceQueen(TeamType.White, this));
                    PlacePiece(new Vector2Int(i, Size.y - 1), new PieceQueen(TeamType.Black, this));
                }
                if (i == center)
                {
                    PlacePiece(new Vector2Int(i, 0), new PieceKing(TeamType.White, this));
                    PlacePiece(new Vector2Int(i, Size.y - 1), new PieceKing(TeamType.Black, this));
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

        public override string ToString()
        {
			string result = "";
            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
					Vector2Int pos = new Vector2Int(x, y);
					if (GetSquare(pos).Piece == null)
					{
						result += ".";
					}
                    else if (GetSquare(pos).Piece.Type == PieceType.King)
                    {
                        result += "k";
                    }
                    else if (GetSquare(pos).Piece.Type == PieceType.Queen)
                    {
                        result += "q";
                    }
                }
				result += "\n";
            }
            

            return result;
        }

    }



	public enum TeamType {
		White,
		Black
	}
}
