using DChess.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes {
	public class SceneMenu : Scene {

		public SceneMenu() {
			ButtonText textButton = new("Start", WindowAlignment.Center, new Vector2Int(100, 20), Color.Black, Color.White);
			textButton.OnClickEvent += () => testMethod();
			buttonManager.AddButton(textButton);
			content.Add(textButton);

			BackGroundColor = Color.Gray;
		}

		private void testMethod() {
			Debug.WriteLine("Pressed Button!");
		}
	}
}
