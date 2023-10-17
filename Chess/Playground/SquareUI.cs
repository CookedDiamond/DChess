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
		public Vector2Int Position { get; private set; }
		private TeamType _teamColor;

		public SquareUI(BoardUI boardUI, Vector2Int position, TeamType teamColor) {
			_boardUI = boardUI;
			Position = position;
			_teamColor = teamColor;
		}

		public virtual void OnClick() {
			_boardUI.SelectSquare(this);
		}

		public void Draw(SpriteBatch spriteBatch) {
			if (_teamColor == TeamType.None) return;
			Vector2 windowPosition = ScalingUtil.Instance.GetWindowPositionFromBoard(Position);

			Color color = (_teamColor == TeamType.White) ? BoardUI.LIGHT_SQUARES_COLOR : BoardUI.DARK_SQUARES_COLOR;
			spriteBatch.DrawSprite(TextureLoader.SquareTexture, windowPosition, color, 1);
		}
	}
}
