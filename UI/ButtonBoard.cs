using DChess.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI {
	public class ButtonBoard : Button {
		private readonly Vector2Int _position;

		public ButtonBoard(Vector2Int position) : base() {
			_position = position;
		}

		protected override Rectangle GetButtonRectangle() {
			ScalingUtil gameScaling = ScalingUtil.Instance;

			Vector2Int recPosition = new(gameScaling.GetWindowPositionFromBoard(_position));
			int x =  recPosition.x;
			int y = recPosition.y;
			int size = (int)(gameScaling.SquareSize * gameScaling.Scale);

			return new Rectangle(x, y, size, size);
		}
	}
}
