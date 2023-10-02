using DChess.Chess;
using DChess.Extensions;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes {
	public class SceneBoard : Scene {
		private readonly Board _board;
		private readonly ScalingUtil _gameScaling;

		// Color variables.
		private readonly Color _midColor;
		private readonly Color _lightSquaresColor = new(242 / 255f, 225 / 255f, 195 / 255f);
		private readonly Color _darkSquaresColor = new(195 / 255f, 160 / 255f, 130 / 255f);

		public SceneBoard(Board board) {
			_board = board;
			_gameScaling = ScalingUtil.Instance;

			BackGroundColor = Color.DarkSeaGreen;

			_midColor = _lightSquaresColor.AverageColor(_darkSquaresColor);

			InitializeBoardButtons();
		}

		private void InitializeBoardButtons() {
			foreach (var square in _board.GetSquares()) {
				var button = new ButtonBoard(square.position);
				button.OnClickEvent += () => square.OnClick();
				buttonManager.AddButton(button);
			}
		}

		public override void Draw(SpriteBatch spriteBatch) {
			// Draw Board
			drawBoard(spriteBatch);
			drawLegalMoves(spriteBatch);
			drawPieces(spriteBatch);

			base.Draw(spriteBatch);
		}

		private void drawBoard(SpriteBatch spriteBatch) {
			foreach (var square in _board.GetSquares()) {
				Vector2 windowPosition = _gameScaling.GetWindowPositionFromBoard(square.position);

				Color color = (square.team == TeamType.White) ? _lightSquaresColor : _darkSquaresColor;
				spriteBatch.DrawSprite(TextureLoader.SquareTexture, windowPosition, color, 1);
			}
		}

		private void drawLegalMoves(SpriteBatch spriteBatch) {
			List<Vector2Int> moves = _board.legalMovesWithSelected;
			if (moves == null) {
				return;
			}
			foreach (var move in moves) {
				float offset = (_gameScaling.SquareSize / 4) * _gameScaling.Scale;
				Vector2 windowPosition = _gameScaling.GetWindowPositionFromBoard(move) + new Vector2(offset, offset);
				spriteBatch.DrawSprite(TextureLoader.CircleTexture, windowPosition, _midColor, _gameScaling.CircleFactor * 0.5f);
			}
		}

		private void drawPieces(SpriteBatch spriteBatch) {
			foreach (var square in _board.GetSquares()) {
				Vector2 windowPosition = _gameScaling.GetWindowPositionFromBoard(square.position);

				var piece = square.piece;
				if (piece != null) {
					spriteBatch.DrawSprite(square.piece.GetPieceTexture(), windowPosition, Color.White, _gameScaling.PieceFactor);
				}
			}
		}
	}
}
