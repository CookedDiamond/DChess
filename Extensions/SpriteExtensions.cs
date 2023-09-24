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
	public static class SpriteExtensions {
		public static void DrawSprite(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, float scaleFactor = 1) {
			spriteBatch.Draw(texture: texture,
				position: new Vector2(position.X, position.Y),
				sourceRectangle: null,
				color: color,
				rotation: 0,
				origin: Vector2.Zero,
				scale: GameScaling.Instance.Scale * scaleFactor,
				effects: SpriteEffects.None,
				layerDepth: 0);
		}
	}
}
