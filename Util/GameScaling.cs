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

		public int _squareSize { get; private set; }
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
			_squareSize = TextureLoader.SquareTexture.Width;
			_pieceSize = TextureLoader.PawnTexture[0].Width;
			_circleSize = TextureLoader.Circle.Width;
			PieceFactor = (float)(_squareSize) / (float)(_pieceSize);
			CircleFactor = (float)(_squareSize) / (float)(_circleSize);
		}

		public Vector2 GetWindowPositionFromBoard(Vector2Int boardPosition) {
			return new Vector2(boardPosition.x * _factor + CenterOffsetX,
				(_board._size.y - 1) * _factor - boardPosition.y * _factor + CenterOffsetY);
		}

		public void Update() {
			calculateBoardCenterOffsets();
			calculateScale();
			_factor = getFactor();
		}

		private float getFactor() {
			return _squareSize * Scale;
		}

		private void calculateBoardCenterOffsets() {
			float boardWidth = _board._size.x * _squareSize * Scale;
			float boardheight = _board._size.y * _squareSize * Scale;

			float windowWidth = _graphics.PreferredBackBufferWidth;
			float windowheight = _graphics.PreferredBackBufferHeight;

			CenterOffsetX = (windowWidth - boardWidth) / 2;
			CenterOffsetY = (windowheight - boardheight) / 2;
		}

		private void calculateScale() {
			float boardWidthNoScale = _board._size.x * _squareSize;

			float windowheight = _graphics.PreferredBackBufferHeight;

			float scale = (windowheight * 0.9f) / boardWidthNoScale;
			Scale = MathHelper.Clamp(scale, _minScale, _maxScale);
		}

	}
}
