using DChess.Chess;
using DChess.UI;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace DChess {
	public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		private readonly ButtonManager _buttonManager;
		private readonly GameScaling _gameScaling;
		public static SpriteBatch SpriteBatch { get; private set; }

		// Mouse variables TODO: make Inputhandler Class
		private bool _lastMouseStateWasPressed = false;

		// Board variables
		private readonly Color _lightSquaresColor = new Color(242 / 255f, 225 / 255f, 195 / 255f);
		private readonly Color _darkSquaresColor = new Color(195 / 255f, 160 / 255f, 130 / 255f);
		private readonly Color _midColor;

		private readonly Board _board;

		private SpriteFont _font;

		public Game1(Board board, ButtonManager buttonManager) {
			_board = board;
			_buttonManager = buttonManager;

			_graphics = new GraphicsDeviceManager(this);
			_gameScaling = new GameScaling(_board, _graphics);

			Vector3 color1 = _lightSquaresColor.ToVector3();
			Vector3 color2 = _darkSquaresColor.ToVector3();
			_midColor = new Color((color1.X + color2.X) / 2, (color1.Y + color2.Y) / 2, (color1.Z + color2.Z) / 2);

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
			TextureLoader.LoadPrimitiveShapes(32, _graphics.GraphicsDevice);
			_font = Content.Load<SpriteFont>("Font");

			_gameScaling.Initialize();
		}

		protected override void Update(GameTime gameTime) {
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			_gameScaling.Update();

			var mouseState = Mouse.GetState();
			if (_lastMouseStateWasPressed && mouseState.LeftButton == ButtonState.Released) {
				_buttonManager.OnClick(new Vector2Int(mouseState.X, mouseState.Y));
			}

			if(mouseState.LeftButton == ButtonState.Pressed) {
				_lastMouseStateWasPressed = true;
			}
			else {
				_lastMouseStateWasPressed = false;
			}

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.Clear(Color.DarkSeaGreen);

			SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.LinearClamp, null);
			// Draw Board
			drawBoard(SpriteBatch);
			drawLegalMoves(SpriteBatch);
			drawPieces(SpriteBatch);

			SpriteBatch.End();

			base.Draw(gameTime);
		}

		private void drawBoard(SpriteBatch spriteBatch) {
			foreach (var square in _board.GetSquares()) {
				Vector2 windowPosition = _gameScaling.GetWindowPositionFromBoard(square.position);

				Color color = (square.team == TeamType.White) ? _lightSquaresColor : _darkSquaresColor;
				drawSprite(spriteBatch, TextureLoader.SquareTexture, windowPosition, color, 1);
			}
		}

		private void drawPieces(SpriteBatch spriteBatch) {
			foreach (var square in _board.GetSquares()) {
				Vector2 windowPosition = _gameScaling.GetWindowPositionFromBoard(square.position);

				var piece = square.piece;
				if (piece != null) {
					drawSprite(spriteBatch, square.piece.GetPieceTexture(), windowPosition, Color.White, _gameScaling.PieceFactor);
				}
			}
		}

		private void drawLegalMoves(SpriteBatch spriteBatch) {
			List<Vector2Int> moves = _board.legalMovesWithSelected;
			if (moves == null) {
				return;
			}
            foreach (var move in moves) {
				float offset = (_gameScaling.SquareSize / 4) * _gameScaling.Scale;
				Vector2 windowPosition = _gameScaling.GetWindowPositionFromBoard(move) + new Vector2(offset, offset);
				drawSprite(spriteBatch, TextureLoader.Circle, windowPosition, _midColor, _gameScaling.CircleFactor * 0.5f);
            }
        }

		private void drawSprite(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, float scaleFactor = 1) {
			spriteBatch.Draw(texture: texture, 
				position: new Vector2(position.X, position.Y), 
				sourceRectangle: null, 
				color: color, 
				rotation: 0, 
				origin: Vector2.Zero, 
				scale: _gameScaling.Scale * scaleFactor, 
				effects: SpriteEffects.None, 
				layerDepth: 0);
		}

	}
}