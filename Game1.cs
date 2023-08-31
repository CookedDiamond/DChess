using DChess.Chess;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DChess {
	public class Game1 : Game {
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private Texture2D _squareTexture;
		private readonly int _squareSize = 32;
		private readonly float _scale = 4f;
		private Texture2D _pawnTexture;
		private Texture2D _bishopTexture;
		private Texture2D _knightTexture;
		private Texture2D _rookTexture;
		private Texture2D _queenTexture;
		private Texture2D _kingTexture;


		private Board _board;

		public Game1(Board board) {
			_board = board;

			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			
		}

		protected override void Initialize() {
			_board.Initialize();
			_graphics.PreferredBackBufferHeight = 1080;
			_graphics.PreferredBackBufferWidth = 1920;
			_graphics.ApplyChanges();

			base.Initialize();
		}

		protected override void LoadContent() {
			_spriteBatch = new SpriteBatch(GraphicsDevice);
			_squareTexture = Content.Load<Texture2D>("square");
			_pawnTexture = Content.Load<Texture2D>("pawn");
			_bishopTexture = Content.Load<Texture2D>("bishop");
			_knightTexture = Content.Load<Texture2D>("knight");
			_rookTexture = Content.Load<Texture2D>("rook");
			_queenTexture = Content.Load<Texture2D>("queen");
			_kingTexture = Content.Load<Texture2D>("king");
		}

		protected override void Update(GameTime gameTime) {
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.Clear(Color.DarkSeaGreen);
			_spriteBatch.Begin();
			// Draw Board
			drawBoard(_spriteBatch);

			// Draw Pieces
			drawPieces(_spriteBatch);
			_spriteBatch.End();

			base.Draw(gameTime);
		}

		private void drawBoard(SpriteBatch spriteBatch) {
			foreach (var square in _board.GetSquares()) {
				Color color = (square.team == TeamType.White) ? Color.White : Color.DarkGray;
				float factor = _squareSize * _scale;
				float x = square.position.x * factor;
				float y = (_board._size.y - 1) * factor - square.position.y * factor;
				spriteBatch.Draw(_squareTexture, new Vector2(x, y), null, color, 0, Vector2.Zero, _scale, SpriteEffects.None, 0);
			}
		}

		private void drawPieces(SpriteBatch spriteBatch) {

		}
	}
}