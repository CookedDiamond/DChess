using DChess.Chess.Pieces;
using DChess.Util;
using System;
using System.Collections.Generic;
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
            }
        }

        public void Undo(Board board)
        {
            foreach (var change in Changes)
            {
                board.PlacePiece(change.boardPosition, change.oldPiece);
            }
        }

        public float IsAttack()
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

        public override string ToString()
        {
            return $"Move: from {1}, to {1}";
        }
    }
}
