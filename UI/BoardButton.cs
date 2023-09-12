using DChess.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI {
	public class BoardButton : Button {
		private readonly Vector2Int _position;

		public BoardButton(Vector2Int position) : base() {
			_position = position;
		}

		protected override Rectangle GetButtonRectangle() {
			GameScaling gameScaling = GameScaling.Instance;

			Vector2Int recPosition = new Vector2Int(gameScaling.GetWindowPositionFromBoard(_position));
			int x =  recPosition.x;
			int y = recPosition.y;
			int size = (int)(gameScaling._squareSize * gameScaling.Scale);

			return new Rectangle(x, y, size, size);
		}
	}
}
