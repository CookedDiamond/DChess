using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class WinTests {

        [TestMethod]
        public void Capturing_enemy_king_wins_the_game() {
            var b = EmptyBoard();
            Place(b, 0, 0, PieceType.King, TeamType.White);
            Place(b, 1, 0, PieceType.Rook, TeamType.White);
            Place(b, 7, 0, PieceType.King, TeamType.Black);
            Assert.AreEqual(TeamType.None, b.HasTeamWon(), "no winner yet");

            var capture = FindMove(b, new Vector2Int(1, 0), new Vector2Int(7, 0));
            Assert.IsNotNull(capture, "white rook can take black king down the rank");
            b.MakeMove(capture);

            Assert.AreEqual(TeamType.White, b.HasTeamWon());
        }

        [TestMethod]
        public void Undoing_the_winning_move_clears_the_winner() {
            var b = EmptyBoard();
            Place(b, 0, 0, PieceType.King, TeamType.White);
            Place(b, 1, 0, PieceType.Rook, TeamType.White);
            Place(b, 7, 0, PieceType.King, TeamType.Black);

            b.MakeMove(FindMove(b, new Vector2Int(1, 0), new Vector2Int(7, 0)));
            Assert.AreEqual(TeamType.White, b.HasTeamWon());

            b.UndoLastMove();
            Assert.AreEqual(TeamType.None, b.HasTeamWon(),                  "win cleared after undo");
            Assert.AreEqual(PieceType.King, b.GetPiece(new Vector2Int(7, 0)).Type, "black king restored");
        }
    }
}
