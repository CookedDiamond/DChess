using DChess.Chess.Playground;
using DChess.UI.Scenes;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Util {
	public class InputHandler {

		private bool _lastMouseStateWasPressed = false;
		private const int _keyInputDelay = 20;
		private int _lastKeyInput = 0;

		public void HandleInputs(MouseState mouseState, Scene activeScene, BoardManager boardManager) {
			handleMouseInput(mouseState, activeScene);
			handleKeyInputs(boardManager);
		}
		private void handleMouseInput(MouseState mouseState, Scene activeScene) {
			var mousePos = new Vector2Int(mouseState.X, mouseState.Y);
			activeScene.MouseHover(mousePos);
			if (_lastMouseStateWasPressed && mouseState.LeftButton == ButtonState.Released) {
				activeScene.MouseClick(mousePos);
			}

			if (mouseState.LeftButton == ButtonState.Pressed) {
				_lastMouseStateWasPressed = true;
			}
			else {
				_lastMouseStateWasPressed = false;
			}
		}

		private void handleKeyInputs(BoardManager boardManager) {
			_lastKeyInput++;
			KeyboardState keyState = Keyboard.GetState();
			if (keyState.IsKeyDown(Keys.A) && _keyInputDelay <= _lastKeyInput) {
				boardManager.MakeComputerMove(automatic: false);
				_lastKeyInput = 0;
			}
			if (keyState.IsKeyDown(Keys.S) && _keyInputDelay <= _lastKeyInput) {
				Debug.WriteLine($"Current Eval: {boardManager.Board.GetEvaluaton()}");
				_lastKeyInput = 0;
			}
			if (keyState.IsKeyDown(Keys.D) && _keyInputDelay <= _lastKeyInput) {
				boardManager.UndoLastMove();
				_lastKeyInput = 0;
			}
		}
	}
}

