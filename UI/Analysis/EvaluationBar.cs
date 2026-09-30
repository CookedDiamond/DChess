using DChess.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DChess.UI.Analysis;

internal static class EvaluationBar {
    internal static void Draw(SpriteBatch batch, Rectangle board, bool flipped, StockfishEvaluation evaluation, bool statusBelow = true) {
        int width = Math.Max(20, Math.Min(32, board.Height / 18));
        var bar = new Rectangle(board.X - width - 8, board.Y, width, board.Height);
        batch.DrawRectangle(bar, new Color(40, 40, 40));
        double fraction = evaluation.Score?.WhiteFraction ?? .5;
        int whiteHeight = (int)(bar.Height * fraction);
        batch.DrawRectangle(new Rectangle(bar.X, flipped ? bar.Y : bar.Bottom - whiteHeight, bar.Width, whiteHeight), new Color(235, 235, 235));
        batch.DrawRectangleOutline(bar, new Color(110, 110, 110), 1);
        var label = new Rectangle(bar.X, bar.Center.Y - 12, bar.Width, 24);
        batch.DrawRectangle(label, Color.DarkSlateGray);
        batch.DrawTextCentered(evaluation.Score?.Label ?? (evaluation.Status.Contains("calculating") ? "..." : "N/A"), label, 12, Color.White);
        batch.DrawTextLine(evaluation.Status, new Vector2(board.X, statusBelow ? board.Bottom + 2 : board.Y - 20), Math.Max(10, board.Height / 45f), Color.LightGray);
    }
}
