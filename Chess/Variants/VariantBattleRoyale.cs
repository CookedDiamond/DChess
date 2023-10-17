using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Numerics;

namespace DChess.Chess.Variants
{
    public class VariantBattleRoyale : Variant {

		private readonly int _decreasingIntervall;
		private readonly float _decreasingStrenth;
		private int decreaseCounter = 0;

		public VariantBattleRoyale(int decreasingIntervall, float decreasingStrenth = 1f) {
			_decreasingIntervall = decreasingIntervall;
			_decreasingStrenth = decreasingStrenth;
		}

		public override void AfterTurnUpdate(Board board) {
			base.AfterTurnUpdate(board);
			var offset = board.GetCenter();
			float halfSize = Math.Max(offset.X, offset.Y);
			float outsideCornerDistance = (float) Math.Sqrt(halfSize * halfSize * 2f);
			if (board.TurnCount % _decreasingIntervall == 0 ) {
				decreaseCounter++;
				for (int x = 0; x < board.Size.x; x++) {
					for (int y = 0; y < board.Size.y; y++) {
						Vector2 offsetPos = new Vector2(x - offset.X +.5f, y - offset.Y + .5f);
						float distance = (float)Math.Sqrt(offsetPos.X * offsetPos.X + offsetPos.Y * offsetPos.Y);
						if (distance >= outsideCornerDistance - decreaseCounter * _decreasingStrenth) {
							board.RemoveSquare(new Vector2Int(x, y));
						}
					}
				}
			}
		}
	}
}
