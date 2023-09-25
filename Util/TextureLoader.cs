using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DChess.Util {
	public class TextureLoader {
		// Dependencies.
		private static GraphicsDevice _graphics;
		private static ContentManager _content;

		// Chess textures.
		public static Texture2D[] PawnTexture { get; private set; }
		public static Texture2D[] BishopTexture { get; private set; }
		public static Texture2D[] KnightTexture { get; private set; }
		public static Texture2D[] RookTexture { get; private set; }
		public static Texture2D[] QueenTexture { get; private set; }
		public static Texture2D[] KingTexture { get; private set; }

		// Primitive shapes.
		public static Texture2D SquareTexture { get; private set; }
		public static Texture2D RectangleBorderTexture { get; private set; }
		public static Texture2D CircleTexture { get; private set; }
		private static readonly Dictionary<Vector2Int, Texture2D> _customRectangleTextures = new();

		public static void InitialiceTextures(GraphicsDevice graphics, ContentManager content, int baseSize) {
			_graphics = graphics;
			_content = content;
			loadStandardTextures();
			loadPrimitiveShapes(baseSize);
		}

		private static void loadStandardTextures() {
			PawnTexture = new Texture2D[2];
			BishopTexture = new Texture2D[2];
			KnightTexture = new Texture2D[2];
			RookTexture = new Texture2D[2];
			QueenTexture = new Texture2D[2];
			KingTexture = new Texture2D[2];

			PawnTexture[0] = _content.Load<Texture2D>("pawn_white");
			BishopTexture[0] = _content.Load<Texture2D>("bishop_white");
			KnightTexture[0] = _content.Load<Texture2D>("knight_white");
			RookTexture[0] = _content.Load<Texture2D>("rook_white");
			QueenTexture[0] = _content.Load<Texture2D>("queen_white");
			KingTexture[0] = _content.Load<Texture2D>("king_white");

			PawnTexture[1] = _content.Load<Texture2D>("pawn_black");
			BishopTexture[1] = _content.Load<Texture2D>("bishop_black");
			KnightTexture[1] = _content.Load<Texture2D>("knight_black");
			RookTexture[1] = _content.Load<Texture2D>("rook_black");
			QueenTexture[1] = _content.Load<Texture2D>("queen_black");
			KingTexture[1] = _content.Load<Texture2D>("king_black");

		}

		private static void loadPrimitiveShapes(int baseSize) {
			SquareTexture = createRectangle(new Vector2Int(baseSize, baseSize));
			CircleTexture = createCircle(baseSize);
			RectangleBorderTexture = createRectangleBorder(baseSize);
		}

		private static Texture2D createRectangle(Vector2Int baseSize) {
			Color[] squareData = new Color[baseSize.x * baseSize.y];
			for (int i = 0; i < squareData.Length; ++i) {
				squareData[i] = Color.White;
			}

			var texture = new Texture2D(_graphics, baseSize.x, baseSize.y);
			texture.SetData(squareData);
			return texture;
		}

		private static Texture2D createRectangleBorder(int baseSize) {
			Color[] rectangleBorderData = new Color[baseSize * baseSize];
			for (int i = 0; i < rectangleBorderData.Length; ++i) {
				int x = i % baseSize;
				int y = i / baseSize;

				if (x < baseSize / 8
					|| y < baseSize / 8
					|| x > (baseSize / 8) * 7
					|| y > (baseSize / 8) * 7) {
					rectangleBorderData[i] = Color.White;
				}
			}

			var texture = new Texture2D(_graphics, baseSize, baseSize);
			texture.SetData(rectangleBorderData);
			return texture;
		}

		private static Texture2D createCircle(int baseSize) {
			// Increase the resolution so the scaling looks better.
			baseSize *= 4;

			Color[] halfCircleData = new Color[baseSize * baseSize];
			for (int i = 0; i < halfCircleData.Length; ++i) {
				int center = baseSize / 2;
				int x = i % baseSize - center;
				int y = i / baseSize - center;

				if ((x * x) + (y * y) < center * center) {
					halfCircleData[i] = Color.White;
				}

			}

			var texture = new Texture2D(_graphics, baseSize, baseSize);
			texture.SetData(halfCircleData);
			return texture;
		}

		public static Texture2D GetOrGenerateRectangleTexture(Vector2Int size) {
			_customRectangleTextures.TryGetValue(size, out Texture2D texture);
			if (texture == null)
            {
				Debug.WriteLine($"Created new texture with size {size}");
				var createdTexture = createRectangle(size);
				_customRectangleTextures.Add(size, createdTexture);
				texture = createdTexture;
			}
			return texture;
        }
	}
}
