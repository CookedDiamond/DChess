using DChess.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI.Scenes {
	public class SceneMenu : Scene {

		public SceneMenu(ButtonManager buttonManager) {

			ButtonText textButton = new("test", WindowAlignment.Center, new Vector2Int(100, 100));
			textButton.OnClickEvent += () => testMethod();
			buttonManager.AddButton(textButton);
			content.Add(textButton);
		}

		private void testMethod() {
			Debug.WriteLine("Pressed Button!");
		}
	}
}
