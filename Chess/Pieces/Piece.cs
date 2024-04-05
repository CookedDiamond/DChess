using DChess.Chess.Playground;
using DChess.Util;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DChess.Chess.Pieces {
	public abstract class Piece {

		public static readonly Piece NULL_PIECE = new PieceNull();

		public PieceType Type { get; set; }

		public TeamType Team { get; set; }

		public int MoveCount { get; set; }

		protected readonly Board _board;

		public Piece(PieceType type, TeamType team, Board board) {
			Type = type;
			Team = team;
			_board = board;
			MoveCount = 0;
		}

		public virtual List<Move> GetAllLegalMoves(Vector2Int fromPosition) {
			List<Move> moves = new();
			foreach (var variant in _board.Variants) {
				var newMoves = variant.AdditionalMoves(_board, this, fromPosition);
				if (newMoves != null) {
					moves.AddRange(newMoves);
				}
			}

			return moves;
		}

		public virtual Move GetMove(Vector2Int fromPosition, Vector2Int toPosition) {
			var moves = GetAllLegalMoves(fromPosition);
			foreach (var move in moves) {
				bool containsFromPos = false;
				bool containsToPos = false;

				foreach (var change in move.Changes)
				{
					if (change.boardPosition == toPosition) {
						containsToPos = true;
					}
					if (change.boardPosition == fromPosition) {
						containsFromPos = true;
					}
					if (containsFromPos && containsToPos) { 
						return move;
					}
				}
				
			}

			return null;
		}

		protected List<Move> getMovesInDirection(Vector2Int fromPosition, Vector2Int direction) {
			List<Vector2Int> destinations = new();

			Vector2Int destination = fromPosition + direction;
			while (_board.IsValidPosition(destination)) {
				MoveType result = getMoveType(destination);
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

			return ChessUtil.CreateMoveListFromVectorList(destinations, fromPosition, _board);
		}

		public virtual bool IsEnemyTeam(TeamType team) {
			bool result;
			if (team == Team) {
				result = false;
			}
			else {
				result = true;
			}
			foreach (var variant in _board.Variants) {
				result = variant.IsPieceEnemyTeam(result, this);
			}
			return result;
		}

		protected MoveType getMoveType(Vector2Int destination) {
			if (_board.IsValidPosition(destination)) {
				var piece = _board.GetPiece(destination);
				if (piece != NULL_PIECE) {
					if (IsEnemyTeam(piece.Team)) {
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

		public abstract float GetPieceScore();

		public abstract float GetPieceScore(Board board, Vector2Int position, TeamType team);

		public override string ToString() {
			return $"type: {TypeAsChar(this)} team: {teamAsString(this)}";
		}

		public static Texture2D GetPieceTexture(Piece piece) {
			return piece.Type switch {
				PieceType.Pawn => TextureLoader.PawnTexture[(int)piece.Team],
				PieceType.Bishop => TextureLoader.BishopTexture[(int)piece.Team],
				PieceType.Knight => TextureLoader.KnightTexture[(int)piece.Team],
				PieceType.Rook => TextureLoader.RookTexture[(int)piece.Team],
				PieceType.Queen => TextureLoader.QueenTexture[(int)piece.Team],
				PieceType.King => TextureLoader.KingTexture[(int)piece.Team],
				_ => throw new NotImplementedException()
			};
		}

		public static char TypeAsChar(Piece piece) {
			return piece.Type switch {
				PieceType.Pawn => 'p',
				PieceType.Bishop => 'b',
				PieceType.Knight => 'n',
				PieceType.Rook => 'r',
				PieceType.Queen => 'q',
				PieceType.King => 'k',
				PieceType.None => 'E',
				_ => throw new NotImplementedException()
			};
		}

		private static string teamAsString(Piece piece) {
			return piece.Team switch {
				TeamType.Black => "black",
				TeamType.White => "white",
				TeamType.None => "none",
				_ => throw new NotImplementedException()
			};
		}

		private Piece getPieceFromType() {
			return Type switch { 
				PieceType.Pawn => new PiecePawn(Team, _board),
				PieceType.Bishop => new PieceBishop(Team, _board),
				PieceType.Knight => new PieceKnight(Team, _board),
				PieceType.Rook => new PieceRook(Team, _board),
				PieceType.Queen => new PieceQueen(Team, _board),
				PieceType.King => new PieceKing(Team, _board),
				PieceType.None => NULL_PIECE,
				_ => throw new NotImplementedException()	
			};
		}

		public Piece ClonePiece() {
			Piece clonedPiece = getPieceFromType();
			clonedPiece.MoveCount = MoveCount;
			return clonedPiece;
		}
	}

	public enum PieceType {
		Pawn,
		Bishop,
		Knight,
		Rook,
		Queen,
		King,
		None
	}

	public enum MoveType {
		Empty,
		Enemy,
		OutOfBounds,
		TeamMate
	}
}
