using DChess.UI.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Util {
	public class InputHandler {

		private bool _lastMouseStateWasPressed = false;
		private int _lastScrollWheelValue = 0;
		private KeyboardState _lastKeyboardState;

		// Held arrow keys repeat, so you can fast forward through a game.
		private static readonly Keys[] REPEATING_KEYS = { Keys.Left, Keys.Right, Keys.Up, Keys.Down };
		private const double REPEAT_DELAY_SECONDS = 0.4;
		private const double REPEAT_INTERVAL_SECONDS = 0.06;
		private readonly Dictionary<Keys, double> _keyHeldSeconds = new();

		public void HandleInputs(MouseState mouseState, KeyboardState keyboardState, Scene activeScene, GameTime gameTime) {
			handleMouseInput(mouseState, activeScene);
			handleKeyInputs(keyboardState, activeScene, gameTime);
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

			int scrollDelta = mouseState.ScrollWheelValue - _lastScrollWheelValue;
			_lastScrollWheelValue = mouseState.ScrollWheelValue;
			if (scrollDelta != 0) {
				activeScene.MouseScroll(mousePos, scrollDelta);
			}
		}

		private void handleKeyInputs(KeyboardState keyboardState, Scene activeScene, GameTime gameTime) {
			double seconds = gameTime.ElapsedGameTime.TotalSeconds;
			foreach (Keys key in keyboardState.GetPressedKeys()) {
				if (!_lastKeyboardState.IsKeyDown(key)) {
					activeScene.KeyPressed(key);
					_keyHeldSeconds[key] = 0;
					continue;
				}
				if (!REPEATING_KEYS.Contains(key)) continue;

				double before = _keyHeldSeconds.GetValueOrDefault(key);
				double after = before + seconds;
				_keyHeldSeconds[key] = after;
				if (after < REPEAT_DELAY_SECONDS) continue;
				int repeatsBefore = (int)Math.Floor((before - REPEAT_DELAY_SECONDS) / REPEAT_INTERVAL_SECONDS);
				int repeatsAfter = (int)Math.Floor((after - REPEAT_DELAY_SECONDS) / REPEAT_INTERVAL_SECONDS);
				for (int i = Math.Max(repeatsBefore, -1); i < repeatsAfter; i++) {
					activeScene.KeyPressed(key);
				}
			}
			_lastKeyboardState = keyboardState;
		}
	}
}
