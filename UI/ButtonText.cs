using DChess.Extensions;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.UI {
	public class ButtonText : Button, IDrawable {

		private string _text;
		private Vector2Int _offset;
		private Vector2Int _size;
		private WindowAlignment _alignment;
		private Color _textColor;
		private Color _storeBackgroundColor;
		private Color _currentBackgroundColor;

		private readonly ScalingUtil _gameScaling;

		private Thread clickEffectThread;

		public ButtonText(string text, WindowAlignment alignment, Vector2Int offset, Vector2Int size, Color textColor, Color backGroundColor) : base() {
			_text = text;
			_alignment = alignment;
			_size = size;
			_textColor = textColor;
			_storeBackgroundColor = backGroundColor;
			_currentBackgroundColor = backGroundColor;
			_offset = offset;

			_gameScaling = ScalingUtil.Instance;

			OnClickEvent += () => clickEffect();
		}

		public ButtonText(string text, WindowAlignment alignment, Vector2Int size) : this(text, alignment, Vector2Int.ZERO, size, Color.Black, Color.Transparent) { }

		protected override Rectangle GetButtonRectangle() {
			Vector2Int pos = new(_gameScaling.GetWindowPositionFromAlignment(_alignment, _size));
			float scale = _gameScaling.Scale;

			return new Rectangle(pos.x + (int)(_offset.x * scale), pos.y + (int)(_offset.y * scale), 
				(int)(_size.x * scale), (int)(_size.y * scale));
		}

		private void clickEffect() {
			if (clickEffectThread == null || clickEffectThread.ThreadState == ThreadState.Stopped) {
				clickEffectThread = new(() => UIEffectsUtil.HighlightBlend(_storeBackgroundColor, setBackGroundColor, 10));
				clickEffectThread.Start();
			}
		}

		protected override void OnHoverEnter() {
			base.OnHoverEnter();
			setBackGroundColor(_storeBackgroundColor.DivideColor(1.5f));
		}

		protected override void OnHoverExit() {
			base.OnHoverExit();
			setBackGroundColor(_storeBackgroundColor);
		}

		private bool setBackGroundColor(Color color) {
			_currentBackgroundColor = color;
			return true;
		}

		public void Draw(SpriteBatch spriteBatch) {
			Rectangle rect = GetButtonRectangle();
			
			Vector2 pos = new Vector2(rect.X, rect.Y);
			
			spriteBatch.DrawSprite(TextureLoader.GetOrGenerateRectangleTexture(_size), pos, _currentBackgroundColor);
			spriteBatch.DrawBoundedText(_text, pos, _textColor, new Vector2Int(rect.Width, rect.Height), Game1.Font);

		}
	}
}
