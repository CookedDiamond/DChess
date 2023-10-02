using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Extensions {
	public static class SpriteBatchExtensions {

		private const int fontPixelOffsetY = 0;

		public static void DrawSprite(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, float scaleFactor = 1) {
			spriteBatch.Draw(texture: texture,
				position: new Vector2(position.X, position.Y),
				sourceRectangle: null,
				color: color,
				rotation: 0,
				origin: Vector2.Zero,
				scale: ScalingUtil.Instance.Scale * scaleFactor,
				effects: SpriteEffects.None,
				layerDepth: 0);
		}

		public static void DrawBoundedText(this SpriteBatch spriteBatch, string text, Vector2 position, 
			Color color, Vector2Int bound, SpriteFont font, bool isCentered = true) {

			Vector2 textSize = font.MeasureString(text);

			float maxSize = Math.Min(bound.x / textSize.X, bound.y / textSize.Y);
			float scale = maxSize;

			//textSize *= scale;
			//float yOffset = (bound.y - textSize.Y) / 2;
			//position += Vector2.UnitY * yOffset;

			position -= Vector2.UnitY * fontPixelOffsetY * ScalingUtil.Instance.Scale;

			spriteBatch.DrawString(font, text, position, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
		}
	}
}
