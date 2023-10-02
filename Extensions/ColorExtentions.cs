using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Extensions {
	public static class ColorExtentions {
		public static Color DivideColor(this Color color, float factor) {
			float r = color.R / factor / 256;
			float g = color.G / factor / 256;
			float b = color.B / factor / 256;
			return new Color(r,g,b);
		}

		public static Color AverageColor(this Color color1, Color color2) {
			return new Color((color1.R + color2.R) / 2, (color1.G + color2.G) / 2, (color1.B + color2.B) / 2);
		}
	}
}
