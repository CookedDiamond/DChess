using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.UI {
	public class ButtonManager {

		private readonly List<Button> _buttons = new();

		public void AddButton(Button button) {
			_buttons.Add(button);
		}

		public void OnClick(Vector2Int mousePosition) {
			foreach (Button button in _buttons) {
				button.TriggerButton(mousePosition);
			}
		}

		public void OnHover(Vector2Int mousePosition) {
			foreach (Button button in _buttons) { 
				button.HoverButton(mousePosition);
			}
		}
	}
}
