using DChess.Util;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace DChess.UI {
	public class Button {
		private Rectangle _rectangle;

		public Button(Rectangle rectangle) {
			_rectangle = rectangle;
		}

		public Button() {
			_rectangle = new Rectangle();
		}

		public void TriggerButton(Vector2Int mousePosition) {
			if (GetButtonRectangle().Intersects(new Rectangle(mousePosition.x, mousePosition.y, 1, 1))) {

				Debug.WriteLine("Button pressed! " + _rectangle.X + " " + _rectangle.Y);
			}
		}

		protected virtual Rectangle GetButtonRectangle() {
			return _rectangle;
		}
	}
}
