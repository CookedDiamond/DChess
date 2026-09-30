using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.UI.Scenes;
using DChess.Util;
using DChess.Persistence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Threading;

namespace DChess {
	public class Game1 : Game {
		private readonly GraphicsDeviceManager _graphics;
		private static ScalingUtil _gameScaling;
		public static SpriteBatch SpriteBatch { get; private set; }

		private readonly int _smokeTestFrames;
		private int _framesDrawn;
		private readonly string _smokeTestScene;
        private readonly string _smokeCapturePath;
        private readonly Point? _smokeWindowSize;
		private readonly InputHandler _inputHandler;

		private Scene _menuScene;
		private Scene _activeScene;
		private bool _isResizing;
		private long _sessionGeneration;
		public AutosaveStore Autosave { get; }
		public string SaveStatus => Autosave?.LastError;
		public bool CanResume => Autosave?.Exists == true;
        public TournamentStore TournamentHistory { get; }
        public TournamentRunner Tournament { get; private set; }

		public static SpriteFont Font { get; private set; }

		/// <summary>Size of the drawing area (window content) in pixels.</summary>
		public static Point ScreenSize { get; private set; } = new(1280, 720);

		public SceneType ActiveSceneType { get; private set; }

		/// <param name="smokeTestFrames">If greater than 0, the game exits after drawing this many frames ("--smoke-test").</param>
        public Game1(int smokeTestFrames = 0, string smokeTestScene = null, string smokeCapturePath = null, string smokeArchiveDirectory = null, Point? smokeWindowSize = null) {
			_smokeTestFrames = smokeTestFrames;
			_smokeTestScene = smokeTestScene;
            _smokeCapturePath = smokeCapturePath;
            _smokeWindowSize = smokeWindowSize;
			Autosave = smokeTestFrames > 0 ? null : new AutosaveStore();
            TournamentHistory = new TournamentStore(smokeTestFrames > 0 ? smokeArchiveDirectory ?? System.IO.Path.Combine(AppContext.BaseDirectory, "tournament-smoke", Guid.NewGuid().ToString("N")) : null);
			_inputHandler = new InputHandler();

			_graphics = new GraphicsDeviceManager(this);
			_gameScaling = new ScalingUtil(this, _graphics);

			Content.RootDirectory = "Content";
			IsMouseVisible = true;
            Exiting += (sender, args) => { stopSession(); Tournament?.Pause(); Tournament?.Flush(); };
		}

		protected override void Initialize() {
			// Open the window with 75% of the screen size, centered.
			DisplayMode display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
			int width = Math.Min(display.Width, Math.Max(1024, (int)(display.Width * 0.75f)));
			int height = Math.Min(display.Height, Math.Max(640, (int)(display.Height * 0.75f)));
            if (_smokeTestFrames > 0 && _smokeWindowSize.HasValue) {
                width = _smokeWindowSize.Value.X; height = _smokeWindowSize.Value.Y;
            }
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
            if (_smokeTestFrames > 0 && _smokeTestScene == "tournaments") OpenTournaments();
            if (_smokeTestFrames > 0 && _smokeTestScene == "tournament-live") StartTournament(new TournamentSettings {
                Bots = new() { "GreedyBot", "RandomBot", "RandomBot", "GreedyBot" }, GamesPerPairing = 2, TimeLimitMilliseconds = 100
            });
            if (_smokeTestFrames > 0 && _smokeTestScene == "tournament-history") OpenTournaments(history: true);
            if (_smokeTestFrames > 0 && _smokeTestScene is "tournament-results" or "tournament-replay") {
                var summary = TournamentHistory.ListSummaries().FirstOrDefault() ?? throw new InvalidOperationException("Provide a smoke archive directory containing a tournament.");
                var archive = TournamentHistory.Load(summary.Id);
                if (_smokeTestScene == "tournament-results") OpenTournaments(archive);
                else OpenTournamentPairing(archive, archive.Pairings[0]);
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
        public void OpenTournaments(TournamentState selected = null, bool history = false, int? pairingNumber = null) {
            stopSession();
            switchScene(new SceneTournaments(this, selected, history, pairingNumber), SceneType.Tournament);
        }
        public void StartTournament(TournamentSettings settings, TournamentState resume = null) {
            if (Tournament?.View().Status == TournamentStatus.Running) throw new InvalidOperationException("Pause the active tournament before starting another.");
            var runner = new TournamentRunner(settings, TournamentHistory, resume);
            stopSession(); Tournament = runner; runner.Start();
            OpenTournaments(runner.View());
        }
        public void OpenTournamentPairing(TournamentState state, TournamentPairing pairing, int gameIndex = 0) {
            var live = Tournament?.View();
            Match match = (live?.Id == state.Id ? Tournament.MatchForPairing(pairing.Number) : null) ?? pairing.Match?.CreateReplay();
            if (match == null) return;
            stopSession();
            switchScene(new SceneArena(this, match, () => OpenTournaments(Tournament?.View().Id == state.Id ? Tournament.View() : state, pairingNumber: pairing.Number), gameIndex), SceneType.Arena);
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
            if (_activeScene is SceneArena arena && arena.OwnsMatch) saveMatch(arena.CaptureState(), Volatile.Read(ref _sessionGeneration));
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
            if (_smokeTestFrames > 0 && ++_framesDrawn >= _smokeTestFrames) {
                if (_smokeCapturePath != null) {
                    var pixels = new Color[ScreenSize.X * ScreenSize.Y];
                    GraphicsDevice.GetBackBufferData(pixels);
                    using var texture = new Texture2D(GraphicsDevice, ScreenSize.X, ScreenSize.Y);
                    texture.SetData(pixels);
                    using var file = System.IO.File.Create(_smokeCapturePath);
                    texture.SaveAsPng(file, ScreenSize.X, ScreenSize.Y);
                }
                Exit();
            }
		}
	}

	public enum SceneType {
		None,
		Board,
		Menu,
        Arena,
        Tournament
	}
}
