using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class CompositeTests {

        [TestMethod]
        public void Knight_capture_can_be_undone_then_an_alternate_capture_played() {
            var b = EmptyBoard();
            Place(b, 0, 0, PieceType.King,   TeamType.White);
            Place(b, 7, 7, PieceType.King,   TeamType.Black);
            Place(b, 4, 3, PieceType.Knight, TeamType.White); // e4
            Place(b, 5, 5, PieceType.Pawn,   TeamType.Black); // f6
            Place(b, 3, 5, PieceType.Pawn,   TeamType.Black); // d6

            var capF6 = FindMove(b, new Vector2Int(4, 3), new Vector2Int(5, 5));
            Assert.IsNotNull(capF6);
            b.MakeMove(capF6);
            Assert.AreEqual(PieceType.Knight, b.GetPiece(new Vector2Int(5, 5)).Type);

            b.UndoLastMove();
            Assert.AreEqual(PieceType.Pawn, b.GetPiece(new Vector2Int(5, 5)).Type);
            Assert.AreEqual(TeamType.Black, b.GetPiece(new Vector2Int(5, 5)).Team);

            var capD6 = FindMove(b, new Vector2Int(4, 3), new Vector2Int(3, 5));
            Assert.IsNotNull(capD6, "alternate capture remains legal after undo");
            b.MakeMove(capD6);
            Assert.AreEqual(PieceType.Knight, b.GetPiece(new Vector2Int(3, 5)).Type);
            Assert.AreEqual(PieceType.Pawn,   b.GetPiece(new Vector2Int(5, 5)).Type);
        }
    }
}
