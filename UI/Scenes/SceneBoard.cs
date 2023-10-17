using DChess.Chess.Playground;
using DChess.Extensions;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes
{
    public class SceneBoard : Scene {
		private readonly BoardUI _boardUI;
		
		public SceneBoard(BoardUI board) {
			_boardUI = board;

			BackGroundColor = Color.DarkSeaGreen;
			InitializeBoardButtons();
		}

		private void InitializeBoardButtons() {
			foreach (var square in _boardUI.GetSquaresUI()) {
				var button = new ButtonBoard(square.Position);
				button.OnClickEvent += () => square.OnClick();
				buttonManager.AddButton(button);
			}
		}

		public override void Draw(SpriteBatch spriteBatch) {
			_boardUI.Draw(spriteBatch);

			base.Draw(spriteBatch);
		}
	}
}
