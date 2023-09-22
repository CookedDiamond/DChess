using DChess.Util;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess {
	public class Piece {
		public PieceType type { get; set; }

		public TeamType team { get; set; }

		protected readonly Board _board;

		public Piece(PieceType type, TeamType team, Board board) {
			this.type = type;
			this.team = team;
			_board = board;
		}

		public virtual List<Vector2Int> GetAllLegalMoves(Square fromSquare) {
			List<Vector2Int> moves = new() {
			};

			return moves;
		}

		public List<Vector2Int> getMovesInDirection(Square fromSquare, Board board, Vector2Int direction) {
			List<Vector2Int> moves = new();

			Vector2Int move = fromSquare.position + direction;
			while (board.IsInBounds(move)) {
				MoveType result = getMoveType(fromSquare.piece, move);
				if (result == MoveType.Enemy) {
					moves.Add(move);
					break;
				}
				if (result == MoveType.Empty) {
					moves.Add(move);
					move += direction;
				}
				else {
					break;
				}
			}

			return moves;
		}

		protected MoveType getMoveType(Piece piece, Vector2Int destination) {
			if (_board.IsInBounds(destination)) {
				Square square = _board.GetSquare(destination);
				if (square.HasPiece()) {
					if (square.IsPieceEnemyTeam(piece.team)) {
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
			return type switch {
				PieceType.Pawn => TextureLoader.PawnTexture[(int)team],
				PieceType.Bishop => TextureLoader.BishopTexture[(int)team],
				PieceType.Knight => TextureLoader.KnightTexture[(int)team],
				PieceType.Rook => TextureLoader.RookTexture[(int)team],
				PieceType.Queen => TextureLoader.QueenTexture[(int)team],
				PieceType.King => TextureLoader.KingTexture[(int)team],
				_ => throw new NotImplementedException()
			};
		}

		private char typeAsChar() {
			return type switch {
				PieceType.Pawn => 'p',
				PieceType.Bishop => 'b',
				PieceType.Knight => 'n',
				PieceType.Rook => 'r',
				PieceType.Queen => 'q',
				PieceType.King => 'k',
				_ => throw new NotImplementedException()
			};
		}

		private String teamAsString() {
			return team switch {
				TeamType.Black => "black",
				TeamType.White => "white",
				_ => throw new NotImplementedException()
			};
		}

		public override string ToString() {
			return $"type: {typeAsChar()} team: {teamAsString()}";
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
