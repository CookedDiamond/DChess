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
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace DChess {
	public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		private static ScalingUtil _gameScaling;
		public static SpriteBatch SpriteBatch { get; private set; }

		private readonly BoardManager _boardManager;
		private readonly InputHandler _inputHandler;

		private Scene _menuScene;
		private Scene _boardScene;
		private Scene _activeScene;

		public static SpriteFont Font { get; private set; }
		public SceneType ActiveSceneType { get; private set; }

		public Game1(BoardManager boardManager) {
			_boardManager = boardManager;
			_inputHandler = new InputHandler();

			_graphics = new GraphicsDeviceManager(this);
			_gameScaling = new ScalingUtil(boardManager.Board, this, _graphics);

			_boardScene = new SceneBoard(boardManager.BoardUI, boardManager.Board);
			_menuScene = new SceneMenu(this, boardManager.Board, boardManager.BoardNetworking);

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
			// Computer move
			if (_boardManager.GetComputerPlayerTeamType() != null
				&& _boardManager.GetComputerPlayerTeamType() == _boardManager.Board.GetTurnTeamType()) {
				_boardManager.MakeComputerMove();
			}

			_gameScaling.Update();

			_inputHandler.HandleInputs(Mouse.GetState(), _activeScene, _boardManager);

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
				case SceneType.None:
					throw new NotImplementedException();
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