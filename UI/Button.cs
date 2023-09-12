using DChess.Util;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace DChess.UI {

	public class Button {
		public delegate void OnButtonClicked();

		public event OnButtonClicked OnClickEvent;

		private Rectangle _rectangle;		

		public Button(Rectangle rectangle) {
			_rectangle = rectangle;
		}

		public Button() {
			_rectangle = new Rectangle();
		}

		public void TriggerButton(Vector2Int mousePosition) {
			if (GetButtonRectangle().Intersects(new Rectangle(mousePosition.x, mousePosition.y, 1, 1))) {
				if (OnClickEvent != null) {
					OnClickEvent();
				}
			}
		}

		protected virtual Rectangle GetButtonRectangle() {
			return _rectangle;
		}
	}
}
