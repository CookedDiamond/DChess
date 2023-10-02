using DChess.Chess;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Multiplayer {
	public class ByteConverter {
		public static readonly int INT_LENGTH = 4;

		public static byte[] ToBytes(int i) {
			return BitConverter.GetBytes(i);
		}

		public static int ToInt(byte[] bytes) {
			return BitConverter.ToInt32(bytes);
		}

		public static byte[] ToBytes(Vector2Int vector2int) {
			List<byte> bytes = new List<byte>();
			bytes.AddRange(ToBytes(vector2int.x));
			bytes.AddRange(ToBytes(vector2int.y));
			return bytes.ToArray();
		}

		public static Vector2Int ToVector2Int(byte[] bytes) {
			int x = ToInt(bytes.Take(INT_LENGTH).ToArray());
			int y = ToInt(bytes.Skip(INT_LENGTH).ToArray());
			return new Vector2Int(x, y);
		}

		public static byte[] ToBytes(Move move) {
			List<byte> result = new();
			result.AddRange(ToBytes(move.origin));
			result.AddRange(ToBytes(move.destination));
			return result.ToArray();
		}

		public static Move ToMove(byte[] bytes, Board board) {
			var origin = ToVector2Int(bytes.Take(INT_LENGTH * 2).ToArray());
			var destination = ToVector2Int(bytes.Skip(INT_LENGTH * 2).Take(INT_LENGTH * 2).ToArray());

			return new Move(board, origin, destination);
		}
	}
}
