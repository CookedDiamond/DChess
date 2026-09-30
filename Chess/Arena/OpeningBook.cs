using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Linq;

namespace DChess.Chess.Arena;

public sealed record OpeningLine(string Name, string[] Moves);

public static class OpeningBook {
    private static readonly OpeningLine[] Lines = {
        new("Italian game", new[] { "e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "f8c5" }),
        new("Ruy Lopez", new[] { "e2e4", "e7e5", "g1f3", "b8c6", "f1b5", "a7a6" }),
        new("Sicilian defence", new[] { "e2e4", "c7c5", "g1f3", "d7d6", "d2d4", "c5d4" }),
        new("French defence", new[] { "e2e4", "e7e6", "d2d4", "d7d5", "b1c3", "g8f6" }),
        new("Queen's gambit", new[] { "d2d4", "d7d5", "c2c4", "e7e6", "b1c3", "g8f6" }),
        new("King's Indian defence", new[] { "d2d4", "g8f6", "c2c4", "g7g6", "b1c3", "f8g7" }),
        new("Caro-Kann defence", new[] { "e2e4", "c7c6", "d2d4", "d7d5", "b1c3", "d5e4" }),
        new("Scotch game", new[] { "e2e4", "e7e5", "g1f3", "b8c6", "d2d4", "e5d4" })
    };
    public static OpeningLine Select(GameConfiguration configuration, int pairIndex, int seed) {
        return Select(configuration.CreateBoard(), pairIndex, seed);
    }
    public static OpeningLine Select(Board initial, int pairIndex, int seed) {
        if (pairIndex == 0) return new("Start position", Array.Empty<string>());
        var compatible = Lines.Where(line => IsPlayable(initial.CloneBoard(), line)).ToArray();
        if (compatible.Length > 0) {
            int offset = (int)((uint)seed % compatible.Length);
            return compatible[(offset + pairIndex - 1) % compatible.Length];
        }
        // A future variant may replace pieces or the initial position. Use a shared,
        // deterministic legal prefix when orthodox book lines do not apply.
        var board = initial.CloneBoard(); var random = new Random(seed ^ pairIndex);
        var moves = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 6 && !IsTerminal(board); i++) {
            var legal = board.GetAllLegalMovesForTeam(board.GetTurnTeamType());
            if (legal.Count == 0) break;
            var move = legal[random.Next(legal.Count)];
            moves.Add(move.From.ToSquareName() + move.To.ToSquareName()); board.MakeMove(move);
        }
        return moves.Count == 6 && !IsTerminal(board)
            ? new("Variant opening (no compatible book line)", moves.ToArray())
            : new("Start position (no playable book opening)", Array.Empty<string>());
    }
    private static bool IsPlayable(Board board, OpeningLine line) {
        foreach (string text in line.Moves) {
            var move = FindMove(board, text);
            if (move == null) return false;
            board.MakeMove(move); if (IsTerminal(board)) return false;
        }
        return true;
    }
    private static bool IsTerminal(Board board) => board.HasTeamWon() != TeamType.None ||
        board.Variants.Any(v => v.GetOutcome(board) != null) || !board.GetAllLegalMovesForTeam(board.GetTurnTeamType()).Any();
    public static Move FindMove(Board board, string text) => board.GetPiece(Vector2Int.FromSquareName(text[..2]))
        .GetMove(Vector2Int.FromSquareName(text[..2]), Vector2Int.FromSquareName(text[2..]));
}
