using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess {
	public class Move {
		public Vector2Int origin {get; private set;}
		public Vector2Int destination { get; private set; }

		public Move(Vector2Int origin, Vector2Int destination) {
			this.origin = origin;
			this.destination = destination;
		}

		public override string ToString() {
			return $"Move: from {origin}, to {destination}";
		}

		public override bool Equals(object obj) {
			if (obj is not Move) return false;
			Move other = (Move)obj;
			return other.destination == destination && other.origin == origin;
		}

		// Probably some bullshit lol idk.
		public override int GetHashCode() {
			return HashCode.Combine(origin, destination);
		}
	}
}
