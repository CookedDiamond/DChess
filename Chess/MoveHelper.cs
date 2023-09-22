using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess {
	public static class MoveHelper {

		public static List<Vector2Int> CombineMoves(List<Vector2Int> moves1, List<Vector2Int> moves2) {
			foreach (var move in moves2) {
				if (!moves1.Contains(move)) {
					moves1.Add(move);
				}
			}
			return moves1;
		}

	}
}
