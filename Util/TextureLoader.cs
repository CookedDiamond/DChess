using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DChess.Util {
	public class TextureLoader {
		public static Texture2D[] PawnTexture { get; private set; }
		public static Texture2D[] BishopTexture { get; private set; }
		public static Texture2D[] KnightTexture { get; private set; }
		public static Texture2D[] RookTexture { get; private set; }
		public static Texture2D[] QueenTexture { get; private set; }
		public static Texture2D[] KingTexture { get; private set; }
		public static Texture2D SquareTexture { get; private set; }

		public static void LoadStandardTextures(ContentManager content) {
			PawnTexture = new Texture2D[2];
			BishopTexture = new Texture2D[2];
			KnightTexture = new Texture2D[2];
			RookTexture = new Texture2D[2];
			QueenTexture = new Texture2D[2];
			KingTexture = new Texture2D[2];

			SquareTexture = content.Load<Texture2D>("square");

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

	}
}
