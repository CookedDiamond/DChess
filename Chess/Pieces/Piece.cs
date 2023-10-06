using DChess.Util;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Pieces {
	public class Piece {
		public PieceType Type { get; set; }

		public TeamType Team { get; set; }

		protected readonly Board _board;

		public Piece(PieceType type, TeamType team, Board board) {
			Type = type;
			Team = team;
			_board = board;
		}

		public virtual List<Move> GetAllLegalMoves(Square fromSquare) {
			List<Move> moves = new() {
			};

			return moves;
		}

		protected List<Move> getMovesInDirection(Square fromSquare, Vector2Int direction) {
			List<Vector2Int> destinations = new();

			Vector2Int destination = fromSquare.Position + direction;
			while (_board.IsInBounds(destination)) {
				MoveType result = getMoveType(fromSquare.Piece, destination);
				if (result == MoveType.Enemy) {
					destinations.Add(destination);
					break;
				}
				if (result == MoveType.Empty) {
					destinations.Add(destination);
					destination += direction;
				}
				else {
					break;
				}
			}

			return ChessUtil.CreateMoveListFromVectorList(destinations, fromSquare.Position, _board);
		}

		protected MoveType getMoveType(Piece piece, Vector2Int destination) {
			if (_board.IsInBounds(destination)) {
				Square square = _board.GetSquare(destination);
				if (square.HasPiece()) {
					if (square.IsPieceEnemyTeam(piece.Team)) {
						return MoveType.Enemy;
					}
					return MoveType.TeamMate;
				}
				else {
					return MoveType.Empty;
				}
			}
			return MoveType.OutOfBounds;
		}

		public Texture2D GetPieceTexture() {
			return Type switch {
				PieceType.Pawn => TextureLoader.PawnTexture[(int)Team],
				PieceType.Bishop => TextureLoader.BishopTexture[(int)Team],
				PieceType.Knight => TextureLoader.KnightTexture[(int)Team],
				PieceType.Rook => TextureLoader.RookTexture[(int)Team],
				PieceType.Queen => TextureLoader.QueenTexture[(int)Team],
				PieceType.King => TextureLoader.KingTexture[(int)Team],
				_ => throw new NotImplementedException()
			};
		}

		private char typeAsChar() {
			return Type switch {
				PieceType.Pawn => 'p',
				PieceType.Bishop => 'b',
				PieceType.Knight => 'n',
				PieceType.Rook => 'r',
				PieceType.Queen => 'q',
				PieceType.King => 'k',
				_ => throw new NotImplementedException()
			};
		}

		private string teamAsString() {
			return Team switch {
				TeamType.Black => "black",
				TeamType.White => "white",
				_ => throw new NotImplementedException()
			};
		}

		public override string ToString() {
			return $"type: {typeAsChar()} team: {teamAsString()}";
		}



		public static Piece GetPieceFromType(PieceType type, TeamType team, Board board) {
			return type switch { 
				PieceType.Pawn => new PiecePawn(team, board),
				PieceType.Bishop => new PieceBishop(team, board),
				PieceType.Knight => new PieceKnight(team, board),
				PieceType.Rook => new PieceRook(team, board),
				PieceType.Queen => new PieceQueen(team, board),
				PieceType.King => new PieceKing(team, board),
				_ => throw new NotImplementedException()	
			};
		}
	}

	public enum PieceType {
		Pawn,
		Bishop,
		Knight,
		Rook,
		Queen,
		King
	}

	public enum MoveType {
		Empty,
		Enemy,
		OutOfBounds,
		TeamMate
	}
}
