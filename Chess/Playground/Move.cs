using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Xml;

namespace DChess.Chess.Playground
{
	public class Move
	{
		public readonly List<BoardChange> Changes = new();

		public Move()
		{

		}

		public void AddChange(BoardChange change)
		{
			Changes.Add(change);
		}

		public void AddChange(Vector2Int position, Piece oldPiece, Piece newPiece)
		{
			Changes.Add(new BoardChange(position, oldPiece, newPiece));
		}

		public void Apply(Board board)
		{
			foreach (var change in Changes)
			{
				board.PlacePiece(change.boardPosition, change.newPiece);
				if (change.oldPiece == Piece.NULL_PIECE)
				{
					change.newPiece.MoveCount += 1;
				}
			}
		}

		public void Undo(Board board)
		{
			foreach (var change in Changes)
			{
				board.PlacePiece(change.boardPosition, change.oldPiece);

                if (change.newPiece == Piece.NULL_PIECE)
                {
                    change.oldPiece.MoveCount -= 1;
                }
            }
		}

		public float AttackScore()
		{
			float maxAttack = 0;
			foreach (var change in Changes)
			{
				if (change.newPiece != Piece.NULL_PIECE && change.oldPiece != Piece.NULL_PIECE)
				{
					float pieceScore = change.oldPiece.GetPieceScore();
					if (pieceScore < maxAttack) continue;

					maxAttack = pieceScore;
				}
			}
			return maxAttack;
		}

		public override bool Equals(object obj)
		{
			if (obj is not Move)
			{
				return false;
			}

			Move move = obj as Move;
			foreach (var change in Changes) { 
				if (!move.Changes.Contains(change))
				{
					return false;
				}
			}
			return true;
		}

		public override string ToString()
		{
			BoardChange boardChange1 = Changes[0];
			BoardChange boardChange2 = Changes[1];

			return $"Move: from {boardChange1.boardPosition}, to {boardChange2.boardPosition} with {Changes[0].oldPiece}";
		}
	}
}
