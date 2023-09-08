using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI {
	public class ButtonManager {

		private List<Button> buttons = new List<Button>();

		public ButtonManager() {

		}

		public void AddButton(Button button) {
			buttons.Add(button);
		}

		public void OnClick(Vector2Int mousePosition) {
			foreach (Button button in buttons) {
				button.TriggerButton(mousePosition);
			}
		}
	}
}
