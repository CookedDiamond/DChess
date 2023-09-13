using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DChess.Util {
	public class TextureLoader {
		public static Texture2D[] PawnTexture { get; private set; }
		public static Texture2D[] BishopTexture { get; private set; }
		public static Texture2D[] KnightTexture { get; private set; }
		public static Texture2D[] RookTexture { get; private set; }
		public static Texture2D[] QueenTexture { get; private set; }
		public static Texture2D[] KingTexture { get; private set; }

		// Primitive shapes.
		public static Texture2D SquareTexture { get; private set; }
		public static Texture2D RectangleBorderTexture { get; private set; }
		public static Texture2D Circle { get; private set; }

		public static void LoadStandardTextures(ContentManager content) {
			PawnTexture = new Texture2D[2];
			BishopTexture = new Texture2D[2];
			KnightTexture = new Texture2D[2];
			RookTexture = new Texture2D[2];
			QueenTexture = new Texture2D[2];
			KingTexture = new Texture2D[2];

			PawnTexture[0] = content.Load<Texture2D>("pawn_white");
			BishopTexture[0] = content.Load<Texture2D>("bishop_white");
			KnightTexture[0] = content.Load<Texture2D>("knight_white");
			RookTexture[0] = content.Load<Texture2D>("rook_white");
			QueenTexture[0] = content.Load<Texture2D>("queen_white");
			KingTexture[0] = content.Load<Texture2D>("king_white");

			PawnTexture[1] = content.Load<Texture2D>("pawn_black");
			BishopTexture[1] = content.Load<Texture2D>("bishop_black");
			KnightTexture[1] = content.Load<Texture2D>("knight_black");
			RookTexture[1] = content.Load<Texture2D>("rook_black");
			QueenTexture[1] = content.Load<Texture2D>("queen_black");
			KingTexture[1] = content.Load<Texture2D>("king_black");

		}

		public static void LoadPrimitiveShapes(int baseSize, GraphicsDevice graphics) {
			createSquare(baseSize, graphics);
			createCircle(baseSize, graphics);
			createRectangleBorder(baseSize, graphics);
		}

		private static void createSquare(int baseSize, GraphicsDevice graphics) {
			Color[] squareData = new Color[baseSize * baseSize];
			for (int i = 0; i < squareData.Length; ++i) {
				squareData[i] = Color.White;
			}

			SquareTexture = new Texture2D(graphics, baseSize, baseSize);
			SquareTexture.SetData(squareData);
		}

		private static void createRectangleBorder(int baseSize, GraphicsDevice graphics) {
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

			RectangleBorderTexture = new Texture2D(graphics, baseSize, baseSize);
			RectangleBorderTexture.SetData(rectangleBorderData);
		}

		private static void createCircle(int baseSize, GraphicsDevice graphics) {
			// Increase the resolution so the scaling looks better.
			baseSize *= 4;

			Color[] halfCircleData = new Color[baseSize * baseSize];
			for (int i = 0; i < halfCircleData.Length; ++i) {
				int center = baseSize / 2;
				int x = i % baseSize - center;
				int y = i / baseSize - center;

				if ((x * x) + (y * y) < center * center) {
					halfCircleData[i] = new Color(Color.White, 0.1f);
				}

			}

			Circle = new Texture2D(graphics, baseSize, baseSize);
			Circle.SetData(halfCircleData);
		}
	}
}
