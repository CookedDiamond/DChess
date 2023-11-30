using DChess.Chess.ChessAI;
using DChess.Chess.Pieces;
using DChess.Chess.Variants;
using DChess.Multiplayer;
using DChess.UI;
using DChess.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Playground {
	public class Board {
		public static readonly TeamType START_TEAM = TeamType.White;
		public static readonly TeamType SECOND_TEAM = TeamType.Black;

		// TODO: Remove Dictionary -> performance
		public readonly Dictionary<Vector2Int, Piece> Pieces = new();
		public SquareType[,] SquareMap;
		public Vector2Int Size { get; set; }
		private List<Move> _moveHistory = new();
		public bool IsWhitesTurn => _moveHistory.Count % 2 == (START_TEAM == TeamType.White ? 0 : 1);
		public List<Variant> Variants { get; set; }

		public Board(Vector2Int size) {
			Size = size;
			Variants = new List<Variant>();
			SquareMap = new SquareType[size.x, size.y];
		}

		public void PlacePiece(Vector2Int position, Piece piece) {
			Pieces[position] = piece;
		}

		public bool RemovePiece(Vector2Int position) {
			return Pieces.Remove(position);
		}

		public void RemoveSquare(Vector2Int position) {
			SquareMap[position.x, position.y] = SquareType.Disabled;
			Pieces.Remove(position);
		}

		public Vector2 GetCenter() {
			return new Vector2(Size.x / 2f, Size.y / 2f);
		}

		public bool MakeMove(Move move) {
			Piece piece = GetPiece(move.origin);

			if (piece == Piece.NULL_PIECE) {
				Debug.WriteLine("Illegal move!");
				return false;
			}
			List<Move> legalMoves = piece.GetAllLegalMoves(move.origin);
			if (!legalMoves.Contains(move)) {
				Debug.WriteLine("Illegal move!");
				return false;
			}
			PlacePiece(move.destination, piece);
			RemovePiece(move.origin);

			afterTurnUpdate(move);
			return true;
		}

		private void afterTurnUpdate(Move lastMove) {
			_moveHistory.Add(lastMove);

			foreach (var variant in Variants) {
				variant.AfterTurnUpdate(this);
			}
		}

		public void MakeComputerMove() {
			if (HasTeamWon() != TeamType.None) return;
			var algo = new MinMaxRecursive();
			var move = algo.GetBestMove(this);
			MakeMove(move);
		}

		public TeamType GetTurnTeamType() {
			return IsWhitesTurn ? START_TEAM : SECOND_TEAM;
		}

		public Piece GetPiece(Vector2Int position) {
			if (!IsValidPosition(position)) {
				return Piece.NULL_PIECE;
			}
			if (Pieces.TryGetValue(position, out Piece piece)) {
				return piece;
			}
			return Piece.NULL_PIECE;
		}

		public Dictionary<Vector2Int, Piece> GetPieceDictionary() {
			return Pieces;
		}

		public List<KeyValuePair<Vector2Int, Piece>> GetAllPiecesFromTeam (TeamType teamType) {
			List<KeyValuePair<Vector2Int, Piece>> result = new();
			foreach (var keyValuePair in Pieces) {
				if (keyValuePair.Value.Team == teamType) {
					result.Add(keyValuePair);
				}
			}

			return result;
		}

		public List<Move> GetAllLegalMovesForTeam(TeamType team) {
			List<KeyValuePair<Vector2Int, Piece>> teamPieces = GetAllPiecesFromTeam(team);
			List<Move> result = new();
			foreach (var pair in teamPieces) {
				result.AddRange(pair.Value.GetAllLegalMoves(pair.Key));
			}
			return result;
		}

		public List<KeyValuePair<Vector2Int, Piece>> GetPiecesFromTeamWithType(PieceType pieceType, TeamType teamType) {
			List<KeyValuePair<Vector2Int, Piece>> result = new();
			foreach (var pair in Pieces) {
				if (pair.Value != Piece.NULL_PIECE
					&& pair.Value.Type == pieceType
					&& pair.Value.Team == teamType) {
					result.Add(pair);
				}
			}
			return result;
		}

		public int GetMoveCount() {
			return _moveHistory.Count;
		}

		public Move GetLastMove() {
			return _moveHistory.Last();
		}
		public float GetEvaluaton() {
			return new StandartEvaluation(this).GetEvaluation();
		}

		public int GetTotalPieceCount() {
			return Pieces.Count;
		}

		public bool IsValidPosition(Vector2Int vector) {
			bool inBoundsLocation = vector.x >= 0
									&& vector.y >= 0
									&& vector.x < Size.x
									&& vector.y < Size.y;
			if (inBoundsLocation && SquareMap[vector.x, vector.y] == SquareType.Disabled) return false;
			return inBoundsLocation;
		}
		public bool IsStartingPawnRow(TeamType team, int row) {
			if (team == TeamType.White && row == 1
				|| team == TeamType.Black && row == Size.y - 2) {
				return true;
			}
			return false;
		}

		public TeamType HasTeamWon() {
			var blackKingList = GetPiecesFromTeamWithType(PieceType.King, TeamType.Black);
			var whiteKingList = GetPiecesFromTeamWithType(PieceType.King, TeamType.White);

			if (blackKingList.Count == 0) {
				return TeamType.White;
			}
			if (whiteKingList.Count == 0) {
				return TeamType.Black;
			}
			return TeamType.None;
		}

		public Board CloneBoard() {
			Board returnBoard = new(Size);

			for (int x = 0; x < Size.x; x++) {
				for (int y = 0; y < Size.y; y++) {
					returnBoard.SquareMap[x, y] = SquareMap[x, y];
				}
			}

			foreach (var pair in Pieces) {
				var oldPiece = pair.Value;
				returnBoard.PlacePiece(pair.Key, Piece.GetPieceFromType(oldPiece.Type, oldPiece.Team, returnBoard));
			}

			foreach (var move in _moveHistory) {
				returnBoard._moveHistory.Add(move);
			}

			foreach (var variant in Variants) {
				returnBoard.Variants.Add(variant.Clone());
			}

			return returnBoard;
		}

		public static Vector2Int GetTeamDirection(TeamType team) {
			return team switch {
				TeamType.White => Vector2Int.UP,
				TeamType.Black => Vector2Int.DOWN,
				_ => throw new NotImplementedException()
			};
		}

		public override string ToString() {
			string result = "";
			for (int y = 0; y < Size.y; y++) {
				for (int x = 0; x < Size.x; x++) {
					Vector2Int position = new (x, y);
					if (GetPiece(position) == Piece.NULL_PIECE) {
						result += ".";
					}
					else {
						result += Piece.TypeAsChar(GetPiece(position));
					}
				}
				result += "\n";
			}

			return result;
		}

	}

	public enum SquareType {
		Normal,
		Disabled
	}

	public enum TeamType {
		White,
		Black,
		None
	}
}
