using DChess.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DChess.UI {
	/// <summary>
	/// A button whose position is calculated every frame (so it follows window resizing)
	/// and whose text can change.
	/// </summary>
	public class ButtonRect : Button, IDrawable {
		private readonly Func<Rectangle> _bounds;
		private readonly Func<string> _text;
		private bool _isHovered;

		/// <summary>Disabled buttons are drawn dimmed and can not be clicked.</summary>
		public Func<bool> IsEnabled { get; set; } = () => true;

		/// <summary>Highlighted buttons use the accent color.</summary>
		public Func<bool> IsHighlighted { get; set; } = () => false;

		public ButtonRect(Func<Rectangle> bounds, Func<string> text) : base() {
			_bounds = bounds;
			_text = text;
		}

		public ButtonRect(Func<Rectangle> bounds, string text) : this(bounds, () => text) { }

		protected override Rectangle GetButtonRectangle() {
			return IsEnabled() ? _bounds() : Rectangle.Empty;
		}

		protected override void OnHoverEnter() {
			base.OnHoverEnter();
			_isHovered = true;
		}

		protected override void OnHoverExit() {
			base.OnHoverExit();
			_isHovered = false;
		}

		public void Draw(SpriteBatch spriteBatch) {
			Rectangle rect = _bounds();
			if (rect.Width <= 0 || rect.Height <= 0) return;

			bool enabled = IsEnabled();
			Color background = IsHighlighted()
				? (_isHovered && enabled ? Theme.AccentHover : Theme.Accent)
				: (_isHovered && enabled ? Theme.ButtonHover : Theme.Button);
			if (!enabled) background *= 0.5f;

			spriteBatch.DrawRectangle(rect, background);
			spriteBatch.DrawTextCentered(_text(), rect, rect.Height * 0.55f, enabled ? Theme.Text : Theme.TextDim);
		}
	}
}
