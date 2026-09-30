using DChess.Chess.Arena;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Util;
using System;
using System.Linq;
using System.Text;

namespace DChess.UI.Analysis;

// Position data only. Stockfish scores never enter the rules engine or bot API.
internal sealed record AnalysisPosition(string Fen, string Unavailable = null) {
    internal static AnalysisPosition FromBoard(Board board) {
        if (board.Size != new Vector2Int(8, 8) || board.Variants.Any(v =>
            v is not VariantPawnQueenPromotion && v is not VariantCastling) ||
            board.Variants.OfType<VariantCastling>().Any(v => v.CastlingDistance != 2))
            return new(null, "Unsupported variant");
        string rights = "";
        if (board.Variants.OfType<VariantCastling>().Any()) {
            foreach (var (team, y, kingSide, queenSide) in new[] {
                (TeamType.White, 0, "K", "Q"), (TeamType.Black, 7, "k", "q") }) {
                var king = board.GetPiece(new(4, y));
                if (king.Type != PieceType.King || king.Team != team || king.MoveCount != 0) continue;
                foreach (var (x, symbol) in new[] { (7, kingSide), (0, queenSide) }) {
                    var rook = board.GetPiece(new(x, y));
                    if (rook.Type == PieceType.Rook && rook.Team == team && rook.MoveCount == 0) rights += symbol;
                }
            }
        }
        int quiet = 0;
        foreach (var move in board.GetMoveHistory())
            quiet = move.IsCapture || move.MovingPiece.Type == PieceType.Pawn || move.IsPromotion ? 0 : quiet + 1;
        return Create(board, board.GetTurnTeamType(), rights, quiet, board.GetMoveCount());
    }

    internal static AnalysisPosition FromGame(GameRecord game, int ply) {
        var snapshot = game.GetPosition(ply);
        var board = new Board(new(snapshot.Width, snapshot.Height));
        for (int x = 0; x < snapshot.Width; x++) for (int y = 0; y < snapshot.Height; y++) {
            board.SquareMap[x, y] = snapshot.Disabled[x, y] ? SquareType.Disabled : SquareType.Normal;
            if (snapshot.Types[x, y] != PieceType.None)
                board.PlacePiece(new(x, y), Piece.GetPieceFromType(snapshot.Types[x, y], snapshot.Teams[x, y], board));
        }
        // Arena games start from the standard board. Once a home square changes,
        // its original king/rook can no longer confer castling rights.
        var changed = Enumerable.Range(1, ply).SelectMany(i => game.GetPosition(i).ChangedSquares).ToHashSet();
        string rights = "";
        foreach (var (y, k, q) in new[] { (0, "K", "Q"), (7, "k", "q") }) {
            if (changed.Contains(new(4, y))) continue;
            if (!changed.Contains(new(7, y))) rights += k;
            if (!changed.Contains(new(0, y))) rights += q;
        }
        int quiet = 0;
        for (int i = ply; i > 0; i--) {
            var before = game.GetPosition(i - 1);
            var after = game.GetPosition(i);
            if (after.ChangedSquares.Any(s => before.Types[s.x, s.y] == PieceType.Pawn) ||
                before.Types.Cast<PieceType>().Count(t => t != PieceType.None) != after.Types.Cast<PieceType>().Count(t => t != PieceType.None)) break;
            quiet++;
        }
        return Create(board, snapshot.SideToMove, rights, quiet, ply);
    }

    private static AnalysisPosition Create(Board board, TeamType side, string rights, int quiet, int ply) {
        if (board.Size != new Vector2Int(8, 8) || board.SquareMap.Cast<SquareType>().Any(s => s == SquareType.Disabled))
            return new(null, "Unsupported board");
        foreach (var team in new[] { TeamType.White, TeamType.Black }) {
            var kings = board.Pieces.Where(p => p.Value.Team == team && p.Value.Type == PieceType.King).ToArray();
            if (kings.Length != 1 || (team != side && board.IsSquareAttackedBy(kings[0].Key, side)))
                return new(null, "Not a standard-chess position");
        }
        if (board.Pieces.Any(p => p.Value.Type == PieceType.Pawn && (p.Key.y == 0 || p.Key.y == 7)))
            return new(null, "Not a standard-chess position");
        var fen = new StringBuilder();
        for (int y = 7; y >= 0; y--) {
            int empty = 0;
            for (int x = 0; x < 8; x++) {
                var piece = board.GetPiece(new(x, y));
                if (piece.Type == PieceType.None) { empty++; continue; }
                if (empty > 0) { fen.Append(empty); empty = 0; }
                char symbol = piece.Type switch {
                    PieceType.Pawn => 'p', PieceType.Knight => 'n', PieceType.Bishop => 'b',
                    PieceType.Rook => 'r', PieceType.Queen => 'q', PieceType.King => 'k', _ => '?'
                };
                fen.Append(piece.Team == TeamType.White ? char.ToUpperInvariant(symbol) : symbol);
            }
            if (empty > 0) fen.Append(empty);
            if (y > 0) fen.Append('/');
        }
        // DChess has no en passant. This is explicitly a standard-chess estimate.
        fen.Append($" {(side == TeamType.White ? "w" : "b")} {(rights.Length == 0 ? "-" : rights)} - {quiet} {ply / 2 + 1}");
        return new(fen.ToString());
    }
}
