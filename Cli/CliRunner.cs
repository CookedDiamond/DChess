using DChess.Chess.ChessAI;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Chess.Variants;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Cli {
    /// <summary>
    /// Headless text-based interface to the chess engine. Designed to be driven
    /// either by a human in a terminal or by a subagent piping commands on stdin.
    /// All output goes to stdout; nothing depends on the MonoGame UI layer.
    /// </summary>
    public class CliRunner {

        private Board _board;
        private BoardManager _manager;
        private readonly List<string> _initialVariants;
        private readonly string _preset;
        private readonly int _boardSize;
        private bool _running = true;

        // Variant registry — extend here when adding a new variant.
        private static readonly Dictionary<string, Func<Variant>> VariantFactory = new(StringComparer.OrdinalIgnoreCase) {
            ["promotion"]    = () => new VariantPawnQueenPromotion(),
            ["friendlyfire"] = () => new VariantFriendlyFire(),
            ["battleroyale"] = () => new VariantBattleRoyale(15, 1f),
            ["castling"]     = () => new VariantCastling(),
        };

        public CliRunner(List<string> variants, string preset, int boardSize) {
            if (boardSize < 4 || boardSize > 26) throw new ArgumentOutOfRangeException(nameof(boardSize), "Board size must be between 4 and 26.");
            if (!preset.Equals("small", StringComparison.OrdinalIgnoreCase) && !preset.Equals("standard", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Preset must be standard or small.", nameof(preset));
            if (preset.Equals("standard", StringComparison.OrdinalIgnoreCase) && boardSize != 8)
                throw new ArgumentException("The standard preset requires an 8x8 board; use --preset small for other sizes.");
            foreach (var variant in variants)
                if (!VariantFactory.ContainsKey(variant)) throw new ArgumentException($"Unknown variant '{variant}'.");
            _initialVariants = variants;
            _preset = preset;
            _boardSize = boardSize;
            ResetBoard();
        }

        public static int Run(string[] args) {
            try {
                return RunCore(args);
            } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
                Console.Error.WriteLine("error: " + ex.Message);
                return 1;
            }
        }

        private static int RunCore(string[] args) {
            var variants = new List<string> { "promotion" };
            string preset = "standard";
            int size = 8;

            for (int i = 0; i < args.Length; i++) {
                switch (args[i]) {
                    case "--variant":
                        if (i + 1 >= args.Length) throw new ArgumentException("--variant requires a name.");
                        variants.Add(args[++i]);
                        break;
                    case "--no-default-variants":
                        variants.Clear();
                        break;
                    case "--preset":
                        if (i + 1 >= args.Length) throw new ArgumentException("--preset requires a name.");
                        preset = args[++i];
                        break;
                    case "--size":
                        if (i + 1 >= args.Length) throw new ArgumentException("--size requires a number.");
                        size = int.Parse(args[++i]);
                        break;
                    case "--help":
                    case "-h":
                        PrintCliUsage();
                        return 0;
                    default:
                        throw new ArgumentException($"Unknown option '{args[i]}'.");
                }
            }

            var runner = new CliRunner(variants, preset, size);
            runner.Loop();
            return 0;
        }

        private static void PrintCliUsage() {
            Console.WriteLine("DChess CLI");
            Console.WriteLine("Usage: dotnet run -- cli [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --variant <name>          enable a variant (repeatable). Default: promotion");
            Console.WriteLine("  --no-default-variants     start with no variants enabled");
            Console.WriteLine("  --preset standard|small   starting position preset (default: standard)");
            Console.WriteLine("  --size <n>                board side length (default: 8)");
            Console.WriteLine("Available variants: " + string.Join(", ", VariantFactory.Keys));
        }

        private void ResetBoard() {
            _board = new Board(new Vector2Int(_boardSize, _boardSize));
            foreach (var name in _initialVariants) {
                if (VariantFactory.TryGetValue(name, out var factory)) {
                    _board.Variants.Add(factory());
                } else {
                    Console.Error.WriteLine($"warn: unknown variant '{name}' (skipped)");
                }
            }
            _manager = new BoardManager(_board, new BoardNetworking());
            if (_preset.Equals("small", StringComparison.OrdinalIgnoreCase)) _manager.BuildSmallBoard();
            else _manager.Build8x8StandardBoard();
        }

        private void Loop() {
            Console.WriteLine("DChess CLI. Type 'help' for commands.");
            PrintBoard();
            while (_running) {
                Console.Write(PromptText());
                var line = Console.ReadLine();
                if (line == null) break; // EOF
                line = line.Trim();
                if (line.Length == 0) continue;
                try {
                    Execute(line);
                } catch (Exception ex) {
                    Console.WriteLine("error: " + ex.Message);
                }
            }
        }

        private string PromptText() {
            var team = _board.GetTurnTeamType();
            var won = _board.HasTeamWon();
            if (won != TeamType.None) return $"[{won} won]> ";
            return $"[{team} #{_board.GetMoveCount() + 1}]> ";
        }

        private void Execute(string line) {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLowerInvariant();

            switch (cmd) {
                case "help":     PrintHelp(); break;
                case "board":
                case "print":    PrintBoard(); break;
                case "fen":      Console.WriteLine(BoardSerializer.ToCompact(_board)); break;
                case "turn":     Console.WriteLine(_board.GetTurnTeamType()); break;
                case "eval":     Console.WriteLine(new Evaluation(_board).GetEvaluation()); break;
                case "moves":    PrintMoves(parts); break;
                case "move":     DoMove(parts); break;
                case "undo":     DoUndo(); break;
                case "ai":       DoAi(); break;
                case "winner":   Console.WriteLine(_board.HasTeamWon()); break;
                case "variants": Console.WriteLine(string.Join(", ", _board.Variants.Select(v => v.GetType().Name))); break;
                case "addvariant": DoAddVariant(parts); break;
                case "listvariants": Console.WriteLine(string.Join(", ", VariantFactory.Keys)); break;
                case "reset":    ResetBoard(); PrintBoard(); break;
                case "history":  PrintHistory(); break;
                case "piece":    PrintPiece(parts); break;
                case "place":    DoPlace(parts); break;
                case "remove":   DoRemove(parts); break;
                case "quit":
                case "exit":     _running = false; break;
                default:
                    Console.WriteLine($"unknown command '{cmd}'. Try 'help'.");
                    break;
            }
        }

        private void PrintHelp() {
            Console.WriteLine("Commands:");
            Console.WriteLine("  board                   print the board");
            Console.WriteLine("  fen                     print compact one-line position string");
            Console.WriteLine("  turn                    show whose turn it is");
            Console.WriteLine("  moves [square]          list all legal moves (optionally filtered to a square)");
            Console.WriteLine("  move <from> <to>        make a move, e.g. 'move e2 e4' or 'move 4,1 4,3'");
            Console.WriteLine("  undo                    undo the last move");
            Console.WriteLine("  ai                      let the engine play one move for the side to move");
            Console.WriteLine("  eval                    print position evaluation (positive favors White)");
            Console.WriteLine("  winner                  print the winning team or 'None'");
            Console.WriteLine("  history                 list all moves made so far");
            Console.WriteLine("  piece <square>          describe the piece on a square");
            Console.WriteLine("  place <square> <code>   place a piece (e.g. 'place e4 P' or 'place e4 q')");
            Console.WriteLine("                          uppercase = White, lowercase = Black; codes p/n/b/r/q/k");
            Console.WriteLine("  remove <square>         remove the piece on a square");
            Console.WriteLine("  variants                list active variants on this board");
            Console.WriteLine("  listvariants            list available variants that can be added");
            Console.WriteLine("  addvariant <name>       add a variant to the live board");
            Console.WriteLine("  reset                   restart with the same preset/variants");
            Console.WriteLine("  quit                    exit");
            Console.WriteLine();
            Console.WriteLine("Squares: algebraic 'e4' (board <=26 wide) or 'x,y' indices.");
            Console.WriteLine("  x is the file (0..size-1), y is the rank, y=0 is White's back rank.");
        }

        private void PrintBoard() {
            int sx = _board.Size.x, sy = _board.Size.y;
            // Top: rank labels first, with y=size-1 at top.
            for (int y = sy - 1; y >= 0; y--) {
                Console.Write($"{y + 1,2} ");
                for (int x = 0; x < sx; x++) {
                    var pos = new Vector2Int(x, y);
                    if (!_board.IsValidPosition(pos)) { Console.Write(" #"); continue; }
                    var piece = _board.GetPiece(pos);
                    if (piece == Piece.NULL_PIECE) { Console.Write(" ."); continue; }
                    char c = Piece.TypeAsChar(piece);
                    if (piece.Team == TeamType.White) c = char.ToUpperInvariant(c);
                    Console.Write(" " + c);
                }
                Console.WriteLine();
            }
            Console.Write("   ");
            for (int x = 0; x < sx; x++) {
                if (sx <= 26) Console.Write(" " + (char)('a' + x));
                else Console.Write(" " + x);
            }
            Console.WriteLine();
            Console.WriteLine($"turn: {_board.GetTurnTeamType()}  move#: {_board.GetMoveCount() + 1}  eval: {new Evaluation(_board).GetEvaluation():0.00}");
            var won = _board.HasTeamWon();
            if (won != TeamType.None) Console.WriteLine($"** {won} has won **");
        }

        private void PrintMoves(string[] parts) {
            List<Move> moves;
            if (parts.Length >= 2) {
                var pos = ParseSquare(parts[1]);
                var piece = _board.GetPiece(pos);
                if (piece == Piece.NULL_PIECE) { Console.WriteLine("no piece on " + FormatSquare(pos)); return; }
                moves = piece.GetAllLegalMoves(pos);
            } else {
                moves = _board.GetAllLegalMovesForTeam(_board.GetTurnTeamType());
            }
            foreach (var m in moves) {
                Console.WriteLine(FormatMove(m));
            }
            Console.WriteLine($"({moves.Count} moves)");
        }

        private void DoMove(string[] parts) {
            if (parts.Length < 3) { Console.WriteLine("usage: move <from> <to>"); return; }
            if (_board.HasTeamWon() != TeamType.None) { Console.WriteLine("game is over"); return; }
            var from = ParseSquare(parts[1]);
            var to = ParseSquare(parts[2]);

            var piece = _board.GetPiece(from);
            if (piece == Piece.NULL_PIECE) { Console.WriteLine("no piece on " + FormatSquare(from)); return; }
            if (piece.Team != _board.GetTurnTeamType()) { Console.WriteLine("not your turn (piece belongs to " + piece.Team + ")"); return; }

            var legal = piece.GetAllLegalMoves(from);
            Move chosen = null;
            foreach (var m in legal) {
                if (MoveDestination(m) == to && MoveOrigin(m) == from) { chosen = m; break; }
            }
            if (chosen == null) { Console.WriteLine("illegal move"); return; }
            _manager.MakeMove(chosen);
            PrintBoard();
        }

        private void DoUndo() {
            if (_board.GetMoveCount() == 0) { Console.WriteLine("nothing to undo"); return; }
            _board.UndoLastMove();
            PrintBoard();
        }

        private void DoAi() {
            if (_board.HasTeamWon() != TeamType.None) { Console.WriteLine("game is over"); return; }
            Console.WriteLine("thinking...");
            _manager.MakeComputerMove(false);
            PrintBoard();
        }

        private void DoAddVariant(string[] parts) {
            if (parts.Length < 2) { Console.WriteLine("usage: addvariant <name>"); return; }
            if (!VariantFactory.TryGetValue(parts[1], out var factory)) { Console.WriteLine("unknown variant"); return; }
            _board.Variants.Add(factory());
            Console.WriteLine("added " + parts[1]);
        }

        private void PrintHistory() {
            for (int i = 0; i < _board.GetMoveCount(); i++) {
                // No public accessor for full history list; keep this lightweight.
            }
            Console.WriteLine($"{_board.GetMoveCount()} moves played; last: " +
                (_board.GetMoveCount() == 0 ? "(none)" : FormatMove(_board.GetLastMove())));
        }

        private void PrintPiece(string[] parts) {
            if (parts.Length < 2) { Console.WriteLine("usage: piece <square>"); return; }
            var pos = ParseSquare(parts[1]);
            var p = _board.GetPiece(pos);
            if (p == Piece.NULL_PIECE) Console.WriteLine("empty");
            else Console.WriteLine($"{p.Team} {p.Type} at {FormatSquare(pos)}");
        }

        private void DoPlace(string[] parts) {
            if (parts.Length < 3) { Console.WriteLine("usage: place <square> <code>"); return; }
            var pos = ParseSquare(parts[1]);
            char code = parts[2][0];
            TeamType team = char.IsUpper(code) ? TeamType.White : TeamType.Black;
            PieceType type = char.ToLowerInvariant(code) switch {
                'p' => PieceType.Pawn,
                'n' => PieceType.Knight,
                'b' => PieceType.Bishop,
                'r' => PieceType.Rook,
                'q' => PieceType.Queen,
                'k' => PieceType.King,
                _ => PieceType.None
            };
            if (type == PieceType.None) { Console.WriteLine("unknown piece code"); return; }
            _board.PlacePiece(pos, Piece.GetPieceFromType(type, team, _board));
            PrintBoard();
        }

        private void DoRemove(string[] parts) {
            if (parts.Length < 2) { Console.WriteLine("usage: remove <square>"); return; }
            var pos = ParseSquare(parts[1]);
            _board.RemovePiece(pos);
            PrintBoard();
        }

        // ---- Move/square helpers ----------------------------------------

        private static Vector2Int MoveOrigin(Move m) {
            // Origin = the change whose newPiece is NULL.
            foreach (var c in m.Changes) if (c.newPiece == Piece.NULL_PIECE) return c.boardPosition;
            return m.Changes[0].boardPosition;
        }

        private static Vector2Int MoveDestination(Move m) {
            // Destination = the change whose newPiece is the moving piece (non-null).
            foreach (var c in m.Changes) if (c.newPiece != Piece.NULL_PIECE) return c.boardPosition;
            return m.Changes[^1].boardPosition;
        }

        private string FormatMove(Move m) {
            var from = MoveOrigin(m);
            var to = MoveDestination(m);
            // Read the moving piece from the move itself, not the live board —
            // the board may have moved on (or the move may have been undone).
            Piece moving = Piece.NULL_PIECE;
            foreach (var ch in m.Changes) {
                if (ch.boardPosition == to && ch.newPiece != Piece.NULL_PIECE) { moving = ch.newPiece; break; }
            }
            char c = moving == Piece.NULL_PIECE ? '?' : Piece.TypeAsChar(moving);
            if (moving != Piece.NULL_PIECE && moving.Team == TeamType.White) c = char.ToUpperInvariant(c);
            return $"{c} {FormatSquare(from)} -> {FormatSquare(to)}";
        }

        private Vector2Int ParseSquare(string s) {
            var position = ParseCoordinates(s);
            if (!_board.IsValidPosition(position)) throw new FormatException($"Square '{s}' is outside the playable board.");
            return position;
        }

        private static Vector2Int ParseCoordinates(string s) {
            if (s.Contains(',')) {
                var bits = s.Split(',');
                if (bits.Length != 2) throw new FormatException($"can't parse square '{s}'");
                return new Vector2Int(int.Parse(bits[0]), int.Parse(bits[1]));
            }
            // Algebraic: file letter + rank number (1-indexed).
            if (s.Length >= 2 && char.IsLetter(s[0])) {
                int x = char.ToLowerInvariant(s[0]) - 'a';
                int y = int.Parse(s.Substring(1)) - 1;
                return new Vector2Int(x, y);
            }
            throw new FormatException($"can't parse square '{s}'");
        }

        private string FormatSquare(Vector2Int v) {
            if (_board.Size.x <= 26) return $"{(char)('a' + v.x)}{v.y + 1}";
            return $"({v.x},{v.y})";
        }
    }

    internal static class BoardSerializer {
        // Compact, FEN-ish single-line dump. Not real FEN — coordinates here
        // are y=0 first (matching Board.ToString); useful for diffing positions.
        public static string ToCompact(Board board) {
            var sb = new System.Text.StringBuilder();
            for (int y = 0; y < board.Size.y; y++) {
                int empty = 0;
                for (int x = 0; x < board.Size.x; x++) {
                    var p = board.GetPiece(new Vector2Int(x, y));
                    if (p == Piece.NULL_PIECE) { empty++; continue; }
                    if (empty > 0) { sb.Append(empty); empty = 0; }
                    char c = Piece.TypeAsChar(p);
                    if (p.Team == TeamType.White) c = char.ToUpperInvariant(c);
                    sb.Append(c);
                }
                if (empty > 0) sb.Append(empty);
                if (y < board.Size.y - 1) sb.Append('/');
            }
            sb.Append(' ');
            sb.Append(board.IsWhitesTurn ? 'w' : 'b');
            return sb.ToString();
        }
    }
}
