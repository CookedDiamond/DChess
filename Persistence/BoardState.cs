using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DChess.Persistence {
    // Piece IDs retain shared identities between the live position and undo history.
    public sealed class BoardState {
        public int Width { get; set; }
        public int Height { get; set; }
        public SquareType[] Squares { get; set; }
        public List<PieceState> Pieces { get; set; } = new();
        public List<PlacementState> Placements { get; set; } = new();
        public List<List<ChangeState>> History { get; set; } = new();
        public List<VariantState> Variants { get; set; } = new();

        public static BoardState Capture(Board board) {
            var state = new BoardState { Width = board.Size.x, Height = board.Size.y, Squares = new SquareType[board.Size.x * board.Size.y] };
            var ids = new Dictionary<Piece, int>();
            int Id(Piece piece) {
                if (piece == Piece.NULL_PIECE) return -1;
                if (!ids.TryGetValue(piece, out int id)) {
                    id = state.Pieces.Count;
                    ids.Add(piece, id);
                    state.Pieces.Add(new PieceState { Type = piece.Type, Team = piece.Team, MoveCount = piece.MoveCount });
                }
                return id;
            }
            for (int y = 0; y < state.Height; y++)
                for (int x = 0; x < state.Width; x++) state.Squares[y * state.Width + x] = board.SquareMap[x, y];
            foreach (var pair in board.Pieces)
                state.Placements.Add(new PlacementState { X = pair.Key.x, Y = pair.Key.y, Piece = Id(pair.Value) });
            foreach (var move in board.GetMoveHistory())
                state.History.Add(move.Changes.Select(c => new ChangeState {
                    X = c.boardPosition.x, Y = c.boardPosition.y, OldPiece = Id(c.oldPiece), NewPiece = Id(c.newPiece),
                    OldSquare = c.oldSquare, NewSquare = c.newSquare
                }).ToList());
            foreach (var variant in board.Variants) state.Variants.Add(VariantState.Capture(variant));
            return state;
        }

        public Board Restore() {
            if (Width < 1 || Height < 1 || Width > 256 || Height > 256 || Squares?.Length != Width * Height)
                throw new InvalidDataException("Invalid saved board dimensions.");
            var board = new Board(new Vector2Int(Width, Height));
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    var square = Squares[y * Width + x];
                    if (!Enum.IsDefined(square)) throw new InvalidDataException("Invalid saved square.");
                    board.SquareMap[x, y] = square;
                }
            }
            var pieces = Pieces.Select(p => {
                if (!Enum.IsDefined(p.Type) || p.Type == PieceType.None || p.Team is not (TeamType.White or TeamType.Black) || p.MoveCount < 0)
                    throw new InvalidDataException("Invalid saved piece.");
                var piece = Piece.GetPieceFromType(p.Type, p.Team, board);
                piece.MoveCount = p.MoveCount;
                return piece;
            }).ToArray();
            Piece Get(int id) => id == -1 ? Piece.NULL_PIECE : id >= 0 && id < pieces.Length ? pieces[id] : throw new InvalidDataException("Invalid saved piece reference.");
            Vector2Int Position(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height ? new(x, y) : throw new InvalidDataException("Invalid saved position.");
            foreach (var p in Placements) {
                var position = Position(p.X, p.Y);
                if (!board.IsValidPosition(position) || board.Pieces.ContainsKey(position) || p.Piece == -1)
                    throw new InvalidDataException("Invalid saved placement.");
                board.PlacePiece(position, Get(p.Piece));
            }
            var moves = new List<Move>();
            foreach (var changes in History) {
                var move = new Move();
                foreach (var c in changes) {
                    if ((c.OldSquare.HasValue && !Enum.IsDefined(c.OldSquare.Value)) || (c.NewSquare.HasValue && !Enum.IsDefined(c.NewSquare.Value)))
                        throw new InvalidDataException("Invalid saved square change.");
                    move.AddChange(new BoardChange(Position(c.X, c.Y), Get(c.OldPiece), Get(c.NewPiece), c.OldSquare, c.NewSquare));
                }
                moves.Add(move);
            }
            board.RestoreHistory(moves);
            foreach (var variant in Variants) board.Variants.Add(variant.Restore());
            return board;
        }
    }

    public sealed class PieceState {
        public PieceType Type { get; set; }
        public TeamType Team { get; set; }
        public int MoveCount { get; set; }
    }
    public sealed class PlacementState {
        public int X { get; set; }
        public int Y { get; set; }
        public int Piece { get; set; }
    }
    public sealed class ChangeState {
        public int X { get; set; }
        public int Y { get; set; }
        public int OldPiece { get; set; }
        public int NewPiece { get; set; }
        public SquareType? OldSquare { get; set; }
        public SquareType? NewSquare { get; set; }
    }
    public sealed class VariantState {
        public string Kind { get; set; }
        public int Parameter { get; set; }
        public float Strength { get; set; }
        public static VariantState Capture(Variant variant) => variant switch {
            VariantCastling c => new() { Kind = "castling", Parameter = c.CastlingDistance },
            VariantBattleRoyale b => new() { Kind = "battleroyale", Parameter = b.Interval, Strength = b.Strength },
            VariantPawnQueenPromotion => new() { Kind = "promotion" },
            VariantFriendlyFire => new() { Kind = "friendlyfire" },
            _ => throw new InvalidDataException($"Cannot save variant {variant.GetType().Name}.")
        };
        public Variant Restore() => Kind switch {
            "castling" => new VariantCastling(Parameter),
            "battleroyale" => new VariantBattleRoyale(Parameter, Strength),
            "promotion" => new VariantPawnQueenPromotion(),
            "friendlyfire" => new VariantFriendlyFire(),
            _ => throw new InvalidDataException($"Unknown saved variant '{Kind}'.")
        };
    }
}
