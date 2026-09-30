using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.Chess.Playground;
using DChess.Extensions;
using DChess.Util;
using DChess.Persistence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes
{
	/// <summary>
	/// Sandbox: move pieces for both sides by clicking.
	/// A: let the bot make a move, D: undo, S: print the evaluation, Esc: menu.
	/// </summary>
    public class SceneBoard : Scene {
		private readonly Game1 _game;
		private readonly BoardUI _boardUI;
		private readonly Board _board;
		private readonly BoardManager _boardManager;
		private readonly string _helperBotName;

		public SceneBoard(Game1 game, BotInfo helperBot, int botTimeLimitMilliseconds, Board restoredBoard = null, bool resumeBotMove = false) {
			_game = game;
			_board = restoredBoard ?? BoardSetup.CreateStandardBoard();
			_boardManager = new BoardManager(_board, new BoardNetworking());
			helperBot ??= BotRegistry.Find("MinMaxBot");
			_helperBotName = helperBot?.Name;
			if (helperBot != null) {
				_boardManager.SetComputerBot(helperBot.Create(), botTimeLimitMilliseconds, helperBot.Create);
			}
			_boardUI = _boardManager.BoardUI;
			_boardManager.BoardChanged += () => _game.SaveCurrentSession();
			ScalingUtil.Instance.SetBoard(_board);

			BackGroundColor = Color.DarkSeaGreen;
			InitializeBoardButtons();

			var menuButton = new ButtonRect(() => {
				Point screen = Game1.ScreenSize;
				int height = screen.Y / 20;
				return new Rectangle(screen.X - 5 * height - 10, 10, 5 * height, height);
			}, "Menu (Esc)");
			menuButton.Initialize(buttonManager, () => _game.OpenMenu());
			content.Add(menuButton);
			if (resumeBotMove) _boardManager.BeginComputerMove(false);
		}

		private void InitializeBoardButtons() {
			foreach (var square in _boardUI.GetSquaresUI()) {
				var button = new ButtonBoard(square.Position);
				button.OnClickEvent += () => square.OnClick();
				buttonManager.AddButton(button);
			}
		}

		public override void Update(GameTime gameTime) {
			_boardManager.UpdateComputerMove();
			// Computer move
			if (_boardManager.GetComputerPlayerTeamType() != null
				&& _boardManager.GetComputerPlayerTeamType() == _board.GetTurnTeamType()) {
				_boardManager.BeginComputerMove();
			}
		}

		public override void KeyPressed(Keys key) {
			switch (key) {
				case Keys.A:
					_boardManager.BeginComputerMove(automatic: false);
					break;
				case Keys.S:
					Console.WriteLine($"Current Eval: {_board.GetEvaluaton()}");
					break;
				case Keys.D:
					_boardManager.UndoLastMove();
					break;
				case Keys.Escape:
					_game.OpenMenu();
					break;
			}
		}

		public override void Draw(SpriteBatch spriteBatch) {
			_boardUI.Draw(spriteBatch);
			float lineHeight = Game1.ScreenSize.Y / 30f;
			spriteBatch.DrawTextLine($"Moves: {_board.GetMoveCount()}", new Vector2(10, 10), lineHeight, Color.White);

			TeamType winner = _board.HasTeamWon();
			string status = winner != TeamType.None ? $"{winner} wins!" : $"{_board.GetTurnTeamType()} to move";
			if (_boardManager.IsThinking) status += " - bot thinking...";
			if (_boardManager.BotError != null) status += " - " + _boardManager.BotError;
			spriteBatch.DrawTextLine(status, new Vector2(10, 10 + lineHeight * 1.3f), lineHeight, Color.White);

			string botName = _boardManager.ComputerBotName ?? "MinMaxBot";
			string help = $"A: {botName} moves   D: undo   S: print eval   Esc: menu";
			spriteBatch.DrawTextLine(help, new Vector2(10, Game1.ScreenSize.Y - lineHeight * 1.4f), lineHeight * 0.8f, Color.White);
			base.Draw(spriteBatch);
		}

		public SessionState CaptureState() => new() {
			Mode = "sandbox", Board = BoardState.Capture(_board), HelperBot = _helperBotName,
			BotTimeLimitMilliseconds = _boardManager.ComputerTimeLimitMilliseconds,
			BotMovePending = _boardManager.HasPendingComputerMove
		};

		public void Stop() => _boardManager.CancelComputerMove();
	}
}
