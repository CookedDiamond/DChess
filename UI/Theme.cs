using Microsoft.Xna.Framework;

namespace DChess.UI {
	/// <summary>
	/// Colors shared by the menu and arena scenes.
	/// </summary>
	public static class Theme {
		public static readonly Color Background = new(33, 37, 43);
		public static readonly Color Panel = new(44, 49, 57);
		public static readonly Color PanelLight = new(58, 64, 74);
		public static readonly Color Button = new(70, 78, 90);
		public static readonly Color ButtonHover = new(92, 102, 118);
		public static readonly Color Accent = new(110, 150, 70);
		public static readonly Color AccentHover = new(132, 174, 90);
		public static readonly Color Text = new(232, 234, 237);
		public static readonly Color TextDim = new(150, 157, 168);
		public static readonly Color Warning = new(235, 190, 90);
		public static readonly Color Good = new(150, 205, 110);
		public static readonly Color Bad = new(235, 110, 100);

		public static readonly Color LightSquare = new(242, 225, 195);
		public static readonly Color DarkSquare = new(195, 160, 130);
		public static readonly Color LastMove = new Color(215, 220, 90) * 0.55f;
		public static readonly Color Selected = new Color(90, 170, 255) * 0.55f;
		public static readonly Color MoveDot = new Color(40, 40, 40) * 0.35f;
	}
}
