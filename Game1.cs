using DChess.Chess.Playground;
using DChess.UI;
using DChess.UI.Scenes;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DChess
{
    public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		private static ScalingUtil _gameScaling;
		public static SpriteBatch SpriteBatch { get; private set; }

		// Mouse variables TODO: make Inputhandler Class
		private bool _lastMouseStateWasPressed = false;
		private const int keyInputDelay = 30;
		private int lastKeyInput = 0;

		private readonly BoardManager _boardManager;

		public static SpriteFont Font { get; private set; }

		private Scene _menuScene;
		private Scene _boardScene;
		private Scene _activeScene;
		public SceneType ActiveSceneType { get; private set; }

		public Game1(BoardManager boardManager) {
			_boardManager = boardManager;

			_graphics = new GraphicsDeviceManager(this);
			_gameScaling = new ScalingUtil(boardManager.Board, this, _graphics);

			_boardScene = new SceneBoard(boardManager.BoardUI, boardManager.Board);
			_menuScene = new SceneMenu(this, boardManager.Board);

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

			TextureLoader.InitialiceTextures(_graphics.GraphicsDevice, Content, 32);
			Font = Content.Load<SpriteFont>("Font");
			_gameScaling.Initialize();
		}

		protected override void Update(GameTime gameTime) {
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			_gameScaling.Update();

			// Mouse Inputs.
			var mouseState = Mouse.GetState();
			var mousePos = new Vector2Int(mouseState.X, mouseState.Y);
			_activeScene.MouseHover(mousePos);
			if (_lastMouseStateWasPressed && mouseState.LeftButton == ButtonState.Released) {
				_activeScene.MouseClick(mousePos);
			}

			if (mouseState.LeftButton == ButtonState.Pressed) {
				_lastMouseStateWasPressed = true;
			}
			else {
				_lastMouseStateWasPressed = false;
			}

			// Key Inputs.
			lastKeyInput++;
			KeyboardState keyState = Keyboard.GetState();
			if (keyState.IsKeyDown(Keys.A) && keyInputDelay <= lastKeyInput) {
				_boardManager.Board.MakeComputerMove();
				lastKeyInput = 0;
			}
			if (keyState.IsKeyDown(Keys.S) && keyInputDelay <= lastKeyInput) {
				Debug.WriteLine($"Current Eval: {_boardManager.Board.GetEvaluaton()}");
				lastKeyInput = 0;
			}

			base.Update(gameTime);
		}

		public void SwitchScene(SceneType scene) {
			switch (scene) {
				case SceneType.Board:
					_activeScene = _boardScene;
					break;
				case SceneType.Menu:
					_activeScene = _menuScene;
					break;
				default:
					throw new NotImplementedException();
			}
			ActiveSceneType = scene;
		}

		protected override void Draw(GameTime gameTime) {
			if (_activeScene == null) {
				return;
			}

			GraphicsDevice.Clear(_activeScene.BackGroundColor);

			SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.LinearClamp, null);
			_activeScene.Draw(SpriteBatch);
			SpriteBatch.End();

			base.Draw(gameTime);
		}
	}

	public enum SceneType {
		None,
		Board,
		Menu
	}
}