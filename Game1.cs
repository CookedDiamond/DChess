using DChess.Chess;
using DChess.UI;
using DChess.UI.Scenes;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace DChess {
	public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		private readonly ButtonManager _buttonManager;
		private static GameScaling _gameScaling;
		public static SpriteBatch SpriteBatch { get; private set; }

		// Mouse variables TODO: make Inputhandler Class
		private bool _lastMouseStateWasPressed = false;

		private readonly Board _board;

		public static SpriteFont Font {get; private set;}

		private Scene _menuScene;
		private Scene _boardScene;
		private Scene _currentScene;

		public Game1(Board board, ButtonManager buttonManager) {
			_board = board;
			_buttonManager = buttonManager;

			_graphics = new GraphicsDeviceManager(this);
			_gameScaling = new GameScaling(_board, _graphics);

			_boardScene = new SceneBoard(board);
			_menuScene = new SceneMenu(buttonManager);

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
			Font = Content.Load<SpriteFont>("Font");

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

			if (mouseState.LeftButton == ButtonState.Pressed) {
				_lastMouseStateWasPressed = true;
			}
			else {
				_lastMouseStateWasPressed = false;
			}

			base.Update(gameTime);
		}

		public void SwitchScene(GameScenes scene) {
			_ = scene switch {
				GameScenes.Board => _currentScene = _boardScene,
				GameScenes.Menu => _currentScene = _menuScene,
				_ => throw new NotImplementedException()
			};
		}

		protected override void Draw(GameTime gameTime) {
			if (_currentScene == null) {
				return;
			}

			GraphicsDevice.Clear(_currentScene.BackGroundColor);

			SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.LinearClamp, null);
			_currentScene.Draw(SpriteBatch);
			SpriteBatch.End();

			base.Draw(gameTime);
		}
	}

	public enum GameScenes {
		None,
		Board,
		Menu
	}
}