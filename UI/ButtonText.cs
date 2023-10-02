using DChess.Extensions;
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
		private Vector2Int _offset;
		private Vector2Int _size;
		private WindowAlignment _alignment;
		private Color _textColor;
		private Color _backGroundColor;

		private readonly GameScaling _gameScaling;

		public ButtonText(string text, WindowAlignment alignment, Vector2Int offset, Vector2Int size, Color textColor, Color backGroundColor) : base() {
			_text = text;
			_alignment = alignment;
			_size = size;
			_textColor = textColor;
			_backGroundColor = backGroundColor;
			_offset = offset;

			_gameScaling = GameScaling.Instance;
		}

		public ButtonText(string text, WindowAlignment alignment, Vector2Int size) : this(text, alignment, Vector2Int.ZERO, size, Color.Black, Color.Transparent) { }

		protected override Rectangle GetButtonRectangle() {
			Vector2Int pos = new(_gameScaling.GetWindowPositionFromAlignment(_alignment, _size));
			float scale = _gameScaling.Scale;

			return new Rectangle(pos.x + (int)(_offset.x * scale), pos.y + (int)(_offset.y * scale), 
				(int)(_size.x * scale), (int)(_size.y * scale));
		}



		public void Draw(SpriteBatch spriteBatch) {
			Rectangle rect = GetButtonRectangle();
			Vector2 pos = new Vector2(rect.X, rect.Y);
			spriteBatch.DrawSprite(TextureLoader.GetOrGenerateRectangleTexture(_size), pos, _backGroundColor);
			spriteBatch.DrawBoundedText(_text, pos, _textColor, new Vector2Int(rect.Width, rect.Height), Game1.Font);
		}
	}
}
