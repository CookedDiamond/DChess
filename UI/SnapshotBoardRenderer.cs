using DChess.Chess.Arena;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Extensions;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace DChess.UI {
	/// <summary>
	/// Draws a recorded position (PositionSnapshot) into a rectangle of the window.
	/// </summary>
	public class SnapshotBoardRenderer {
		public bool Flipped { get; set; }

		/// <summary>The area the board is drawn into (the board is centered inside it).</summary>
		public Rectangle Area { get; set; }

		public int SquareSize(PositionSnapshot snapshot) {
			return System.Math.Max(1, System.Math.Min(Area.Width / snapshot.Width, Area.Height / snapshot.Height));
		}

		public Rectangle BoardRectangle(PositionSnapshot snapshot) {
			int size = SquareSize(snapshot);
			int width = size * snapshot.Width;
			int height = size * snapshot.Height;
			return new Rectangle(Area.X + (Area.Width - width) / 2, Area.Y + (Area.Height - height) / 2, width, height);
		}

		public Rectangle SquareRectangle(PositionSnapshot snapshot, Vector2Int square) {
			int size = SquareSize(snapshot);
			Rectangle board = BoardRectangle(snapshot);
			int column = Flipped ? snapshot.Width - 1 - square.x : square.x;
			int row = Flipped ? square.y : snapshot.Height - 1 - square.y;
			return new Rectangle(board.X + column * size, board.Y + row * size, size, size);
		}

		/// <returns>The board square under the window position, or null.</returns>
		public Vector2Int? SquareAt(PositionSnapshot snapshot, Vector2Int windowPosition) {
			Rectangle board = BoardRectangle(snapshot);
			if (!board.Contains(windowPosition.x, windowPosition.y)) return null;
			int size = SquareSize(snapshot);
			int column = (windowPosition.x - board.X) / size;
			int row = (windowPosition.y - board.Y) / size;
			int x = Flipped ? snapshot.Width - 1 - column : column;
			int y = Flipped ? row : snapshot.Height - 1 - row;
			return new Vector2Int(x, y);
		}

		public void Draw(SpriteBatch spriteBatch, PositionSnapshot snapshot, Vector2Int? selectedSquare = null, IEnumerable<Vector2Int> targetSquares = null) {
			int size = SquareSize(snapshot);

			for (int x = 0; x < snapshot.Width; x++) {
				for (int y = 0; y < snapshot.Height; y++) {
					if (snapshot.Disabled[x, y]) continue;
					Rectangle rect = SquareRectangle(snapshot, new Vector2Int(x, y));
					bool light = (x + y) % 2 == 1;
					spriteBatch.DrawRectangle(rect, light ? Theme.LightSquare : Theme.DarkSquare);
				}
			}

			foreach (var square in snapshot.ChangedSquares) {
				spriteBatch.DrawRectangle(SquareRectangle(snapshot, square), Theme.LastMove);
			}
			if (selectedSquare.HasValue) {
				spriteBatch.DrawRectangle(SquareRectangle(snapshot, selectedSquare.Value), Theme.Selected);
			}

			for (int x = 0; x < snapshot.Width; x++) {
				for (int y = 0; y < snapshot.Height; y++) {
					PieceType type = snapshot.Types[x, y];
					if (type == PieceType.None) continue;
					Texture2D texture = getTexture(type, snapshot.Teams[x, y]);
					spriteBatch.Draw(texture, SquareRectangle(snapshot, new Vector2Int(x, y)), Color.White);
				}
			}

			if (targetSquares != null) {
				foreach (var square in targetSquares) {
					Rectangle rect = SquareRectangle(snapshot, square);
					int dot = size / 3;
					spriteBatch.Draw(TextureLoader.CircleTexture, new Rectangle(rect.Center.X - dot / 2, rect.Center.Y - dot / 2, dot, dot), Theme.MoveDot);
				}
			}

			drawCoordinates(spriteBatch, snapshot, size);
		}

		private void drawCoordinates(SpriteBatch spriteBatch, PositionSnapshot snapshot, int size) {
			float textHeight = size * 0.22f;
			for (int x = 0; x < snapshot.Width; x++) {
				Rectangle rect = SquareRectangle(snapshot, new Vector2Int(x, Flipped ? snapshot.Height - 1 : 0));
				Color color = (x + (Flipped ? snapshot.Height - 1 : 0)) % 2 == 1 ? Theme.DarkSquare : Theme.LightSquare;
				string file = ((char)('a' + x)).ToString();
				Vector2 textSize = SpriteBatchExtensions.MeasureText(file, textHeight);
				spriteBatch.DrawTextLine(file, new Vector2(rect.Right - textSize.X - 3, rect.Bottom - textSize.Y), textHeight, color);
			}
			for (int y = 0; y < snapshot.Height; y++) {
				Rectangle rect = SquareRectangle(snapshot, new Vector2Int(Flipped ? snapshot.Width - 1 : 0, y));
				Color color = ((Flipped ? snapshot.Width - 1 : 0) + y) % 2 == 1 ? Theme.DarkSquare : Theme.LightSquare;
				spriteBatch.DrawTextLine((y + 1).ToString(), new Vector2(rect.X + 3, rect.Y + 1), textHeight, color);
			}
		}

		private static Texture2D getTexture(PieceType type, TeamType team) {
			int index = team == TeamType.White ? 0 : 1;
			return type switch {
				PieceType.Pawn => TextureLoader.PawnTexture[index],
				PieceType.Bishop => TextureLoader.BishopTexture[index],
				PieceType.Knight => TextureLoader.KnightTexture[index],
				PieceType.Rook => TextureLoader.RookTexture[index],
				PieceType.Queen => TextureLoader.QueenTexture[index],
				_ => TextureLoader.KingTexture[index]
			};
		}
	}
}
