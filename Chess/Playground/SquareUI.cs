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
	public class SquareUI : UI.IDrawable {
		private readonly BoardUI _boardUI;
		public Vector2Int Position { get; set; }
		private Color _color;

		public SquareUI(BoardUI boardUI, Vector2Int position, Color color) {
			_boardUI = boardUI;
			Position = position;
			_color = color;
		}

		public virtual void OnClick() {
			_boardUI.SelectSquare(this);
		}

		public void Draw(SpriteBatch spriteBatch) {
			Vector2 windowPosition = ScalingUtil.Instance.GetWindowPositionFromBoard(Position);
			spriteBatch.DrawSprite(TextureLoader.SquareTexture, windowPosition, _color, 1);
		}
	}
}
