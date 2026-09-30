using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.Extensions;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.UI.Scenes
{
	/// <summary>
	/// Main menu: choose two players (bots or human), number of games and time per move.
	/// </summary>
	public class SceneMenu : Scene {
		private static readonly int[] GAME_COUNTS = { 1, 2, 4, 6, 10, 20, 50, 100 };
		private static readonly int[] TIME_LIMITS = { 50, 100, 250, 500, 1000, 2000, 5000, 10000 };

		private readonly Game1 _game;
		private readonly IReadOnlyList<BotInfo> _players;

		// Selected option per row: player 1, player 2, games, time.
		private readonly int[] _selection = new int[4];
		private readonly Rectangle[] _rows = new Rectangle[4];
		private readonly string[] _rowLabels = { "Player 1", "Player 2", "Games", "Time per move" };

		private float _unit;
		private Rectangle _panel;
		private Rectangle _startButton;
		private Rectangle _sandboxButton;
		private Rectangle _quitButton;
		private Rectangle _resumeButton;

		public SceneMenu(Game1 game) {
			_game = game;
			_players = BotRegistry.AllPlayers;
			BackGroundColor = Theme.Background;

			int greedy = indexOfPlayer("GreedyBot");
			_selection[0] = greedy;
			_selection[1] = greedy;
			_selection[2] = Array.IndexOf(GAME_COUNTS, 2);
			_selection[3] = Array.IndexOf(TIME_LIMITS, 1000);

			for (int row = 0; row < _rows.Length; row++) {
				int r = row;
				addButton(new ButtonRect(() => leftArrow(_rows[r]), "<"), () => changeSelection(r, -1));
				addButton(new ButtonRect(() => rightArrow(_rows[r]), ">"), () => changeSelection(r, +1));
			}

			var start = new ButtonRect(() => _startButton, "Start Match") { IsHighlighted = () => true };
			addButton(start, startMatch);
			addButton(new ButtonRect(() => _sandboxButton, "Sandbox (free play)"), startSandbox);
			addButton(new ButtonRect(() => _quitButton, "Quit"), () => _game.Exit());
			addButton(new ButtonRect(() => _resumeButton, "Resume saved game") { IsEnabled = () => _game.CanResume }, () => _game.ResumeGame());
		}

		private void addButton(ButtonRect button, Button.OnButtonClicked onClick) {
			button.Initialize(buttonManager, onClick);
			content.Add(button);
		}

		private int indexOfPlayer(string name) {
			for (int i = 0; i < _players.Count; i++) {
				if (_players[i].Name == name) return i;
			}
			return _players.Count > 1 ? 1 : 0;
		}

		private int optionCount(int row) {
			return row switch {
				0 or 1 => _players.Count,
				2 => GAME_COUNTS.Length,
				_ => TIME_LIMITS.Length
			};
		}

		private void changeSelection(int row, int direction) {
			int count = optionCount(row);
			_selection[row] = (_selection[row] + direction + count) % count;
		}

		private string optionText(int row) {
			return row switch {
				0 or 1 => _players[_selection[row]].Name,
				2 => GAME_COUNTS[_selection[2]].ToString(),
				_ => formatTime(TIME_LIMITS[_selection[3]])
			};
		}

		private static string formatTime(int milliseconds) {
			return milliseconds < 1000 ? $"{milliseconds} ms" : $"{milliseconds / 1000.0:0.#} s";
		}

		private MatchSettings createSettings() {
			return new MatchSettings {
				Player1 = _players[_selection[0]],
				Player2 = _players[_selection[1]],
				Games = GAME_COUNTS[_selection[2]],
				TimeLimitMilliseconds = TIME_LIMITS[_selection[3]],
			};
		}

		private void startMatch() {
			_game.StartMatch(createSettings());
		}

		private void startSandbox() {
			var settings = createSettings();
			BotInfo helper = new[] { settings.Player1, settings.Player2 }.FirstOrDefault(p => !p.IsHuman);
			_game.StartSandbox(helper, settings.TimeLimitMilliseconds);
		}

		public override void KeyPressed(Keys key) {
			if (key == Keys.Enter) startMatch();
			if (key == Keys.Escape) _game.Exit();
		}

		public override void Update(GameTime gameTime) {
			layout();
		}

		private void layout() {
			Point screen = Game1.ScreenSize;
			_unit = screen.Y / 20f;
			float u = _unit;

			int panelWidth = (int)Math.Min(screen.X * 0.9f, 16 * u);
			int panelX = (screen.X - panelWidth) / 2;
			int rowHeight = (int)(1.3f * u);
			int rowGap = (int)(0.45f * u);
			int top = (int)(4.6f * u);

			for (int row = 0; row < _rows.Length; row++) {
				_rows[row] = new Rectangle(panelX, top + row * (rowHeight + rowGap), panelWidth, rowHeight);
			}

			int buttonsTop = _rows[^1].Bottom + (int)(1.2f * u);
			_startButton = new Rectangle(panelX, buttonsTop, panelWidth, (int)(1.6f * u));
			int halfWidth = (panelWidth - rowGap) / 2;
			int secondTop = _startButton.Bottom + rowGap;
			_sandboxButton = new Rectangle(panelX, secondTop, halfWidth, (int)(1.2f * u));
			_quitButton = new Rectangle(panelX + panelWidth - halfWidth, secondTop, halfWidth, (int)(1.2f * u));
			_resumeButton = new Rectangle(panelX, _quitButton.Bottom + rowGap, panelWidth, (int)(1.2f * u));
			_panel = new Rectangle(panelX - (int)u, _rows[0].Y - (int)u, panelWidth + 2 * (int)u, _resumeButton.Bottom - _rows[0].Y + 2 * (int)u);
		}

		// The selector of a row takes the right 60% of the row: [<] value [>]
		private static Rectangle selectorArea(Rectangle row) {
			int width = (int)(row.Width * 0.6f);
			return new Rectangle(row.Right - width, row.Y, width, row.Height);
		}

		private static Rectangle leftArrow(Rectangle row) {
			Rectangle area = selectorArea(row);
			return new Rectangle(area.X, area.Y, area.Height, area.Height);
		}

		private static Rectangle rightArrow(Rectangle row) {
			Rectangle area = selectorArea(row);
			return new Rectangle(area.Right - area.Height, area.Y, area.Height, area.Height);
		}

		public override void Draw(SpriteBatch spriteBatch) {
			layout();
			Point screen = Game1.ScreenSize;
			float u = _unit;

			spriteBatch.DrawTextCentered("DChess", new Rectangle(0, (int)(1.0f * u), screen.X, (int)(2.2f * u)), 2.2f * u, Theme.Text);
			spriteBatch.DrawTextCentered("Bot Arena", new Rectangle(0, (int)(3.1f * u), screen.X, (int)(0.9f * u)), 0.9f * u, Theme.TextDim);

			spriteBatch.DrawRectangle(_panel, Theme.Panel);

			for (int row = 0; row < _rows.Length; row++) {
				Rectangle rect = _rows[row];
				spriteBatch.DrawTextLine(_rowLabels[row], new Vector2(rect.X, rect.Y + rect.Height * 0.2f), rect.Height * 0.6f, Theme.Text);

				Rectangle area = selectorArea(rect);
				Rectangle valueArea = new(area.X + area.Height, area.Y, area.Width - 2 * area.Height, area.Height);
				spriteBatch.DrawRectangle(valueArea, Theme.PanelLight);
				spriteBatch.DrawTextCentered(optionText(row), valueArea, rect.Height * 0.55f, Theme.Text);
			}

			string hint = GAME_COUNTS[_selection[2]] > 1
				? "Player 1 starts as White, colors swap every game."
				: "Player 1 plays White.";
			if (_players[_selection[0]].IsHuman || _players[_selection[1]].IsHuman) {
				hint += " Humans have no time limit.";
			}
			Rectangle lastRow = _rows[^1];
			spriteBatch.DrawTextLine(hint, new Vector2(lastRow.X, lastRow.Bottom + 0.4f * u), 0.55f * u, Theme.TextDim);

			base.Draw(spriteBatch);
			if (_game.SaveStatus != null)
				spriteBatch.DrawTextLine(_game.SaveStatus, new Vector2(_panel.X, _panel.Bottom + 0.2f * u), 0.5f * u, Theme.TextDim);

			int botCount = _players.Count(p => !p.IsHuman);
			string footer = $"{botCount} bots found. Add your own bot as a file in the Bots folder (see BOTS.md).";
			spriteBatch.DrawTextCentered(footer, new Rectangle(0, screen.Y - (int)(1.3f * u), screen.X, (int)(0.8f * u)), 0.55f * u, Theme.TextDim);
		}
	}
}
