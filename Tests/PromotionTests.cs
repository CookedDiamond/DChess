using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class PromotionTests {

        [TestMethod]
        public void Pawn_reaching_last_rank_becomes_a_queen() {
            var b = EmptyBoard();
            b.Variants.Add(new VariantPawnQueenPromotion());
            Place(b, 0, 0, PieceType.King, TeamType.White);
            Place(b, 7, 7, PieceType.King, TeamType.Black);
            Place(b, 4, 6, PieceType.Pawn, TeamType.White);

            b.MakeMove(FindMove(b, new Vector2Int(4, 6), new Vector2Int(4, 7)));

            var landed = b.GetPiece(new Vector2Int(4, 7));
            Assert.AreEqual(PieceType.Queen, landed.Type);
            Assert.AreEqual(TeamType.White,  landed.Team);
        }

        [TestMethod]
        public void Undo_reverts_promotion_back_to_pawn() {
            var b = EmptyBoard();
            b.Variants.Add(new VariantPawnQueenPromotion());
            Place(b, 0, 0, PieceType.King, TeamType.White);
            Place(b, 7, 7, PieceType.King, TeamType.Black);
            Place(b, 4, 6, PieceType.Pawn, TeamType.White);

            b.MakeMove(FindMove(b, new Vector2Int(4, 6), new Vector2Int(4, 7)));
            b.UndoLastMove();

            Assert.AreEqual(PieceType.Pawn,  b.GetPiece(new Vector2Int(4, 6)).Type);
            Assert.AreEqual(TeamType.White,  b.GetPiece(new Vector2Int(4, 6)).Team);
            Assert.AreEqual(Piece.NULL_PIECE, b.GetPiece(new Vector2Int(4, 7)), "queen on the last rank is gone");
        }
    }
}
