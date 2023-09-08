using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DChess.Util {
	public class TextureLoader {
		public static Texture2D PawnTexture { get; private set; }
		public static Texture2D BishopTexture { get; private set; }
		public static Texture2D KnightTexture { get; private set; }
		public static Texture2D RookTexture { get; private set; }
		public static Texture2D QueenTexture { get; private set; }
		public static Texture2D KingTexture { get; private set; }
		public static Texture2D SquareTexture { get; private set; }

		public static void LoadStandardTextures(ContentManager content) {
			SquareTexture = content.Load<Texture2D>("square");
			PawnTexture = content.Load<Texture2D>("pawn_high");
			BishopTexture = content.Load<Texture2D>("bishop_high");
			KnightTexture = content.Load<Texture2D>("knight_high");
			RookTexture = content.Load<Texture2D>("rook_high");
			QueenTexture = content.Load<Texture2D>("queen_high");
			KingTexture = content.Load<Texture2D>("king_high");
		}

	}
}
