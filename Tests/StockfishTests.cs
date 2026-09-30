using DChess.UI.Analysis;
using DChess.Chess.Arena;
using DChess.Chess.Playground;
using DChess.BotApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DChess.Tests;

[TestClass]
public class StockfishTests {
    [TestMethod]
    public void StandardBoardFenAndReplayAgree() {
        var board = BoardSetup.CreateStandardBoard();
        var expected = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        Assert.AreEqual(expected, AnalysisPosition.FromBoard(board).Fen);
        var record = new GameRecord(1, "white", "black", 0);
        record.AddPosition(new PositionSnapshot(board, null, null, 0));
        Assert.AreEqual(expected, AnalysisPosition.FromGame(record, 0).Fen);
        var move = board.GetPiece(new(4, 1)).GetMove(new(4, 1), new(4, 3));
        board.MakeMove(move);
        record.AddPosition(new PositionSnapshot(board, board.GetLastMove(), "e4", 0));
        Assert.AreEqual(AnalysisPosition.FromBoard(board).Fen, AnalysisPosition.FromGame(record, 1).Fen);
    }

    [TestMethod]
    public void InvalidPositionIsNotSentToEngine() {
        var board = BoardSetup.CreateStandardBoard();
        board.RemovePiece(new(4, 7));
        Assert.IsNull(AnalysisPosition.FromBoard(board).Fen);
        board = BoardSetup.CreateStandardBoard();
        board.RemoveSquare(new(0, 0));
        Assert.IsNull(AnalysisPosition.FromBoard(board).Fen);
    }

    [TestMethod]
    public void ScoresUseWhitePerspectiveAndIgnoreBounds() {
        Assert.AreEqual(-125, EvaluationScore.Parse("info depth 9 score cp 125 pv e7e5", false).Value);
        Assert.AreEqual("-M3", EvaluationScore.Parse("info score mate 3", false).Label);
        Assert.AreEqual(0.0, EvaluationScore.Parse("info score mate 0", true).WhiteFraction);
        Assert.AreEqual(1.0, EvaluationScore.Parse("info score mate 0", false).WhiteFraction);
        Assert.IsNull(EvaluationScore.Parse("info score cp 200 lowerbound", true));
        Assert.IsNull(EvaluationScore.Parse("info string no score", true));
        Assert.IsTrue(new EvaluationScore(100, false).WhiteFraction > .5);
    }

    [TestMethod]
    public void StockfishIsNotExportedToBots() {
        Assert.IsFalse(typeof(StockfishEvaluation).IsPublic);
        Assert.IsFalse(typeof(EvaluationScore).IsPublic);
        foreach (var type in new[] { typeof(BotBoard), typeof(BotTimer), typeof(Board), typeof(PositionSnapshot) }) {
            Assert.IsFalse(type.GetMembers().Any(m => m.Name.Contains("Stockfish", StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(type.GetProperties().Any(p => p.PropertyType == typeof(EvaluationScore)));
        }
    }

    [TestMethod]
    public async Task RealStockfishEvaluatesAndCancels() {
        if (!File.Exists(StockfishEvaluation.EnginePath)) Assert.Inconclusive("Run scripts/Install-Stockfish.ps1 and rebuild for engine integration test.");
        var fen = AnalysisPosition.FromBoard(BoardSetup.CreateStandardBoard()).Fen;
        var score = await StockfishEvaluation.EvaluateAsync(fen, CancellationToken.None);
        Assert.IsNotNull(score);
        Assert.IsFalse(score.IsMate);
        var advantage = BoardSetup.CreateStandardBoard();
        advantage.RemovePiece(new(3, 7));
        var winningFen = AnalysisPosition.FromBoard(advantage).Fen;
        var white = await StockfishEvaluation.EvaluateAsync(winningFen, CancellationToken.None);
        var black = await StockfishEvaluation.EvaluateAsync(winningFen.Replace(" w ", " b "), CancellationToken.None);
        Assert.IsTrue(white.Value > 300);
        Assert.IsTrue(black.Value > 300, "Scores must favor White even when Black is to move.");
        using var cancel = new CancellationTokenSource();
        var task = StockfishEvaluation.EvaluateAsync(fen, cancel.Token);
        cancel.CancelAfter(200);
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () => await task);
    }

    [TestMethod]
    public async Task PositionChangeDiscardsOldEvaluation() {
        using var evaluation = new StockfishEvaluation();
        evaluation.Update(AnalysisPosition.FromBoard(BoardSetup.CreateStandardBoard()));
        evaluation.Update(new AnalysisPosition(null, "Unsupported board"));
        await Task.Delay(600);
        evaluation.Update(new AnalysisPosition(null, "Unsupported board"));
        Assert.IsNull(evaluation.Score);
        Assert.AreEqual("Unsupported board", evaluation.Status);
    }
}
