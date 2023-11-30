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
	}
}
