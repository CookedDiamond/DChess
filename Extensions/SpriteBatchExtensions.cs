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

		public static void DrawRectangle(this SpriteBatch spriteBatch, Rectangle rectangle, Color color) {
			spriteBatch.Draw(TextureLoader.SquareTexture, rectangle, color);
		}

		public static void DrawRectangleOutline(this SpriteBatch spriteBatch, Rectangle rectangle, Color color, int thickness) {
			spriteBatch.DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
			spriteBatch.DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
			spriteBatch.DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
			spriteBatch.DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
		}

		/// <summary>Draws one line of text with the given pixel height, top left at position.</summary>
		public static void DrawTextLine(this SpriteBatch spriteBatch, string text, Vector2 position, float height, Color color) {
			SpriteFont font = Game1.Font;
			float scale = height / font.LineSpacing;
			spriteBatch.DrawString(font, text, new Vector2((int)position.X, (int)position.Y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
		}

		/// <summary>Draws text centered in the rectangle, shortened with "..." if it is too wide.</summary>
		public static void DrawTextCentered(this SpriteBatch spriteBatch, string text, Rectangle bounds, float height, Color color) {
			text = FitText(text, height, bounds.Width);
			Vector2 size = MeasureText(text, height);
			Vector2 position = new(bounds.X + (bounds.Width - size.X) / 2, bounds.Y + (bounds.Height - size.Y) / 2);
			spriteBatch.DrawTextLine(text, position, height, color);
		}

		public static Vector2 MeasureText(string text, float height) {
			SpriteFont font = Game1.Font;
			return font.MeasureString(text) * (height / font.LineSpacing);
		}

		/// <summary>Shortens the text with "..." until it fits into maxWidth.</summary>
		public static string FitText(string text, float height, float maxWidth) {
			if (text == null) return "";
			if (MeasureText(text, height).X <= maxWidth) return text;
			for (int length = text.Length - 1; length > 0; length--) {
				string shortened = text.Substring(0, length).TrimEnd() + "...";
				if (MeasureText(shortened, height).X <= maxWidth) return shortened;
			}
			return "";
		}
	}
}
