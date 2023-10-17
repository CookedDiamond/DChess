using DChess.Chess.Pieces;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using DChess.Chess.Playground;

namespace DChess.Util
{
    public static class ChessUtil {

		public static List<T> CombineLists<T>(List<T> moves1, List<T> moves2) {
			foreach (var move in moves2) {
				if (!moves1.Contains(move)) {
					moves1.Add(move);
				}
			}
			return moves1;
		}

		public static T ChoseRandomElement<T>(List<T> list) {
			int randomNumber = new Random().Next(list.Count);
			return list[randomNumber];
		}

		public static List<Move> CreateMoveListFromVectorList(List<Vector2Int> destinations, Vector2Int startPos, Board board) {
			List<Move> moves = new();

			foreach (var destination in destinations) {
				moves.Add(new Move(startPos, destination));
			}

			return moves;
		}

		public static List<Vector2Int> CreateDestinationsListFromMoveList(List<Move> moves) {
			return moves.Select(move => move.destination).ToList();
		}

		public static Board CloneBoard(Board board) {
			Board returnBoard = new(board.Size) {
				IsWhitesTurn = board.IsWhitesTurn
			};

			for (int x = 0; x < board.Size.x; x++) {
				for (int y = 0; y < board.Size.y; y++) {
					returnBoard.SquareMap[x, y] = board.SquareMap[x, y];
				}
			}

			foreach (var pair in board.Pieces) {
				var oldPiece = pair.Value;
				returnBoard.PlacePiece(pair.Key, Piece.GetPieceFromType(oldPiece.Type, oldPiece.Team, returnBoard));
			}

			foreach (var variant in board.Variants) {
				returnBoard.Variants.Add(variant);
			}

			return returnBoard;
		}

	}
}
