using DChess.Util;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;

namespace DChess.UI {

	public abstract class Button {
		public delegate void OnButtonClicked();

		public event OnButtonClicked OnClickEvent;

		private bool isHovered = false;

		public Button() {
		}

		public void TriggerButton(Vector2Int mousePosition) {
			if (mouseOnButton(mousePosition)) {
				OnClickEvent?.Invoke();
			}
		}

		public void HoverButton(Vector2Int mousePosition) {
			if(mouseOnButton(mousePosition) && !isHovered) {
				OnHoverEnter();
			}
			else if (!mouseOnButton(mousePosition) && isHovered) {
				OnHoverExit();
			}
		}

		protected virtual void OnHoverEnter() {
			isHovered = true;
		}

		protected virtual void OnHoverExit() {
			isHovered = false;
		}

		private bool mouseOnButton(Vector2Int mousePosition) {
			return GetButtonRectangle().Intersects(new Rectangle(mousePosition.x, mousePosition.y, 1, 1));
		}

		public void Initialize(ButtonManager buttonManager, OnButtonClicked onClick) {
			OnClickEvent += onClick;
			buttonManager.AddButton(this);
		}

		protected abstract Rectangle GetButtonRectangle();
	}
}
