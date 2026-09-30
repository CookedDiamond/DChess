using DChess.Chess.Playground;
using DChess.Persistence;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Arena;

public sealed class GameConfiguration {
    public int Width { get; set; } = 8;
    public int Height { get; set; } = 8;
    public List<VariantState> Variants { get; set; } = new() {
        new() { Kind = "promotion" }, new() { Kind = "castling", Parameter = 2 }
    };
    public string Description => Variants.Count == 0 ? "King capture" : string.Join(" + ", Variants.Select(v => v.Kind));
    public Board CreateBoard() {
        if (Width < 8 || Height < 4 || Width > 26 || Height > 26) throw new ArgumentException("Board dimensions must be 8-26 files and 4-26 ranks.");
        var board = new Board(new Vector2Int(Width, Height));
        foreach (var state in Variants) board.Variants.Add(state.Restore());
        BoardSetup.PlaceStandardPieces(board);
        foreach (var variant in board.Variants) variant.ConfigureInitialBoard(board);
        return board;
    }
    public GameConfiguration Copy() => new() { Width = Width, Height = Height,
        Variants = Variants.Select(v => VariantState.Capture(v.Restore())).ToList() };
}
