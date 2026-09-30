using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class CaptureTests {

        [TestMethod]
        public void White_pawn_captures_black_pawn_diagonally() {
            var b = EmptyBoard();
            var whitePawn = Place(b, 4, 3, PieceType.Pawn, TeamType.White);
            Place(b, 3, 4, PieceType.Pawn, TeamType.Black);

            var move = FindMove(b, new Vector2Int(4, 3), new Vector2Int(3, 4));
            Assert.IsNotNull(move, "diagonal capture should be a legal pawn move");
            b.MakeMove(move);

            Assert.AreEqual(Piece.NULL_PIECE, b.GetPiece(new Vector2Int(4, 3)), "source must be empty");
            var landed = b.GetPiece(new Vector2Int(3, 4));
            Assert.AreEqual(TeamType.White,  landed.Team);
            Assert.AreEqual(PieceType.Pawn,  landed.Type);
            Assert.AreSame(whitePawn, landed, "the white pawn instance moved to d5");
        }

        [TestMethod]
        public void Undoing_capture_restores_both_pieces() {
            var b = EmptyBoard();
            Place(b, 4, 3, PieceType.Pawn, TeamType.White);
            Place(b, 3, 4, PieceType.Pawn, TeamType.Black);

            var move = FindMove(b, new Vector2Int(4, 3), new Vector2Int(3, 4));
            b.MakeMove(move);
            b.UndoLastMove();

            Assert.AreEqual(TeamType.White, b.GetPiece(new Vector2Int(4, 3)).Team);
            Assert.AreEqual(TeamType.Black, b.GetPiece(new Vector2Int(3, 4)).Team);
            Assert.AreEqual(0, b.GetMoveCount(), "history empty after undo");
            Assert.IsTrue(b.IsWhitesTurn,         "white to move again after undo");
        }

        [TestMethod]
        public void Capture_can_be_replayed_after_undo() {
            var b = EmptyBoard();
            Place(b, 4, 3, PieceType.Pawn, TeamType.White);
            Place(b, 3, 4, PieceType.Pawn, TeamType.Black);

            b.MakeMove(FindMove(b, new Vector2Int(4, 3), new Vector2Int(3, 4)));
            b.UndoLastMove();

            var redo = FindMove(b, new Vector2Int(4, 3), new Vector2Int(3, 4));
            Assert.IsNotNull(redo, "capture must be re-discoverable after undo");
            b.MakeMove(redo);
            Assert.AreEqual(TeamType.White, b.GetPiece(new Vector2Int(3, 4)).Team);
        }
    }
}
