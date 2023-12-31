﻿using DChess.Util;
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

		protected ButtonManager buttonManager = new();

		public Color BackGroundColor { get; protected set; } = Color.White;

		public void MouseClick(Vector2Int mousePos) {
			buttonManager.OnClick(mousePos);
		}

		public void MouseHover(Vector2Int mousePos) {
			buttonManager.OnHover(mousePos);
		}

		public virtual void Draw(SpriteBatch spriteBatch) {
			if (content == null) return;

			foreach (var item in content) {
				item.Draw(spriteBatch);
			}
		}
	}
}
