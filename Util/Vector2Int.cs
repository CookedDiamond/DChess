using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Util {
	public struct Vector2Int {
		public int x { get; set; }
		public int y { get; set; }

		public Vector2Int(int x, int y) {
			this.x = x; 
			this.y = y;
		}

		public static Vector2Int operator +(Vector2Int a)
			=> a;

		public static Vector2Int operator -(Vector2Int a)
			=> new Vector2Int(-a.x, -a.y);

		public static Vector2Int operator +(Vector2Int a, Vector2Int b)
			=> new Vector2Int(a.x + b.x, a.y + b.y);
		public static Vector2Int operator -(Vector2Int a, Vector2Int b)
			=> new Vector2Int(a.x - b.x, a.y - b.y);

		public static Vector2Int operator *(Vector2Int a, int x)
			=> new Vector2Int(a.x * x, a.y * x);

		public static bool operator ==(Vector2Int a, Vector2Int b)
			=> a.x == b.x && a.y == b.y;

		public static bool operator !=(Vector2Int a, Vector2Int b)
			=> a.x != b.x || a.y != b.y;

		public override string ToString() {
			return $"x: {x} y: {y}";
		}
	}
}
