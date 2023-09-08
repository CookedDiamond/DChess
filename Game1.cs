using DChess.Chess;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DChess {
	public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		public static SpriteBatch SpriteBatch { get; private set; }

		// Piece variables
		private float _pieceFactor = 0.5f;

		// Board variables
		private int _squareSize = 32;
		private readonly Color _lightSquaresColor = new Color(242 / 255f, 225 / 255f, 195 / 255f);
		private readonly Color _darkSquaresColor = new Color(195 / 255f, 160 / 255f, 130 / 255f);

		private float _scale = 1.5f;
		private readonly float _minScale = .5f;
		private readonly float _maxScale = 4f;
		private float _centerOffsetX = 0;
		private float _centerOffsetY = 0;

		private readonly Board _board;

		public Game1(Board board) {
			_board = board;

			_graphics = new GraphicsDeviceManager(this);
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
			_squareSize = TextureLoader.SquareTexture.Bounds.Width;
			_pieceFactor = (float)(_squareSize) / (float)(TextureLoader.PawnTexture[0].Bounds.Width);
		}

		protected override void Update(GameTime gameTime) {
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.Clear(Color.DarkSeaGreen);

			calculateBoardCenterOffsets();
			calculateScale();

			SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.LinearClamp, null);
			// Draw Board
			drawBoard(SpriteBatch);

			SpriteBatch.End();

			base.Draw(gameTime);
		}

		private void drawBoard(SpriteBatch spriteBatch) {
			foreach (var square in _board.GetSquares()) {
				
				float factor = _squareSize * _scale;
				float x = square.position.x * factor;
				float y = (_board._size.y - 1) * factor - square.position.y * factor;

				// Squares
				Color color = (square.team == TeamType.White) ? _lightSquaresColor : _darkSquaresColor;
				drawSprite(spriteBatch, TextureLoader.SquareTexture, new Vector2(x, y), color, 0);

				// Pieces
				var piece = square.piece;
				if (piece != null) {
					drawSprite(spriteBatch, square.piece.GetPieceTexture(), new Vector2(x, y), Color.White, 0, _pieceFactor);
				}
				
			}
		}

		private void drawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, float layerDepth, float scaleFactor = 1) {
			spriteBatch.Draw(texture: texture, 
				position: new Vector2(position.X + _centerOffsetX, position.Y + _centerOffsetY), 
				sourceRectangle: null, 
				color: color, 
				rotation: 0, 
				origin: Vector2.Zero, 
				scale: _scale * scaleFactor, 
				effects: SpriteEffects.None, 
				layerDepth: layerDepth);
		}

		private void calculateBoardCenterOffsets() {
			float boardWidth = _board._size.x * _squareSize * _scale;
			float boardheight = _board._size.y * _squareSize * _scale;

			float windowWidth = _graphics.PreferredBackBufferWidth;
			float windowheight = _graphics.PreferredBackBufferHeight;

			_centerOffsetX = (windowWidth - boardWidth) / 2;
			_centerOffsetY = (windowheight - boardheight) / 2;
		}

		private void calculateScale() {
			float boardWidthNoScale = _board._size.x * _squareSize;

			float windowheight = _graphics.PreferredBackBufferHeight;

			float scale = (windowheight * 0.9f) / boardWidthNoScale;
			_scale = MathHelper.Clamp(scale, _minScale, _maxScale);
		}
	}
}