using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DChess.Chess.Playground {
	public class BoardChange {
		public readonly Vector2Int boardPosition;
		public readonly Piece oldPiece;
		public readonly Piece newPiece;
		public readonly SquareType? oldSquare;
		public readonly SquareType? newSquare;

		public BoardChange(Vector2Int boardPosition, Piece oldPiece, Piece newPiece,
			SquareType? oldSquare = null, SquareType? newSquare = null)
		{
			this.oldPiece = oldPiece;
			this.newPiece = newPiece;
			this.boardPosition = boardPosition;
			this.oldSquare = oldSquare;
			this.newSquare = newSquare;
		}

		public override bool Equals(object obj)
		{
			if (obj is not BoardChange) return false;
			BoardChange bc = (BoardChange)obj;
			return (boardPosition == bc.boardPosition
				&& oldPiece == bc.oldPiece
				&& newPiece == bc.newPiece && oldSquare == bc.oldSquare && newSquare == bc.newSquare);
		}

		public override int GetHashCode() => HashCode.Combine(boardPosition, oldPiece, newPiece, oldSquare, newSquare);
	}
}
