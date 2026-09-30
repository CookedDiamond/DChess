using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.UI.Scenes;
using DChess.Util;
using DChess.Persistence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Threading;

namespace DChess {
	public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		private static ScalingUtil _gameScaling;
		public static SpriteBatch SpriteBatch { get; private set; }

		private readonly int _smokeTestFrames;
		private int _framesDrawn;
		private readonly string _smokeTestScene;
		private readonly InputHandler _inputHandler;

		private Scene _menuScene;
		private Scene _activeScene;
		private bool _isResizing;
		private long _sessionGeneration;
		public AutosaveStore Autosave { get; }
		public string SaveStatus => Autosave?.LastError;
		public bool CanResume => Autosave?.Exists == true;

		public static SpriteFont Font { get; private set; }

		/// <summary>Size of the drawing area (window content) in pixels.</summary>
		public static Point ScreenSize { get; private set; } = new(1280, 720);

		public SceneType ActiveSceneType { get; private set; }

		/// <param name="smokeTestFrames">If greater than 0, the game exits after drawing this many frames ("--smoke-test").</param>
		public Game1(int smokeTestFrames = 0, string smokeTestScene = null) {
			_smokeTestFrames = smokeTestFrames;
			_smokeTestScene = smokeTestScene;
			Autosave = smokeTestFrames > 0 ? null : new AutosaveStore();
			_inputHandler = new InputHandler();

			_graphics = new GraphicsDeviceManager(this);
			_gameScaling = new ScalingUtil(this, _graphics);

			Content.RootDirectory = "Content";
			IsMouseVisible = true;
			Exiting += (sender, args) => stopSession();
		}

		protected override void Initialize() {
			// Open the window with 75% of the screen size, centered.
			DisplayMode display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
			int width = Math.Min(display.Width, Math.Max(1024, (int)(display.Width * 0.75f)));
			int height = Math.Min(display.Height, Math.Max(640, (int)(display.Height * 0.75f)));
			_graphics.PreferredBackBufferWidth = width;
			_graphics.PreferredBackBufferHeight = height;
			_graphics.ApplyChanges();
			Window.Position = new Point((display.Width - width) / 2, (display.Height - height) / 2);

			Window.Title = "DChess - Bot Arena";
			Window.AllowUserResizing = true;
			Window.AllowAltF4 = true;
			Window.ClientSizeChanged += onClientSizeChanged;
			updateScreenSize();

			base.Initialize();
		}

		private void onClientSizeChanged(object sender, EventArgs e) {
			Rectangle bounds = Window.ClientBounds;
			if (_isResizing || bounds.Width <= 0 || bounds.Height <= 0) return;

			_isResizing = true;
			_graphics.PreferredBackBufferWidth = bounds.Width;
			_graphics.PreferredBackBufferHeight = bounds.Height;
			_graphics.ApplyChanges();
			_isResizing = false;
			updateScreenSize();
		}

		private void updateScreenSize() {
			PresentationParameters parameters = GraphicsDevice.PresentationParameters;
			ScreenSize = new Point(parameters.BackBufferWidth, parameters.BackBufferHeight);
		}

		protected override void LoadContent() {
			SpriteBatch = new SpriteBatch(GraphicsDevice);

			TextureLoader.InitialiceTextures(_graphics.GraphicsDevice, Content, 32);
			Font = Content.Load<SpriteFont>("Font");
			_gameScaling.Initialize();

			_menuScene = new SceneMenu(this);
			OpenMenu();
			if (_smokeTestFrames > 0 && _smokeTestScene == "sandbox") {
				StartSandbox(BotRegistry.Find("MinMaxBot"), 10000);
				_activeScene.KeyPressed(Keys.A);
			}
			if (_smokeTestFrames > 0 && _smokeTestScene == "arena") {
				StartMatch(new MatchSettings { Player1 = BotRegistry.Find("MinMaxBot"), Player2 = BotRegistry.Human,
					Games = 1, TimeLimitMilliseconds = 10000 });
			}
		}

		protected override void Update(GameTime gameTime) {
			updateScreenSize();
			_gameScaling.Update();

			if (IsActive) {
				_inputHandler.HandleInputs(Mouse.GetState(), Keyboard.GetState(), _activeScene, gameTime);
			}
			_activeScene.Update(gameTime);

			base.Update(gameTime);
		}

		public void OpenMenu() {
			stopSession();
			switchScene(_menuScene, SceneType.Menu);
		}

		/// <summary>Starts a match between two players and shows it.</summary>
		public void StartMatch(MatchSettings settings) {
			stopSession();
			long generation = Interlocked.Increment(ref _sessionGeneration);
			switchScene(new SceneArena(this, settings, null, state => saveMatch(state, generation)), SceneType.Arena);
		}

		/// <summary>Opens a board to play freely, the bot moves when A is pressed.</summary>
		public void StartSandbox(BotInfo helperBot, int botTimeLimitMilliseconds) {
			stopSession();
			Interlocked.Increment(ref _sessionGeneration);
			switchScene(new SceneBoard(this, helperBot, botTimeLimitMilliseconds), SceneType.Board);
			SaveCurrentSession();
		}

		public void ResumeGame() {
			var saved = Autosave?.Load();
			if (saved == null) return;
			stopSession();
			long generation = Interlocked.Increment(ref _sessionGeneration);
			if (saved.Mode == "sandbox") {
				var bot = saved.HelperBot == null ? null : BotRegistry.Find(saved.HelperBot);
				switchScene(new SceneBoard(this, bot, saved.BotTimeLimitMilliseconds, saved.Board.Restore(), saved.BotMovePending), SceneType.Board);
			} else {
				switchScene(new SceneArena(this, saved.Match.CreateSettings(), saved.Match,
					state => saveMatch(state, generation)), SceneType.Arena);
			}
		}

		private void saveMatch(MatchState state, long generation) {
			Autosave?.Save(new SessionState { Mode = "match", Match = state },
				() => generation == Volatile.Read(ref _sessionGeneration));
		}

		public void SaveCurrentSession() {
			if (Autosave == null) return;
			if (_activeScene is SceneBoard board) Autosave.Save(board.CaptureState());
			if (_activeScene is SceneArena arena) saveMatch(arena.CaptureState(), Volatile.Read(ref _sessionGeneration));
		}

		private void stopSession() {
			SaveCurrentSession();
			(_activeScene as SceneArena)?.Stop();
			(_activeScene as SceneBoard)?.Stop();
			Interlocked.Increment(ref _sessionGeneration);
		}

		private void switchScene(Scene scene, SceneType sceneType) {
			_activeScene = scene;
			ActiveSceneType = sceneType;
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
			if (_smokeTestFrames > 0 && ++_framesDrawn >= _smokeTestFrames) Exit();
		}
	}

	public enum SceneType {
		None,
		Board,
		Menu,
		Arena
	}
}
