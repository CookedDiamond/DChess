using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes {
	public abstract class Scene : IDrawable {

		protected List<IDrawable> content = new();

		protected ButtonManager buttonManager = new();

		public Color BackGroundColor { get; protected set; } = Color.White;

		public virtual void MouseClick(Vector2Int mousePos) {
			buttonManager.OnClick(mousePos);
		}

		public void MouseHover(Vector2Int mousePos) {
			buttonManager.OnHover(mousePos);
		}

		/// <summary>Mouse wheel moved. Positive delta = scrolled up.</summary>
		public virtual void MouseScroll(Vector2Int mousePos, int delta) {
		}

		/// <summary>A key was pressed (called once per press, arrow keys repeat while held).</summary>
		public virtual void KeyPressed(Keys key) {
		}

		public virtual void Update(GameTime gameTime) {
		}

		public virtual void Draw(SpriteBatch spriteBatch) {
			if (content == null) return;

			foreach (var item in content) {
				item.Draw(spriteBatch);
			}
		}
	}
}
