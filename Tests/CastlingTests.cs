using System.Linq;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class CastlingTests {

        // A "castling move" is the only Move with 4 BoardChanges (king + rook,
        // each with a source-clear and destination-place change). Tests use that
        // shape as a fingerprint to identify candidate castles.

        [TestMethod]
        public void Kingside_castling_appears_in_kings_legal_moves() {
            var b = CastleReadyBoard();
            int kingHomeX = b.Size.x - 4;
            var moves = b.GetPiece(new Vector2Int(kingHomeX, 0))
                         .GetAllLegalMoves(new Vector2Int(kingHomeX, 0));
            Assert.IsTrue(moves.Any(m => m.Changes.Count == 4));
        }

        [TestMethod]
        public void Kingside_castling_relocates_both_pieces() {
            var b = CastleReadyBoard();
            int kingHomeX = b.Size.x - 4;
            int rookX = b.Size.x - 1;

            var move = b.GetPiece(new Vector2Int(kingHomeX, 0))
                        .GetAllLegalMoves(new Vector2Int(kingHomeX, 0))
                        .First(m => m.Changes.Count == 4 && AnyChangeAt(m, rookX, 0));
            b.MakeMove(move);

            Assert.AreEqual(Piece.NULL_PIECE, b.GetPiece(new Vector2Int(kingHomeX, 0)));
            Assert.AreEqual(Piece.NULL_PIECE, b.GetPiece(new Vector2Int(rookX,     0)));
            Assert.AreEqual(PieceType.King, b.GetPiece(new Vector2Int(kingHomeX + 2, 0)).Type);
            Assert.AreEqual(PieceType.Rook, b.GetPiece(new Vector2Int(kingHomeX + 1, 0)).Type);
        }

        [TestMethod]
        public void Queenside_castling_relocates_both_pieces() {
            var b = CastleReadyBoard();
            int kingHomeX = b.Size.x - 4;

            var move = b.GetPiece(new Vector2Int(kingHomeX, 0))
                        .GetAllLegalMoves(new Vector2Int(kingHomeX, 0))
                        .First(m => m.Changes.Count == 4 && AnyChangeAt(m, 0, 0));
            b.MakeMove(move);

            Assert.AreEqual(Piece.NULL_PIECE, b.GetPiece(new Vector2Int(0,             0)));
            Assert.AreEqual(Piece.NULL_PIECE, b.GetPiece(new Vector2Int(kingHomeX,     0)));
            Assert.AreEqual(PieceType.King, b.GetPiece(new Vector2Int(kingHomeX - 2, 0)).Type);
            Assert.AreEqual(PieceType.Rook, b.GetPiece(new Vector2Int(kingHomeX - 1, 0)).Type);
        }

        [TestMethod]
        public void Castling_is_blocked_by_a_piece_between_king_and_rook() {
            var b = CastleReadyBoard();
            int kingHomeX = b.Size.x - 4;
            Place(b, kingHomeX + 1, 0, PieceType.Knight, TeamType.White);

            var castles = b.GetPiece(new Vector2Int(kingHomeX, 0))
                           .GetAllLegalMoves(new Vector2Int(kingHomeX, 0))
                           .Where(m => m.Changes.Count == 4)
                           .ToList();
            Assert.IsTrue(castles.All(m => !AnyChangeAt(m, b.Size.x - 1, 0)),
                "kingside castle must be suppressed when path blocked");
            Assert.IsTrue(castles.Any(m => AnyChangeAt(m, 0, 0)),
                "queenside castle still available");
        }

        [TestMethod]
        public void Castling_is_unavailable_without_the_castling_variant() {
            var b = CastleReadyBoard(addCastling: false);
            int kingHomeX = b.Size.x - 4;
            var moves = b.GetPiece(new Vector2Int(kingHomeX, 0))
                         .GetAllLegalMoves(new Vector2Int(kingHomeX, 0));
            Assert.IsFalse(moves.Any(m => m.Changes.Count == 4));
        }

        [TestMethod]
        public void Undo_after_castling_restores_both_pieces_to_their_starting_squares() {
            var b = CastleReadyBoard();
            int kingHomeX = b.Size.x - 4;
            int rookX = b.Size.x - 1;

            var move = b.GetPiece(new Vector2Int(kingHomeX, 0))
                        .GetAllLegalMoves(new Vector2Int(kingHomeX, 0))
                        .First(m => m.Changes.Count == 4 && AnyChangeAt(m, rookX, 0));
            b.MakeMove(move);
            b.UndoLastMove();

            Assert.AreEqual(PieceType.King, b.GetPiece(new Vector2Int(kingHomeX, 0)).Type);
            Assert.AreEqual(PieceType.Rook, b.GetPiece(new Vector2Int(rookX,     0)).Type);
            Assert.AreEqual(Piece.NULL_PIECE, b.GetPiece(new Vector2Int(kingHomeX + 2, 0)));
            Assert.AreEqual(Piece.NULL_PIECE, b.GetPiece(new Vector2Int(kingHomeX + 1, 0)));
        }

        [TestMethod]
        public void Black_can_also_castle_kingside_on_its_back_rank() {
            var b = EmptyBoard();
            b.Variants.Add(new VariantCastling());
            int kingHomeX = b.Size.x - 4;
            int homeY = b.Size.y - 1;
            Place(b, kingHomeX,    homeY, PieceType.King, TeamType.Black);
            Place(b, b.Size.x - 1, homeY, PieceType.Rook, TeamType.Black);
            Place(b, 0, 0, PieceType.King, TeamType.White);

            var moves = b.GetPiece(new Vector2Int(kingHomeX, homeY))
                         .GetAllLegalMoves(new Vector2Int(kingHomeX, homeY));
            Assert.IsTrue(moves.Any(m => m.Changes.Count == 4 && AnyChangeAt(m, b.Size.x - 1, homeY)));
        }

        [TestMethod]
        public void Castling_is_restored_after_a_king_move_is_undone() {
            var b = CastleReadyBoard();
            int kingHomeX = b.Size.x - 4;

            var step = FindMove(b, new Vector2Int(kingHomeX, 0), new Vector2Int(kingHomeX + 1, 0));
            Assert.IsNotNull(step, "king should have a normal one-square move");
            b.MakeMove(step);

            // While king is off home, no castle.
            var afterMove = b.GetPiece(new Vector2Int(kingHomeX + 1, 0))
                             .GetAllLegalMoves(new Vector2Int(kingHomeX + 1, 0));
            Assert.IsFalse(afterMove.Any(m => m.Changes.Count == 4),
                "no castle while king is off home square");

            b.UndoLastMove();
            // After undo, castle is back.
            var afterUndo = b.GetPiece(new Vector2Int(kingHomeX, 0))
                             .GetAllLegalMoves(new Vector2Int(kingHomeX, 0));
            Assert.IsTrue(afterUndo.Any(m => m.Changes.Count == 4),
                "castling rights restored once king is back home via undo");
        }

        [TestMethod]
        public void Kingside_castling_is_restored_after_a_rook_move_is_undone() {
            var b = CastleReadyBoard();
            int kingHomeX = b.Size.x - 4;
            int rookX = b.Size.x - 1;

            b.MakeMove(FindMove(b, new Vector2Int(rookX, 0), new Vector2Int(rookX, 3)));

            var blocked = b.GetPiece(new Vector2Int(kingHomeX, 0))
                           .GetAllLegalMoves(new Vector2Int(kingHomeX, 0))
                           .Where(m => m.Changes.Count == 4);
            Assert.IsTrue(blocked.All(m => !AnyChangeAt(m, rookX, 0)),
                "kingside castle gone while rook is off corner");

            b.UndoLastMove();
            var restored = b.GetPiece(new Vector2Int(kingHomeX, 0))
                            .GetAllLegalMoves(new Vector2Int(kingHomeX, 0))
                            .Where(m => m.Changes.Count == 4);
            Assert.IsTrue(restored.Any(m => AnyChangeAt(m, rookX, 0)),
                "kingside castle returns after rook move undone");
        }

        [TestMethod]
        public void Castling_is_unavailable_while_the_king_has_actually_moved() {
            var b = CastleReadyBoard();
            int kingHomeX = b.Size.x - 4;

            b.MakeMove(FindMove(b, new Vector2Int(kingHomeX, 0), new Vector2Int(kingHomeX + 1, 0)));

            var moves = b.GetPiece(new Vector2Int(kingHomeX + 1, 0))
                         .GetAllLegalMoves(new Vector2Int(kingHomeX + 1, 0));
            Assert.IsFalse(moves.Any(m => m.Changes.Count == 4));
        }
    }
}
