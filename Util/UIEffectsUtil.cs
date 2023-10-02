using DChess.Extensions;
using DChess.UI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Util {
	public static class UIEffectsUtil {


		public static void HighlightBlend(Color originalColor, Func<Color, bool> changeColor, int steps = 20, int waitTime = 16, float colorDivide = 2) {
			Color fullHigh = originalColor.DivideColor(colorDivide);
			for (int i = 0; i < steps; i++) {
				Task.Delay(waitTime).Wait();
				float factor = (1 + (i * 1f / steps)) / 256;
				var current = new Color(fullHigh.R * factor, fullHigh.G * factor, fullHigh.B * factor);
				changeColor(current);
			}
			changeColor(originalColor);
		}
	}
}
