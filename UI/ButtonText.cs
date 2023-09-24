using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI {
	public class ButtonText : Button, IDrawable {

		private string _text;
		private Vector2Int _size;
		private WindowAlignment _alignment;
		private Color _color;

		private readonly GameScaling _gameScaling;

		public ButtonText(string text, WindowAlignment alignment, Vector2Int size, Color color) : base() {
			_text = text;
			_alignment = alignment;
			_size = size;
			_color = color;

			_gameScaling = GameScaling.Instance;
		}

		public ButtonText(string text, WindowAlignment alignment, Vector2Int size) : this(text, alignment, size, Color.Black) { }

		protected override Rectangle GetButtonRectangle() {
			Vector2Int pos = new(_gameScaling.GetWindowPositionFromAlignment(_alignment, _size));

			return new Rectangle(pos.x, pos.y, _size.x, _size.y);
		}



		public void Draw(SpriteBatch spriteBatch) {
			Vector2 pos = _gameScaling.GetWindowPositionFromAlignment(_alignment, _size);

			spriteBatch.DrawString(Game1.Font, _text, pos, _color);
		}
	}
}
