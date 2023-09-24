using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes {
	public abstract class Scene : IDrawable {

		protected List<IDrawable> content = new();

		public Color BackGroundColor { get; protected set; } = Color.White;

		public virtual void Draw(SpriteBatch spriteBatch) {
			if (content == null) return;

			foreach (var item in content) {
				item.Draw(spriteBatch);
			}
		}
	}
}
