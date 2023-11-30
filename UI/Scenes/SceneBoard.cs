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
		private readonly Board _board;
		
		public SceneBoard(BoardUI boardUI, Board board) {
			_boardUI = boardUI;
			_board = board;

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
			spriteBatch.DrawBoundedText($"Moves: {_board.GetMoveCount()}", 
				new Vector2(0,0), 
				Color.White, 
				new Vector2Int(100, 100),
				Game1.Font);
			base.Draw(spriteBatch);
		}
	}
}
