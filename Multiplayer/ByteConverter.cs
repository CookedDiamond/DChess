using DChess.Chess.Playground;
using DChess.Util;
using DChess.Chess.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Multiplayer
{
    public class ByteConverter {
		public const int MOVE_LENGTH = 16;
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
			var origin = move.Changes.First(c => c.oldPiece != Piece.NULL_PIECE && c.newPiece == Piece.NULL_PIECE);
			var destination = move.Changes.First(c => c.newPiece == origin.oldPiece);
			return ToBytes(origin.boardPosition).Concat(ToBytes(destination.boardPosition)).ToArray();
		}

		public static Move ToMove(byte[] bytes, Board board) {
			if (bytes.Length != MOVE_LENGTH) throw new ArgumentException("A move must contain exactly 16 bytes.", nameof(bytes));
			var origin = ToVector2Int(bytes.Take(INT_LENGTH * 2).ToArray());
			var destination = ToVector2Int(bytes.Skip(INT_LENGTH * 2).Take(INT_LENGTH * 2).ToArray());

			var piece = board.GetPiece(origin);
			if (piece == Piece.NULL_PIECE || piece.Team != board.GetTurnTeamType()) return null;
			return piece.GetMove(origin, destination);
		}
	}
}
