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
		private readonly Board _board;
		private readonly GraphicsDeviceManager _graphics;

		private int _squareSize;
		private int _pieceSize;
		public float _pieceFactor { get; private set; }

		public float Scale { get; private set; }
		private readonly float _minScale = .5f;
		private readonly float _maxScale = 4f;

		public float CenterOffsetX { get; private set; }
		public float CenterOffsetY { get; private set; }

public GameScaling(Board board, GraphicsDeviceManager graphics) {
			_board = board;
			_graphics = graphics;
		}

		public void Initialize() {
			_squareSize = TextureLoader.SquareTexture.Bounds.Width;
			_pieceSize = TextureLoader.PawnTexture[0].Bounds.Width;
			_pieceFactor = (float)(_squareSize) / (float)(_pieceSize);
		}

		public void Update() {
			calculateBoardCenterOffsets();
			calculateScale();
		}

		public float GetFactor() {
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
