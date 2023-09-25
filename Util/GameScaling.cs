using DChess.Chess;
using Microsoft.Xna.Framework;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Util {
	public class GameScaling {
		public static GameScaling Instance { get; private set; }

		private readonly Board _board;
		private readonly GraphicsDeviceManager _graphics;

		public int SquareSize { get; private set; }
		private int _pieceSize;
		private int _circleSize;

		public float PieceFactor { get; private set; }
		public float CircleFactor { get; private set; }
		private float _factor = 1;

		public float Scale { get; private set; }
		private readonly float _minScale = .5f;
		private readonly float _maxScale = 4f;

		public float CenterOffsetX { get; private set; }
		public float CenterOffsetY { get; private set; }

		public GameScaling(Board board, GraphicsDeviceManager graphics) {
			_board = board;
			_graphics = graphics;
			Instance ??= this;
		}

		public void Initialize() {
			SquareSize = TextureLoader.SquareTexture.Width;
			_pieceSize = TextureLoader.PawnTexture[0].Width;
			_circleSize = TextureLoader.CircleTexture.Width;
			PieceFactor = (float)(SquareSize) / (float)(_pieceSize);
			CircleFactor = (float)(SquareSize) / (float)(_circleSize);
		}

		public Vector2 GetWindowPositionFromBoard(Vector2Int boardPosition) {
			return new Vector2(boardPosition.x * _factor + CenterOffsetX,
				(_board.Size.y - 1) * _factor - boardPosition.y * _factor + CenterOffsetY);
		}

		public Vector2 GetWindowPositionFromAlignment(WindowAlignment windowAlignment, Vector2Int size) {
			if (windowAlignment == WindowAlignment.BoardLeftCenter) {
				Vector2 boardPos = GetWindowPositionFromBoard(new Vector2Int(0, _board.Size.y / 2));
				float verticalOffset = (_board.Size.y % 2 == 0) ? _factor / 2 : 0;
				return new Vector2(CenterOffsetX - size.x
					, boardPos.Y + verticalOffset);
			}

			else if (windowAlignment == WindowAlignment.Center) {
				float windowHeight = _graphics.PreferredBackBufferHeight;
				float windowWidth = _graphics.PreferredBackBufferWidth;

				return new Vector2(windowWidth / 2 - (size.x * Scale) / 2, windowHeight / 2 - (size.y * Scale) / 2);
			}

			return Vector2.Zero;
		}

		public void Update() {
			calculateBoardCenterOffsets();
			calculateScale();
			_factor = getFactor();
		}

		private float getFactor() {
			return SquareSize * Scale;
		}

		private void calculateBoardCenterOffsets() {
			float boardWidth = _board.Size.x * SquareSize * Scale;
			float boardheight = _board.Size.y * SquareSize * Scale;

			float windowWidth = _graphics.PreferredBackBufferWidth;
			float windowheight = _graphics.PreferredBackBufferHeight;

			CenterOffsetX = (windowWidth - boardWidth) / 2;
			CenterOffsetY = (windowheight - boardheight) / 2;
		}

		private void calculateScale() {
			float boardWidthNoScale = Math.Max(_board.Size.x, _board.Size.y) * SquareSize;

			float windowheight = _graphics.PreferredBackBufferHeight;

			float scale = (windowheight * 0.9f) / boardWidthNoScale;
			Scale = MathHelper.Clamp(scale, _minScale, _maxScale);
		}
	}

	public enum WindowAlignment {
		Center,
		BoardLeftCenter,
		Zero
	}
}
