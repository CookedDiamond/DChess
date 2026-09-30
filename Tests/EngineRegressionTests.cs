using System.Linq;
using System.Threading;
using DChess.BotApi;
using DChess.Bots;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Cli;
using DChess.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DChess.Tests.TestHelpers;

namespace DChess.Tests {
    [TestClass]
    public class EngineRegressionTests {
        [TestMethod]
        public void Empty_square_moves_and_undo_on_an_empty_history_are_safe() {
            var board = EmptyBoard();
            board.UndoLastMove();
            Assert.AreEqual(0, board.GetMoveCount());
            Assert.AreEqual(0, board.GetPiece(new(0, 0)).GetAllLegalMoves(new(0, 0)).Count);
            Assert.IsNull(board.GetPiece(new(0, 0)).GetMove(new(0, 0), new(0, 1)));
        }

        [TestMethod]
        public void Moving_and_undoing_keep_piece_count_accurate() {
            var board = CastleReadyBoard();
            int count = board.GetTotalPieceCount();
            board.MakeMove(FindMove(board, new(7, 0), new(7, 3)));
            Assert.AreEqual(count, board.GetTotalPieceCount());
            board.UndoLastMove();
            Assert.AreEqual(count, board.GetTotalPieceCount());
        }

        [TestMethod]
        public void Capture_increments_move_count_and_undo_restores_it() {
            var board = CastleReadyBoard();
            var rook = board.GetPiece(new(7, 0));
            Place(board, 7, 3, PieceType.Pawn, TeamType.Black);
            board.MakeMove(FindMove(board, new(7, 0), new(7, 3)));
            Assert.AreEqual(1, rook.MoveCount);
            board.UndoLastMove();
            Assert.AreEqual(0, rook.MoveCount);
            Assert.AreEqual(PieceType.Pawn, board.GetPiece(new(7, 3)).Type);
        }

        [TestMethod]
        public void Cloned_pieces_generate_moves_using_the_cloned_position() {
            var board = CastleReadyBoard();
            var clone = board.CloneBoard();
            Place(clone, 7, 2, PieceType.Pawn, TeamType.White);
            Assert.IsNull(FindMove(clone, new(7, 0), new(7, 3)));
            Assert.IsNotNull(FindMove(board, new(7, 0), new(7, 3)));
        }

        [TestMethod]
        public void Applying_an_original_move_to_a_clone_preserves_original_pieces() {
            var board = CastleReadyBoard();
            var rook = board.GetPiece(new(7, 0));
            var clone = board.CloneBoard();
            clone.MakeMove(FindMove(board, new(7, 0), new(7, 3)));
            Assert.AreEqual(0, rook.MoveCount);
            Assert.AreNotSame(rook, clone.GetPiece(new(7, 3)));
            Assert.AreEqual(1, clone.GetPiece(new(7, 3)).MoveCount);
            Place(clone, 7, 4, PieceType.Pawn, TeamType.White);
            Assert.IsNull(FindMove(clone, new(7, 3), new(7, 5)));
        }

        [TestMethod]
        public void Undoing_cloned_history_does_not_mutate_original_pieces() {
            var board = CastleReadyBoard();
            board.MakeMove(FindMove(board, new(7, 0), new(7, 3)));
            var clone = board.CloneBoard();
            clone.UndoLastMove();
            Assert.AreEqual(1, board.GetPiece(new(7, 3)).MoveCount);
            Assert.AreEqual(0, clone.GetPiece(new(7, 0)).MoveCount);
            Assert.AreEqual(1, board.GetMoveCount());
            Assert.AreEqual(0, clone.GetMoveCount());
        }

        [TestMethod]
        public void Castling_is_forbidden_after_king_returns_home_without_undo() {
            var board = CastleReadyBoard();
            board.MakeMove(FindMove(board, new(4, 0), new(4, 1)));
            board.MakeMove(FindMove(board, new(4, 1), new(4, 0)));
            Assert.IsFalse(board.GetPiece(new(4, 0)).GetAllLegalMoves(new(4, 0)).Any(m => m.Changes.Count == 4));
        }

        [TestMethod]
        public void Castling_cannot_cross_a_disabled_square_or_select_the_rook_square() {
            var board = CastleReadyBoard();
            var king = board.GetPiece(new(4, 0));
            Assert.IsNull(king.GetMove(new(4, 0), new(7, 0)));
            board.RemoveSquare(new(5, 0));
            Assert.IsFalse(king.GetAllLegalMoves(new(4, 0)).Any(m => m.Changes.Count == 4 && AnyChangeAt(m, 7, 0)));
        }

        [TestMethod]
        public void Battle_royale_undo_restores_squares_and_captured_edge_pieces() {
            var board = CastleReadyBoard();
            board.Variants.Add(new VariantBattleRoyale(1, 1));
            board.MakeMove(FindMove(board, new(4, 0), new(4, 1)));
            Assert.IsFalse(board.IsValidPosition(new(0, 0)));
            board.UndoLastMove();
            Assert.IsTrue(board.IsValidPosition(new(0, 0)));
            Assert.AreEqual(PieceType.Rook, board.GetPiece(new(0, 0)).Type);
            board.MakeMove(FindMove(board, new(4, 0), new(4, 1)));
            Assert.IsFalse(board.IsValidPosition(new(0, 0)));
            Assert.IsTrue(board.IsValidPosition(new(1, 1)));
        }

        [TestMethod]
        public void AI_with_no_moves_returns_null_and_does_not_change_the_board() {
            var board = EmptyBoard();
            Place(board, 0, 0, PieceType.King, TeamType.White);
            Place(board, 7, 7, PieceType.King, TeamType.Black);
            board.RemoveSquare(new(0, 1));
            board.RemoveSquare(new(1, 0));
            board.RemoveSquare(new(1, 1));
            var result = BotRunner.RequestMove(new MinMaxBot(), board, 200, CancellationToken.None);
            Assert.IsNull(result.Move);
            new BoardManager(board, new BoardNetworking()).MakeComputerMove(false);
            Assert.AreEqual(0, board.GetMoveCount());
        }

        [TestMethod]
        public void AI_search_preserves_live_board_and_can_be_reused() {
            var board = EmptyBoard();
            new BoardManager(board, new BoardNetworking()).Build8x8StandardBoard();
            var ai = new MinMaxBot();
            var first = BotRunner.RequestMove(ai, board, 200, CancellationToken.None).Move;
            Assert.IsNotNull(first);
            Assert.AreEqual(0, board.GetMoveCount());
            Assert.AreEqual(32, board.GetTotalPieceCount());
            Assert.IsTrue(board.Pieces.Values.All(p => p.MoveCount == 0));
            board.MakeMove(first);
            var second = BotRunner.RequestMove(ai, board, 200, CancellationToken.None).Move;
            Assert.IsNotNull(second);
            Assert.IsTrue(second.Changes.Any(c => c.newPiece.Team == TeamType.Black));
            Assert.AreEqual(1, board.GetMoveCount());
        }

        [TestMethod]
        [DataRow("--size")]
        [DataRow("--invalid")]
        [DataRow("--variant", "unknown")]
        [DataRow("--size", "-1")]
        public void Invalid_CLI_options_return_failure(params string[] options) {
            Assert.AreEqual(1, CliRunner.Run(options));
        }
    }
}
