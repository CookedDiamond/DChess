using DChess.Util;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace DChess.UI {

	public abstract class Button {
		public delegate void OnButtonClicked();

		public event OnButtonClicked OnClickEvent;

		public Button() {
		}

		public void TriggerButton(Vector2Int mousePosition) {
			if (GetButtonRectangle().Intersects(new Rectangle(mousePosition.x, mousePosition.y, 1, 1))) {
				OnClickEvent?.Invoke();
			}
		}

		protected abstract Rectangle GetButtonRectangle();
	}
}
