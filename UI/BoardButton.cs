using DChess.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI {
	internal class BoardButton : Button {
		public BoardButton(GameScaling game, Vector2Int boardPosition) : base() {

		}

		protected override Rectangle GetButtonRectangle() {
			return new Rectangle();
		}
	}
}
