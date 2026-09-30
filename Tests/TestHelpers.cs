using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;

namespace DChess.Tests {
    /// <summary>Shared setup helpers across the engine tests.</summary>
    internal static class TestHelpers {

        public static Board EmptyBoard(int size = 8) {
            return new Board(new Vector2Int(size, size));
        }

        public static Piece Place(Board b, int x, int y, PieceType type, TeamType team) {
            var piece = Piece.GetPieceFromType(type, team, b);
            b.PlacePiece(new Vector2Int(x, y), piece);
            return piece;
        }

        /// <summary>Find the legal Move that starts at <paramref name="from"/> and lands on
        /// <paramref name="to"/>, or null if there is none. Identifies origin/destination by
        /// inspecting the Move's BoardChanges (origin = change clearing a square; destination =
        /// change placing the moving piece). Multi-change moves like castling work too.</summary>
        public static Move FindMove(Board b, Vector2Int from, Vector2Int to) {
            var piece = b.GetPiece(from);
            if (piece == Piece.NULL_PIECE) return null;
            foreach (var m in piece.GetAllLegalMoves(from)) {
                bool gotOrigin = false, gotDest = false;
                foreach (var c in m.Changes) {
                    if (!gotOrigin && c.newPiece == Piece.NULL_PIECE && c.boardPosition == from) gotOrigin = true;
                    if (!gotDest   && c.newPiece != Piece.NULL_PIECE && c.boardPosition == to)   gotDest = true;
                }
                if (gotOrigin && gotDest) return m;
            }
            return null;
        }

        public static bool AnyChangeAt(Move m, int x, int y) {
            var v = new Vector2Int(x, y);
            foreach (var c in m.Changes) if (c.boardPosition == v) return true;
            return false;
        }

        /// <summary>Standard castling test setup: empty board, both white rooks on
        /// corners, white king on its home square, lone black king tucked away so
        /// HasTeamWon() stays None.</summary>
        public static Board CastleReadyBoard(int size = 8, bool addCastling = true) {
            var b = EmptyBoard(size);
            if (addCastling) b.Variants.Add(new DChess.Chess.Variants.VariantCastling());

            int homeY = 0;
            int kingHomeX = size - 4;
            Place(b, kingHomeX, homeY, PieceType.King, TeamType.White);
            Place(b, 0,         homeY, PieceType.Rook, TeamType.White);
            Place(b, size - 1,  homeY, PieceType.Rook, TeamType.White);
            Place(b, kingHomeX, size - 1, PieceType.King, TeamType.Black);
            return b;
        }
    }
}
