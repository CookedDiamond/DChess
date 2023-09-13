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

		public Piece(PieceType type, TeamType team) {
			this.type = type;
			this.team = team;
		}

		public List<Vector2Int> GetAllLegalMoves(Square fromSquare, Board board) {
			return type switch {
				PieceType.Pawn => getPawnMoves(fromSquare, board),
				PieceType.Bishop => getBishopMoves(fromSquare,board),
				PieceType.Knight => getKnightMoves(fromSquare,board),
				PieceType.Rook => getRookMoves(fromSquare,board),
				PieceType.Queen => getQueenMoves(fromSquare,board),
				PieceType.King => getKingMoves(fromSquare,board),
				_ => null
				};
		}

		private List<Vector2Int> getPawnMoves(Square fromSquare, Board board) {
			List<Vector2Int> moves = new();
			// Move straight.
			Vector2Int forward = fromSquare.position + Board.GetTeamDirection(team);
			Vector2Int twoForward = forward + Board.GetTeamDirection(team);
			Square forwardSquare = board.GetSquare(forward);
			Square twoForwardSquare = board.GetSquare(twoForward);
			if (forwardSquare != null && !forwardSquare.HasPiece()) {
				moves.Add(forward);
			}
			if (twoForwardSquare != null
				&& Board.IsStartingPawnRow(team, fromSquare.position.y) 
				&& !twoForwardSquare.HasPiece()) {
				moves.Add(twoForward);
			}

			// Capture sideways.
			Vector2Int leftCapture = forward + Vector2Int.LEFT;
			Vector2Int rightCapture = forward + Vector2Int.RIGHT;
			Square leftCaptureSquare = board.GetSquare(leftCapture);
			Square rightCaptureSquare = board.GetSquare(rightCapture);

			if (leftCaptureSquare != null
				&& leftCaptureSquare.IsPieceEnemyTeam(team)) {
				moves.Add(leftCapture);
			}
			if (rightCaptureSquare != null
				&& rightCaptureSquare.IsPieceEnemyTeam(team)) {
				moves.Add(rightCapture);
			}

			return moves;
		}

		private List<Vector2Int> getBishopMoves(Square fromSquare, Board board) {
			List<Vector2Int> moves = new();

			moves.AddRange(getMovesInDirection(fromSquare, board, new Vector2Int(1, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, board, new Vector2Int(1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, board, new Vector2Int(-1, -1)));
			moves.AddRange(getMovesInDirection(fromSquare, board, new Vector2Int(-1, 1)));

			return moves;
		}

		private List<Vector2Int> getRookMoves(Square fromSquare, Board board) {
			List<Vector2Int> moves = new();

			moves.AddRange(getMovesInDirection(fromSquare, board, new Vector2Int(1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, board, new Vector2Int(-1, 0)));
			moves.AddRange(getMovesInDirection(fromSquare, board, new Vector2Int(0, 1)));
			moves.AddRange(getMovesInDirection(fromSquare, board, new Vector2Int(0, -1)));

			return moves;
		}

		private List<Vector2Int> getQueenMoves(Square fromSquare, Board board) {
			List<Vector2Int> moves = new();

			moves.AddRange(getBishopMoves(fromSquare, board));
			moves.AddRange(getRookMoves(fromSquare, board));

			return moves;
		}

		private List<Vector2Int> getMovesInDirection(Square fromSquare, Board board, Vector2Int direction) {
			List<Vector2Int> moves = new();

			Vector2Int move = fromSquare.position + direction;
			while (board.IsInBounds(move)) {
				MoveType result = getMoveType(fromSquare.piece, move, board);
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

		private List<Vector2Int> getKingMoves(Square fromSquare, Board board) {
			List<Vector2Int> moves = new();

			for (int x = -1; x <= 1; x++) {
				for (int y = -1; y <= 1; y++) {
					if (x == 0 && y == 0) continue;
					Vector2Int currentMove = fromSquare.position + new Vector2Int(x, y);
					MoveType result = getMoveType(fromSquare.piece, currentMove, board);
					if (result == MoveType.Empty || result == MoveType.Enemy) {
						moves.Add(currentMove);
					}
				}
			}

			return moves;
		}

		private List<Vector2Int> getKnightMoves(Square fromSquare, Board board) {
			List<Vector2Int> moves = new();

			for (int i = 0; i < 8; i++) {
				Vector2Int offset = i switch {
					0 => new Vector2Int(2, 1),
					1 => new Vector2Int(2, -1),
					2 => new Vector2Int(1, 2),
					3 => new Vector2Int(-1, 2),
					4 => new Vector2Int(-2, 1),
					5 => new Vector2Int(-2, -1),
					6 => new Vector2Int(1, -2),
					7 => new Vector2Int(-1, -2),
					_ => throw new NotImplementedException()
				};

				Vector2Int currentMove = fromSquare.position + offset;

				MoveType result = getMoveType(fromSquare.piece, currentMove, board);
				if (result == MoveType.Empty || result == MoveType.Enemy) {
					moves.Add(currentMove);
				}
			}

			return moves;
		}

		private MoveType getMoveType(Piece piece, Vector2Int destination, Board board) {
			if (board.IsInBounds(destination)) {
				Square square = board.GetSquare(destination);
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
