using DChess.Chess.Pieces;
using DChess.Extensions;
using DChess.UI;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Playground {
	public class BoardUI : UI.IDrawable {
		// Color variables.
		private readonly Color _midColor;
		private static readonly Color LIGHT_SQUARES_COLOR = new(242 / 255f, 225 / 255f, 195 / 255f);
		private static readonly Color DARK_SQUARES_COLOR = new(195 / 255f, 160 / 255f, 130 / 255f);
		private static readonly Color MOVE_HIGHLIGHT = new(1, 1, 0, .1f);

		private readonly Board _board;
		private readonly BoardManager _boardManager;
		private readonly SquareUI[,] _squaresUI;

		private SquareUI _selectedSquare = null;
		private List<Vector2Int> _legalMovesWithSelected { get; set; }

		public BoardUI(Board board, BoardManager boardManager) {
			_board = board;
			_boardManager = boardManager;
			_squaresUI = new SquareUI[board.Size.x, board.Size.y];
			_legalMovesWithSelected = new List<Vector2Int>();
			_midColor = LIGHT_SQUARES_COLOR.AverageColor(DARK_SQUARES_COLOR);
			Initialize();
		}

		private void Initialize() {
			for (int x = 0; x < _board.Size.x; x++) {
				for (int y = 0; y < _board.Size.y; y++) {
					byte b = (byte)((byte)(x + 9 * y) % 2);
					TeamType teamType = b == 1 ? TeamType.White : TeamType.Black;
					Color color = teamType == TeamType.White ? LIGHT_SQUARES_COLOR : DARK_SQUARES_COLOR;
					_squaresUI[x, y] = new SquareUI(this, new Vector2Int(x, y), color);
				}
			}
		}

		public SquareUI[,] GetSquaresUI() {
			return _squaresUI;
		}

		public void SelectSquare(SquareUI squareUI) {
			Piece piece = _board.GetPiece(squareUI.Position);
			if (_selectedSquare == null
				&& piece != null
				&& piece.Team == _board.GetTurnTeamType()
				&& piece.GetAllLegalMoves(squareUI.Position).Count > 0) {
				_legalMovesWithSelected = ChessUtil.CreateDestinationsListFromMoveList(piece.GetAllLegalMoves(squareUI.Position));
				_selectedSquare = squareUI;
			}
			else if (_selectedSquare != null) {
				var pos1 = _selectedSquare.Position;
				var pos2 = squareUI.Position;
				var newPiece = _board.GetPiece(pos1);
				var move = new Move();
				move.AddChange(pos1, newPiece, Piece.NULL_PIECE);
				move.AddChange(pos2, piece, newPiece);
				_boardManager.MakeMove(move);
				_legalMovesWithSelected = new List<Vector2Int>();
				_selectedSquare = null;
			}
		}



		public void Draw(SpriteBatch spriteBatch) {
			foreach (var squareUI in _squaresUI) {
				if (squareUI == null 
					|| _board.SquareMap[squareUI.Position.x, squareUI.Position.y] == SquareType.Disabled) continue;
				squareUI.Draw(spriteBatch);
			}
			drawLastMoveHighlight(spriteBatch);
			drawLegalMoves(spriteBatch);
			drawPieces(spriteBatch);
		}

		private void drawLegalMoves(SpriteBatch spriteBatch) {
			ScalingUtil scalingUtil = ScalingUtil.Instance;
			List<Vector2Int> moves = _legalMovesWithSelected;
			if (moves == null) {
				return;
			}
			foreach (var move in moves) {
				float offset = (scalingUtil.SquareSize / 4) * scalingUtil.Scale;
				Vector2 windowPosition = scalingUtil.GetWindowPositionFromBoard(move) + new Vector2(offset, offset);
				spriteBatch.DrawSprite(TextureLoader.CircleTexture, windowPosition, _midColor, scalingUtil.CircleFactor * 0.5f);
			}
		}

		private void drawPieces(SpriteBatch spriteBatch) {
			ScalingUtil scalingUtil = ScalingUtil.Instance;
			foreach (var positionPiecePair in _board.GetPieceDictionary()) {
				Vector2 windowPosition = scalingUtil.GetWindowPositionFromBoard(positionPiecePair.Key);

				var piece = positionPiecePair.Value;
				if (piece != null && piece != Piece.NULL_PIECE) {
					spriteBatch.DrawSprite(Piece.GetPieceTexture(piece), windowPosition, Color.White, scalingUtil.PieceFactor);
				}
			}
		}

		private void drawLastMoveHighlight(SpriteBatch spriteBatch) {
            if (_board.GetMoveCount() == 0) return;
            Move lastMove = _board.GetLastMove();

			foreach (var change in lastMove.Changes) {
				var square = new SquareUI(this, change.boardPosition, MOVE_HIGHLIGHT);
                square.Draw(spriteBatch);
			}
		}
	}
}
