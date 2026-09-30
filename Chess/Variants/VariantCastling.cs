using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;

namespace DChess.Chess.Variants {
    /// <summary>
    /// Castling. Detection is position-based, not history-based: castling is
    /// allowed whenever the king sits on its home square, a same-team rook
    /// sits on the corner, and every square between them is empty.
    ///
    /// This is intentional — the engine supports UndoLastMove and we want
    /// "move king, undo, castle" to actually work. A history-based "king
    /// has ever moved" flag would survive the undo and forbid castling
    /// from a position where the king hasn't moved on the live board.
    ///
    /// Note: there is no check / pass-through-check rule because the engine
    /// has no check detection at all (game ends only on king capture).
    /// </summary>
    public class VariantCastling : Variant {

        public override List<Move> AdditionalMoves(Board board, Piece piece, Vector2Int position) {
            if (piece.Type != PieceType.King) return null;

            int homeY = piece.Team == TeamType.White ? 0 : board.Size.y - 1;
            int kingHomeX = board.Size.x - 4; // matches BoardManager.Build8x8StandardBoard
            if (position.y != homeY || position.x != kingHomeX) return null;

            var moves = new List<Move>();
            // Kingside: rook on the right edge.
            tryAddCastle(board, piece, position, homeY, board.Size.x - 1, kingHomeX + 2, kingHomeX + 1, moves);
            // Queenside: rook on x=0.
            tryAddCastle(board, piece, position, homeY,                0, kingHomeX - 2, kingHomeX - 1, moves);
            return moves;
        }

        private static void tryAddCastle(Board board, Piece king, Vector2Int kingPos, int homeY,
                                         int rookX, int kingDestX, int rookDestX, List<Move> outMoves) {
            var rookPos = new Vector2Int(rookX, homeY);
            if (!board.IsValidPosition(rookPos)) return;

            var rook = board.GetPiece(rookPos);
            if (rook == Piece.NULL_PIECE) return;
            if (rook.Type != PieceType.Rook) return;
            if (rook.Team != king.Team) return;

            // Every square strictly between king and rook must be empty.
            int min = Math.Min(kingPos.x, rookX) + 1;
            int max = Math.Max(kingPos.x, rookX) - 1;
            for (int x = min; x <= max; x++) {
                if (board.GetPiece(new Vector2Int(x, homeY)) != Piece.NULL_PIECE) return;
            }

            var move = new Move();
            // Order matters for Move.Apply when source == dest of another change,
            // but here all four squares are distinct, so any order is fine.
            move.AddChange(kingPos,                              king,            Piece.NULL_PIECE);
            move.AddChange(new Vector2Int(kingDestX, homeY),     Piece.NULL_PIECE, king);
            move.AddChange(rookPos,                              rook,            Piece.NULL_PIECE);
            move.AddChange(new Vector2Int(rookDestX, homeY),     Piece.NULL_PIECE, rook);
            outMoves.Add(move);
        }
    }
}
