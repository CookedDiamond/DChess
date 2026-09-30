using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using DChess.Chess.Pieces;
using System.Numerics;

namespace DChess.Chess.Variants
{
	public class VariantBattleRoyale : Variant {

		private readonly int _decreasingIntervall;
		private readonly float _decreasingStrenth;
		public int Interval => _decreasingIntervall;
		public float Strength => _decreasingStrenth;
        public override bool UseStandardDrawRules => false;

		public VariantBattleRoyale(int decreasingIntervall, float decreasingStrenth = 1f) {
			if (decreasingIntervall <= 0) throw new ArgumentOutOfRangeException(nameof(decreasingIntervall));
			if (!float.IsFinite(decreasingStrenth) || decreasingStrenth <= 0)
				throw new ArgumentOutOfRangeException(nameof(decreasingStrenth));
			_decreasingIntervall = decreasingIntervall;
			_decreasingStrenth = decreasingStrenth;
		}

		public override void AfterTurnUpdate(Board board, Move move) {
			var offset = board.GetCenter();
			float halfSize = Math.Max(offset.X, offset.Y);
			float outsideCornerDistance = (float) Math.Sqrt(halfSize * halfSize * 2f);
			if (board.GetMoveCount() % _decreasingIntervall == 0 ) {
				int decreaseCounter = board.GetMoveCount() / _decreasingIntervall;
				var changes = new List<BoardChange>();
				for (int x = 0; x < board.Size.x; x++) {
					for (int y = 0; y < board.Size.y; y++) {
						Vector2 offsetPos = new Vector2(x - offset.X +.5f, y - offset.Y + .5f);
						float distance = (float)Math.Sqrt(offsetPos.X * offsetPos.X + offsetPos.Y * offsetPos.Y);
						if (distance >= outsideCornerDistance - decreaseCounter * _decreasingStrenth) {
							if (board.SquareMap[x, y] == SquareType.Disabled) continue;
							var position = new Vector2Int(x, y);
							changes.Add(new BoardChange(position, board.GetPiece(position), Piece.NULL_PIECE,
								board.SquareMap[x, y], SquareType.Disabled));
						}
					}
				}
				if (changes.Count > 0) board.AddToLastMove(changes);
			}
		}

		public override Variant Clone() {
			var variant = new VariantBattleRoyale(_decreasingIntervall, _decreasingStrenth);
			return variant;
		}
	}
}
