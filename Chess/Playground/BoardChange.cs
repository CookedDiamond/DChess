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

		public BoardChange(Vector2Int boardPosition, Piece oldPiece, Piece newPiece)
		{
			this.oldPiece = oldPiece;
			this.newPiece = newPiece;
			this.boardPosition = boardPosition;
		}

		public override bool Equals(object obj)
		{
			if (obj is not BoardChange) return false; 
			BoardChange bc = (BoardChange)obj;
			return (boardPosition == bc.boardPosition
				&& oldPiece == bc.oldPiece
				&& newPiece == bc.newPiece);
		}
	}
}
