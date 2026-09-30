using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.Chess.Playground;
using DChess.Extensions;
using DChess.Util;
using DChess.Persistence;
using DChess.UI.Analysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.UI.Scenes {
	/// <summary>
	/// Runs a match and shows it: follow the games live or replay any game move by move.
	/// Left: match score and list of games. Center: board and playback controls. Right: moves of the selected game.
	/// </summary>
	public class SceneArena : Scene {
		private static readonly float[] SPEEDS = { 0.5f, 1, 2, 4, 8, 16, float.PositiveInfinity };
		private const double END_OF_GAME_PAUSE_SECONDS = 2.5;

		private readonly Game1 _game;
		private readonly Match _match;
		private readonly SnapshotBoardRenderer _renderer = new();
        private readonly StockfishEvaluation _evaluation = new();
        private PositionSnapshot _analysisSnapshot;
        private AnalysisPosition _analysisPosition;

		// What is shown.
		private int _gameIndex;
		private int _ply;
		private bool _autoPlay = true;
		private int _speedIndex = 2;
		private double _playTimer;
		private double _endOfGameTimer;
		private bool _manualFlip;
		private int _gamesScroll;
		private int _scrolledToGameIndex = -1;
		private double _time;

		// Human move input.
		private BotBoard _lastPendingBoard;
		private Vector2Int? _selectedSquare;
		private List<Move> _selectedMoves = new();

		// Layout (recalculated every frame).
		private float _unit;
		private Rectangle _leftPanel, _rightPanel, _boardArea, _topLabel, _bottomLabel, _backButton;
		private Rectangle _gamesListArea, _movesListArea;
		private readonly Rectangle[] _controlRects = new Rectangle[8];
		private Rectangle _speedLabel;
		private readonly List<(Rectangle rect, int gameIndex)> _gameRows = new();
		private readonly List<(Rectangle rect, int ply)> _moveCells = new();

		public SceneArena(Game1 game, MatchSettings settings, MatchState resume = null, Action<MatchState> autosave = null) {
			_game = game;
			BackGroundColor = Theme.Background;
			_match = new Match(settings, resume, autosave);
			if (resume?.Games.Count > 0) {
				_gameIndex = resume.Games.Count - 1;
				_ply = resume.Games[^1].Positions.Count - 1;
			}
			_match.Start();

			addButton(() => _controlRects[0], () => "|<", () => goToPly(0));
			addButton(() => _controlRects[1], () => "<", () => step(-1));
			var play = addButton(() => _controlRects[2], () => _autoPlay ? "Pause" : "Play", togglePlay);
			play.IsHighlighted = () => _autoPlay;
			addButton(() => _controlRects[3], () => ">", () => step(1));
			addButton(() => _controlRects[4], () => ">|", goToEnd);
			addButton(() => _controlRects[5], () => "-", () => changeSpeed(-1));
			addButton(() => _controlRects[6], () => "+", () => changeSpeed(1));
			addButton(() => _controlRects[7], () => "Flip", () => _manualFlip = !_manualFlip);
			addButton(() => _backButton, () => "Back to Menu", () => _game.OpenMenu());
		}

		/// <summary>Stops the match (bots finish their current move in the background).</summary>
		public void Stop() {
			_match.Cancel();
            _evaluation.Dispose();
		}

		public MatchState CaptureState() => _match.CaptureState();

		private ButtonRect addButton(Func<Rectangle> bounds, Func<string> text, Button.OnButtonClicked onClick) {
			var button = new ButtonRect(bounds, text);
			button.Initialize(buttonManager, onClick);
			content.Add(button);
			return button;
		}

		// ------------------------------------------------------------------------------------
		// Navigation
		// ------------------------------------------------------------------------------------

		private GameRecord currentGame(List<GameRecord> games) {
			if (games.Count == 0) return null;
			_gameIndex = Math.Clamp(_gameIndex, 0, games.Count - 1);
			return games[_gameIndex];
		}

		private int lastPly() {
			GameRecord game = currentGame(_match.Games);
			return game == null ? 0 : game.PositionCount - 1;
		}

		private void goToPly(int ply) {
			_autoPlay = false;
			_ply = Math.Clamp(ply, 0, lastPly());
			clearSelection();
		}

		private void step(int direction) {
			goToPly(_ply + direction);
		}

		private void goToEnd() {
			GameRecord game = currentGame(_match.Games);
			if (game == null) return;
			goToPly(game.PositionCount - 1);
			// Keep following an ongoing game.
			_autoPlay = !game.IsFinished;
		}

		private void togglePlay() {
			GameRecord game = currentGame(_match.Games);
			if (!_autoPlay && game != null && game.IsFinished && _ply >= game.PositionCount - 1) {
				_ply = 0;
			}
			_autoPlay = !_autoPlay;
			_playTimer = 0;
			_endOfGameTimer = 0;
		}

		private void selectGame(int index) {
			var games = _match.Games;
			if (games.Count == 0) return;
			_gameIndex = Math.Clamp(index, 0, games.Count - 1);
			_ply = 0;
			_autoPlay = true;
			_playTimer = 0;
			_endOfGameTimer = 0;
			clearSelection();
		}

		private void changeSpeed(int direction) {
			_speedIndex = Math.Clamp(_speedIndex + direction, 0, SPEEDS.Length - 1);
		}

		private void clearSelection() {
			_selectedSquare = null;
			_selectedMoves = new List<Move>();
		}

		private static string speedText(float speed) {
			if (float.IsInfinity(speed)) return "Instant";
			return $"{speed:0.#} moves/s";
		}

		// ------------------------------------------------------------------------------------
		// Input
		// ------------------------------------------------------------------------------------

		public override void KeyPressed(Keys key) {
			switch (key) {
				case Keys.Space:
					togglePlay();
					break;
				case Keys.Left:
					step(-1);
					break;
				case Keys.Right:
					step(1);
					break;
				case Keys.Home:
					goToPly(0);
					break;
				case Keys.End:
					goToEnd();
					break;
				case Keys.Up:
				case Keys.PageUp:
					selectGame(_gameIndex - 1);
					break;
				case Keys.Down:
				case Keys.PageDown:
					selectGame(_gameIndex + 1);
					break;
				case Keys.OemPlus:
				case Keys.Add:
					changeSpeed(1);
					break;
				case Keys.OemMinus:
				case Keys.Subtract:
					changeSpeed(-1);
					break;
				case Keys.F:
					_manualFlip = !_manualFlip;
					break;
				case Keys.Escape:
				case Keys.Back:
					_game.OpenMenu();
					break;
			}
		}

		public override void MouseScroll(Vector2Int mousePos, int delta) {
			int steps = Math.Sign(delta);
			if (_gamesListArea.Contains(mousePos.x, mousePos.y)) {
				_gamesScroll -= steps;
			}
			else {
				// Scrolling anywhere else steps through the game.
				step(-steps);
			}
		}

		public override void MouseClick(Vector2Int mousePos) {
			base.MouseClick(mousePos);
			Point point = new(mousePos.x, mousePos.y);

			foreach (var (rect, gameIndex) in _gameRows) {
				if (rect.Contains(point)) {
					selectGame(gameIndex);
					return;
				}
			}
			foreach (var (rect, ply) in _moveCells) {
				if (rect.Contains(point)) {
					goToPly(ply);
					return;
				}
			}
			handleBoardClick(mousePos);
		}

		private HumanPlayer interactiveHuman(List<GameRecord> games) {
			HumanPlayer human = _match.GetWaitingHuman();
			if (human == null || games.Count == 0) return null;
			GameRecord latest = games[^1];
			bool viewingLive = _gameIndex == games.Count - 1 && _ply == latest.PositionCount - 1;
			return viewingLive ? human : null;
		}

		private void handleBoardClick(Vector2Int mousePos) {
			var games = _match.Games;
			HumanPlayer human = interactiveHuman(games);
			BotBoard board = human?.PendingBoard;
			if (board == null) return;

			PositionSnapshot snapshot = games[_gameIndex].GetPosition(_ply);
			Vector2Int? square = _renderer.SquareAt(snapshot, mousePos);
			if (square == null) {
				clearSelection();
				return;
			}

			if (_selectedSquare.HasValue) {
				Move move = _selectedMoves.FirstOrDefault(m => m.To == square.Value);
				if (move != null) {
					human.SubmitMove(move);
					_autoPlay = true;
					_playTimer = 1;
					clearSelection();
					return;
				}
			}

			if (board.GetPiece(square.Value).Team == board.SideToMove && square != _selectedSquare) {
				_selectedSquare = square;
				_selectedMoves = board.GetLegalMovesFrom(square.Value);
			}
			else {
				clearSelection();
			}
		}

		// ------------------------------------------------------------------------------------
		// Update
		// ------------------------------------------------------------------------------------

		public override void Update(GameTime gameTime) {
			double seconds = gameTime.ElapsedGameTime.TotalSeconds;
			_time += seconds;
			layout();

			var games = _match.Games;
			GameRecord game = currentGame(games);
			if (game == null) return;

			// A human has to move: jump to the live position.
			BotBoard pendingBoard = _match.GetWaitingHuman()?.PendingBoard;
			if (pendingBoard != null && pendingBoard != _lastPendingBoard) {
				_gameIndex = games.Count - 1;
				game = games[_gameIndex];
				_ply = game.PositionCount - 1;
				_autoPlay = true;
				clearSelection();
			}
			_lastPendingBoard = pendingBoard;

			// Human games follow each committed position immediately, including while the bot thinks.
			if (_autoPlay && _gameIndex == games.Count - 1 && (_match.Settings.Player1.IsHuman || _match.Settings.Player2.IsHuman)
				&& !games[^1].IsFinished) {
				_gameIndex = games.Count - 1;
				game = games[_gameIndex];
				_ply = game.PositionCount - 1;
				return;
			}

			int last = game.PositionCount - 1;
			_ply = Math.Clamp(_ply, 0, last);
			if (!_autoPlay || pendingBoard != null) return;

			float speed = SPEEDS[_speedIndex];
			if (_ply < last) {
				_endOfGameTimer = 0;
				if (float.IsInfinity(speed)) {
					_ply = last;
				}
				else {
					_playTimer += seconds * speed;
					while (_playTimer >= 1 && _ply < last) {
						_ply++;
						_playTimer -= 1;
					}
				}
			}
			else if (!game.IsFinished) {
				// Waiting for the next move: show it as soon as it is played.
				_playTimer = 1;
			}
			else if (_gameIndex < games.Count - 1) {
				// Pause on the final position, then continue with the next game.
				_endOfGameTimer += seconds;
				if (_endOfGameTimer >= END_OF_GAME_PAUSE_SECONDS) {
					selectGame(_gameIndex + 1);
				}
			}
		}

		// ------------------------------------------------------------------------------------
		// Layout
		// ------------------------------------------------------------------------------------

		private void layout() {
			Point screen = Game1.ScreenSize;
			float u = screen.Y / 40f;
			_unit = u;
			int margin = (int)u;
			int panelWidth = (int)Math.Max(screen.X * 0.21f, 11 * u);
			int labelHeight = (int)(1.9f * u);
			int controlsHeight = (int)(2.1f * u);
			int gap = (int)(0.6f * u);

			_leftPanel = new Rectangle(margin, margin, panelWidth, screen.Y - 2 * margin);
			_rightPanel = new Rectangle(screen.X - margin - panelWidth, margin, panelWidth, screen.Y - 2 * margin);

			int centerX = _leftPanel.Right + margin;
			int centerWidth = _rightPanel.X - margin - centerX;
            int boardSize = Math.Max(8, Math.Min(centerWidth - 44, screen.Y - 2 * margin - 2 * labelHeight - controlsHeight - 2 * gap - 22));
            int boardX = centerX + 44 + (centerWidth - 44 - boardSize) / 2;

			int y = margin;
			_topLabel = new Rectangle(boardX, y, boardSize, labelHeight);
			y += labelHeight + gap / 2;
			_boardArea = new Rectangle(boardX, y, boardSize, boardSize);
            y += boardSize + gap / 2 + 22;
			_bottomLabel = new Rectangle(boardX, y, boardSize, labelHeight);
			y += labelHeight + gap;

			// Controls: |<  <  Play  >  >|   -  speed  +   Flip   (relative widths)
			float[] widths = { 1.2f, 1.2f, 2.2f, 1.2f, 1.2f, 0.5f, 1f, 3f, 1f, 0.5f, 1.8f };
			float unitWidth = boardSize / widths.Sum();
			float x = boardX;
			int control = 0;
			for (int i = 0; i < widths.Length; i++) {
				Rectangle rect = new((int)x + 2, y, (int)(widths[i] * unitWidth) - 4, controlsHeight);
				x += widths[i] * unitWidth;
				if (i == 5 || i == 9) continue;
				if (i == 7) {
					_speedLabel = rect;
					continue;
				}
				_controlRects[control++] = rect;
			}

			_backButton = new Rectangle(_leftPanel.X, _leftPanel.Bottom - (int)(2.2f * u), _leftPanel.Width, (int)(2.2f * u));
			int gamesTop = _leftPanel.Y + (int)(12.6f * u);
			_gamesListArea = new Rectangle(_leftPanel.X, gamesTop, _leftPanel.Width, _backButton.Y - gap - gamesTop);
			int movesTop = _rightPanel.Y + (int)(9.4f * u);
			_movesListArea = new Rectangle(_rightPanel.X, movesTop, _rightPanel.Width, _rightPanel.Bottom - (int)(4.2f * u) - movesTop);

			_renderer.Area = _boardArea;
		}

		// ------------------------------------------------------------------------------------
		// Drawing
		// ------------------------------------------------------------------------------------

		public override void Draw(SpriteBatch spriteBatch) {
			layout();
			var games = _match.Games;
			GameRecord game = currentGame(games);

			spriteBatch.DrawRectangle(_leftPanel, Theme.Panel);
			spriteBatch.DrawRectangle(_rightPanel, Theme.Panel);
			drawMatchPanel(spriteBatch, games);

			if (game == null) {
				spriteBatch.DrawTextCentered("Starting match...", _boardArea, _unit * 1.2f, Theme.TextDim);
				base.Draw(spriteBatch);
				return;
			}

			int last = game.PositionCount - 1;
			_ply = Math.Clamp(_ply, 0, last);
			PositionSnapshot snapshot = game.GetPosition(_ply);

			// Show the human's own side at the bottom.
            if (_analysisSnapshot != snapshot) {
                _analysisSnapshot = snapshot;
                _analysisPosition = AnalysisPosition.FromGame(game, _ply);
            }
            _evaluation.Update(_analysisPosition);
			bool humanIsBlackOnly = _match.Settings.Player1.IsHuman != _match.Settings.Player2.IsHuman
				&& (game.WhitePlayerIndex == 0 ? _match.Settings.Player2.IsHuman : _match.Settings.Player1.IsHuman);
			_renderer.Flipped = humanIsBlackOnly != _manualFlip;

			bool interactive = interactiveHuman(games) != null;
			_renderer.Draw(spriteBatch, snapshot,
				interactive ? _selectedSquare : null,
				interactive ? _selectedMoves.Select(m => m.To) : null);
            EvaluationBar.Draw(spriteBatch, _renderer.BoardRectangle(snapshot), _renderer.Flipped, _evaluation);

			TeamType topTeam = _renderer.Flipped ? TeamType.White : TeamType.Black;
			drawPlayerLabel(spriteBatch, _topLabel, game, topTeam, interactive);
			drawPlayerLabel(spriteBatch, _bottomLabel, game, topTeam == TeamType.White ? TeamType.Black : TeamType.White, interactive);

			if (game.IsFinished && _ply == last) {
				drawResultBanner(spriteBatch, game);
			}

			spriteBatch.DrawTextCentered(speedText(SPEEDS[_speedIndex]), _speedLabel, _speedLabel.Height * 0.5f, Theme.Text);
			drawGamePanel(spriteBatch, game);
			base.Draw(spriteBatch);
		}

		private void drawMatchPanel(SpriteBatch spriteBatch, List<GameRecord> games) {
			float u = _unit;
			Rectangle panel = _leftPanel;
			int padding = (int)(0.6f * u);
			int innerX = panel.X + padding;
			int innerWidth = panel.Width - 2 * padding;
			float y = panel.Y + padding;

			spriteBatch.DrawTextLine("Match", new Vector2(innerX, y), 1.4f * u, Theme.Text);
			y += 2.0f * u;

			for (int player = 0; player < 2; player++) {
				string score = formatScore(_match.GetScore(player));
				Vector2 scoreSize = SpriteBatchExtensions.MeasureText(score, 1.3f * u);
				string name = SpriteBatchExtensions.FitText(_match.PlayerNames[player], 1.0f * u, innerWidth - scoreSize.X - u);
				spriteBatch.DrawTextLine(name, new Vector2(innerX, y + 0.2f * u), 1.0f * u, Theme.Text);
				spriteBatch.DrawTextLine(score, new Vector2(innerX + innerWidth - scoreSize.X, y), 1.3f * u, Theme.Text);
				y += 1.7f * u;
			}

			string record = $"Wins {_match.GetWins(0)} : {_match.GetWins(1)}   Draws {_match.Draws}";
			spriteBatch.DrawTextLine(record, new Vector2(innerX, y), 0.8f * u, Theme.TextDim);
			y += 1.3f * u;

			string status;
			Color statusColor = Theme.TextDim;
			if (_match.IsCancelled) {
				status = "Match stopped";
			}
			else if (_match.IsFinished) {
				status = "Match finished";
				statusColor = Theme.Good;
			}
			else {
				status = $"Playing game {games.Count} of {_match.Settings.Games}";
				statusColor = Theme.Warning;
			}
			spriteBatch.DrawTextLine(status, new Vector2(innerX, y), 0.9f * u, statusColor);
			y += 1.3f * u;
			spriteBatch.DrawTextLine($"{_match.Settings.TimeLimitMilliseconds} ms per move", new Vector2(innerX, y), 0.8f * u, Theme.TextDim);

			// Games list.
			spriteBatch.DrawTextLine("Games", new Vector2(innerX, _gamesListArea.Y - 1.6f * u), 1.0f * u, Theme.Text);
			_gameRows.Clear();
			int rowHeight = (int)(1.35f * u);
			int visibleRows = Math.Max(1, _gamesListArea.Height / rowHeight);
			if (_gameIndex != _scrolledToGameIndex) {
				// Scroll the newly selected game into view.
				if (_gameIndex < _gamesScroll) _gamesScroll = _gameIndex;
				if (_gameIndex >= _gamesScroll + visibleRows) _gamesScroll = _gameIndex - visibleRows + 1;
				_scrolledToGameIndex = _gameIndex;
			}
			_gamesScroll = Math.Clamp(_gamesScroll, 0, Math.Max(0, games.Count - visibleRows));

			for (int i = _gamesScroll; i < games.Count && i < _gamesScroll + visibleRows; i++) {
				GameRecord game = games[i];
				Rectangle row = new(panel.X, _gamesListArea.Y + (i - _gamesScroll) * rowHeight, panel.Width, rowHeight);
				if (i == _gameIndex) spriteBatch.DrawRectangle(row, Theme.PanelLight);
				_gameRows.Add((row, i));

				string result = game.IsFinished ? game.ResultScore : "...";
				float textHeight = rowHeight * 0.6f;
				Vector2 resultSize = SpriteBatchExtensions.MeasureText(result, textHeight);
				string title = SpriteBatchExtensions.FitText($"{game.Number}. {game.WhiteName} - {game.BlackName}", textHeight, innerWidth - resultSize.X - u);
				float textY = row.Y + (rowHeight - textHeight) / 2;
				spriteBatch.DrawTextLine(title, new Vector2(innerX, textY), textHeight, Theme.Text);
				spriteBatch.DrawTextLine(result, new Vector2(innerX + innerWidth - resultSize.X, textY), textHeight, resultColor(game));
			}
		}

		private void drawGamePanel(SpriteBatch spriteBatch, GameRecord game) {
			float u = _unit;
			Rectangle panel = _rightPanel;
			int padding = (int)(0.6f * u);
			int innerX = panel.X + padding;
			int innerWidth = panel.Width - 2 * padding;
			float y = panel.Y + padding;

			int totalPlies = game.PositionCount - 1;
			spriteBatch.DrawTextLine($"Game {game.Number}", new Vector2(innerX, y), 1.4f * u, Theme.Text);
			y += 2.0f * u;
			spriteBatch.DrawTextLine($"Half-move {_ply} of {totalPlies}", new Vector2(innerX, y), 0.85f * u, Theme.TextDim);
			y += 1.2f * u;

			foreach (string line in wrapText(game.ResultText, 0.85f * u, innerWidth).Take(3)) {
				spriteBatch.DrawTextLine(line, new Vector2(innerX, y), 0.85f * u, game.IsFinished ? resultColor(game) : Theme.Warning);
				y += 1.05f * u;
			}

			// Move list.
			spriteBatch.DrawTextLine("Moves", new Vector2(innerX, _movesListArea.Y - 1.6f * u), 1.0f * u, Theme.Text);
			_moveCells.Clear();
			int rowHeight = (int)(1.2f * u);
			int rows = (totalPlies + 1) / 2;
			int visibleRows = Math.Max(1, _movesListArea.Height / rowHeight);
			int currentRow = _ply > 0 ? (_ply - 1) / 2 : 0;
			int firstRow = Math.Clamp(currentRow - visibleRows / 2, 0, Math.Max(0, rows - visibleRows));
			int numberWidth = (int)(2.6f * u);
			int columnWidth = (innerWidth - numberWidth) / 2;
			float textHeight = rowHeight * 0.65f;

			for (int row = firstRow; row < rows && row < firstRow + visibleRows; row++) {
				int rowY = _movesListArea.Y + (row - firstRow) * rowHeight;
				spriteBatch.DrawTextLine($"{row + 1}.", new Vector2(innerX, rowY + (rowHeight - textHeight) / 2), textHeight, Theme.TextDim);
				for (int column = 0; column < 2; column++) {
					int ply = row * 2 + column + 1;
					if (ply > totalPlies) break;
					Rectangle cell = new(innerX + numberWidth + column * columnWidth, rowY, columnWidth - 4, rowHeight);
					if (ply == _ply) spriteBatch.DrawRectangle(cell, Theme.Accent);
					string text = SpriteBatchExtensions.FitText(game.GetPosition(ply).MoveText, textHeight, cell.Width - 8);
					spriteBatch.DrawTextLine(text, new Vector2(cell.X + 4, rowY + (rowHeight - textHeight) / 2), textHeight, Theme.Text);
					_moveCells.Add((cell, ply));
				}
			}

			// Help.
			string[] help = {
				"Space: play/pause   Left/Right: step",
				"Home/End: start/end   Up/Down: game",
				"+/-: speed   F: flip   Esc: menu",
			};
			float helpY = panel.Bottom - padding - help.Length * 1.1f * u;
			foreach (string line in help) {
				spriteBatch.DrawTextLine(SpriteBatchExtensions.FitText(line, 0.75f * u, innerWidth), new Vector2(innerX, helpY), 0.75f * u, Theme.TextDim);
				helpY += 1.1f * u;
			}
		}

		private void drawPlayerLabel(SpriteBatch spriteBatch, Rectangle rect, GameRecord game, TeamType team, bool humanToMove) {
			int iconSize = (int)(rect.Height * 0.9f);
			var icon = TextureLoader.KingTexture[team == TeamType.White ? 0 : 1];
			spriteBatch.Draw(icon, new Rectangle(rect.X, rect.Y + (rect.Height - iconSize) / 2, iconSize, iconSize), Color.White);

			float textHeight = rect.Height * 0.6f;
			string name = team == TeamType.White ? game.WhiteName : game.BlackName;

			// Status on the right side.
			string status = "";
			Color statusColor = Theme.TextDim;
			int last = game.PositionCount - 1;
			PositionSnapshot snapshot = game.GetPosition(_ply);
			bool winner = (game.Result == GameResult.WhiteWins && team == TeamType.White)
				|| (game.Result == GameResult.BlackWins && team == TeamType.Black);
			if (game.IsFinished && _ply == last && winner) {
				status = "Winner";
				statusColor = Theme.Good;
			}
			else if (!game.IsFinished && _ply == last && snapshot.SideToMove == team) {
				if (humanToMove) {
					status = "Your move";
					statusColor = Theme.Good;
				}
				else {
					status = "thinking" + new string('.', 1 + (int)(_time * 3) % 3);
					statusColor = Theme.Warning;
				}
			}
			else {
				// Think time of this side's most recent move.
				int ply = snapshot.SideToMove == team ? _ply - 1 : _ply;
				if (ply >= 1) status = formatMilliseconds(game.GetPosition(ply).ThinkMilliseconds);
			}

			Vector2 statusSize = SpriteBatchExtensions.MeasureText(status, textHeight);
			float textY = rect.Y + (rect.Height - textHeight) / 2;
			float nameX = rect.X + iconSize + _unit * 0.5f;
			string fittedName = SpriteBatchExtensions.FitText(name, textHeight, rect.Right - statusSize.X - _unit - nameX);
			spriteBatch.DrawTextLine(fittedName, new Vector2(nameX, textY), textHeight, Theme.Text);
			spriteBatch.DrawTextLine(status, new Vector2(rect.Right - statusSize.X, textY), textHeight, statusColor);
		}

		private void drawResultBanner(SpriteBatch spriteBatch, GameRecord game) {
			Rectangle board = _renderer.BoardRectangle(game.GetPosition(_ply));
			int height = (int)(board.Height * 0.2f);
			Rectangle banner = new(board.X, board.Center.Y - height / 2, board.Width, height);
			spriteBatch.DrawRectangle(banner, Color.Black * 0.75f);

			string title = game.Result switch {
				GameResult.WhiteWins => "White wins",
				GameResult.BlackWins => "Black wins",
				GameResult.Draw => "Draw",
				_ => "Aborted"
			};
			Rectangle titleRect = new(banner.X, banner.Y + (int)(height * 0.08f), banner.Width, (int)(height * 0.5f));
			spriteBatch.DrawTextCentered($"{title}  {game.ResultScore}", titleRect, height * 0.4f, resultColor(game));
			Rectangle reasonRect = new(banner.X + 8, banner.Y + (int)(height * 0.58f), banner.Width - 16, (int)(height * 0.35f));
			spriteBatch.DrawTextCentered(game.ResultReason, reasonRect, height * 0.2f, Theme.Text);
		}

		private Color resultColor(GameRecord game) {
			return game.Result switch {
				GameResult.Draw => Theme.Warning,
				GameResult.Aborted => Theme.TextDim,
				_ => Theme.Good
			};
		}

		private static string formatScore(double score) {
			return score.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
		}

		private static string formatMilliseconds(long milliseconds) {
			return milliseconds < 1000 ? $"{milliseconds} ms" : $"{milliseconds / 1000.0:0.00} s";
		}

		private static IEnumerable<string> wrapText(string text, float height, float maxWidth) {
			string line = "";
			foreach (string word in text.Split(' ')) {
				string candidate = line.Length == 0 ? word : line + " " + word;
				if (line.Length > 0 && SpriteBatchExtensions.MeasureText(candidate, height).X > maxWidth) {
					yield return line;
					line = word;
				}
				else {
					line = candidate;
				}
			}
			if (line.Length > 0) yield return SpriteBatchExtensions.FitText(line, height, maxWidth);
		}
	}
}
