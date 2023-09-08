using DChess.Chess;
using DChess.UI;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DChess {
	public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		private readonly ButtonManager _buttonManager;
		private readonly GameScaling _gameScaling;
		public static SpriteBatch SpriteBatch { get; private set; }

		// Piece variables

		// Board variables
		private readonly Color _lightSquaresColor = new Color(242 / 255f, 225 / 255f, 195 / 255f);
		private readonly Color _darkSquaresColor = new Color(195 / 255f, 160 / 255f, 130 / 255f);

		private readonly Board _board;

		public Game1(Board board, ButtonManager buttonManager) {
			_board = board;
			_buttonManager = buttonManager;

			_graphics = new GraphicsDeviceManager(this);
			_gameScaling = new GameScaling(_board, _graphics);

			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			
		}

		protected override void Initialize() {
			_graphics.PreferredBackBufferHeight = 1080;
			_graphics.PreferredBackBufferWidth = 1920;
			_graphics.ApplyChanges();

			Window.AllowUserResizing = true;
			Window.AllowAltF4 = true;

			base.Initialize();
		}

		protected override void LoadContent() {
			SpriteBatch = new SpriteBatch(GraphicsDevice);

			TextureLoader.LoadStandardTextures(Content);

			_gameScaling.Initialize();
		}

		protected override void Update(GameTime gameTime) {
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			_gameScaling.Update();

			var mouseState = Mouse.GetState();
			if (mouseState.LeftButton == ButtonState.Pressed) {
				_buttonManager.OnClick(new Vector2Int(mouseState.X, mouseState.Y));
			}

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.Clear(Color.DarkSeaGreen);

			SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.LinearClamp, null);
			// Draw Board
			drawBoard(SpriteBatch);

			SpriteBatch.End();

			base.Draw(gameTime);
		}

		private void drawBoard(SpriteBatch spriteBatch) {
			foreach (var square in _board.GetSquares()) {
				
				float factor = _gameScaling.GetFactor();
				float x = square.position.x * factor;
				float y = (_board._size.y - 1) * factor - square.position.y * factor;

				// Squares
				Color color = (square.team == TeamType.White) ? _lightSquaresColor : _darkSquaresColor;
				drawSprite(spriteBatch, TextureLoader.SquareTexture, new Vector2(x, y), color, 0);

				// Pieces
				var piece = square.piece;
				if (piece != null) {
					drawSprite(spriteBatch, square.piece.GetPieceTexture(), new Vector2(x, y), Color.White, 0, _gameScaling._pieceFactor);
				}
				
			}
		}

		private void drawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, float layerDepth, float scaleFactor = 1) {
			spriteBatch.Draw(texture: texture, 
				position: new Vector2(position.X + _gameScaling.CenterOffsetX, position.Y + _gameScaling.CenterOffsetY), 
				sourceRectangle: null, 
				color: color, 
				rotation: 0, 
				origin: Vector2.Zero, 
				scale: _gameScaling.Scale * scaleFactor, 
				effects: SpriteEffects.None, 
				layerDepth: layerDepth);
		}

	}
}