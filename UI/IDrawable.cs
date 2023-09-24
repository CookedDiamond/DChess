using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI {
	public interface IDrawable {

		public abstract void Draw(SpriteBatch spriteBatch);

	}
}
