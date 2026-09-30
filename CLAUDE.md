# DChess

Custom chess engine + GUI written in C# / .NET 10 (Windows) on **MonoGame** with a pluggable variant system and a MinMax AI. There is also a **CLI mode** for headless play and automated testing by subagents (see `Cli/` and "Running the CLI" below).

## Build & run

```sh
dotnet build                    # builds the WinExe
dotnet run                      # launches the GUI (MonoGame window)
dotnet run -- cli               # launches the text-based CLI (no window)
dotnet run -- cli --variant friendlyfire --variant promotion
```

`TargetFramework = net10.0-windows`, `OutputType = WinExe`, `UseWindowsForms = true`. Windows-only because of MonoGame.WindowsDX. Console I/O still works when launched from a terminal via `dotnet run`.

## Top-level layout

```
Program.cs                 entrypoint — dispatches to GUI (Game1) or CLI based on args
Game1.cs                   MonoGame Game subclass (GUI lifecycle, scenes)
Cli/                       headless console UI (added for agent testing)
Chess/
  Playground/              Board, Move, BoardChange, BoardManager, BoardUI/Networking
  Pieces/                  Piece (abstract) + one file per type (Pawn/Bishop/Knight/Rook/Queen/King/Null)
  Variants/                Variant (abstract) + variant rules (Promotion, FriendlyFire, BattleRoyale, Castling)
  ChessAI/                 MinMax search + Evaluation
UI/                        MonoGame UI primitives + Scenes (Menu, Board)
Multiplayer/               TCP client/server + ByteConverter
Util/                      Vector2Int, ChessUtil, TextureLoader, ScalingUtil, UIEffectsUtil
Extensions/                color & SpriteBatch extensions
Content/                   MGCB content (textures, fonts)
```

## Core model

- **`Board`** (`Chess/Playground/Board.cs`) is the single source of truth for game state.
  - `Pieces` is a `Dictionary<Vector2Int, Piece>` (sparse — missing key = empty square).
  - `SquareMap[x,y]` marks `Disabled` squares (used by BattleRoyale shrinking board).
  - `IsWhitesTurn` is derived from `_moveHistory.Count` (no separate turn flag).
  - `MakeMove(Move)` applies the move, then runs every `Variant.AfterTurnUpdate(board, move)`.
  - `UndoLastMove()` reverses via `Move.Undo`.
  - `CloneBoard()` deep-copies (used heavily by the AI).
  - Coordinates: **`x` is file (0..size.x-1)**, **`y` is rank**, `y=0` is White's back rank, `y=size-1` is Black's. White moves `+y`. This is *not* standard chess coordinates — there is no a-h/1-8 mapping anywhere; everything is `(x,y)` ints.
  - `HasTeamWon()` returns the team whose opponent has no king (this codebase uses king-capture, **not checkmate** — there is no check detection).

- **`Piece`** (`Chess/Pieces/Piece.cs`) abstract base. Subclasses override `GetAllLegalMoves` and **must call** `base.GetAllLegalMoves(...)` so variant-supplied moves are appended. `Piece.NULL_PIECE` represents an empty square. `Piece.GetPieceFromType(...)` is the factory used by cloning.

- **`Move`** (`Chess/Playground/Move.cs`) is a list of `BoardChange`s (`(position, oldPiece, newPiece)`). A normal move is two changes: clear source, set destination. Captures are encoded as the destination's `oldPiece` being the captured piece. This list-of-changes design makes it trivial to add multi-square moves (castling, en passant, promotion-as-move).

- **`Variant`** (`Chess/Variants/Variant.cs`) — extension hooks:
  - `AdditionalMoves(board, piece, position)` — append moves (e.g. castling).
  - `AfterTurnUpdate(board, move)` — mutate state after each move (e.g. promotion replaces pawns on the back rank; BattleRoyale removes squares).
  - `IsPieceEnemyTeam(normalResult, piece)` — override friend/foe logic (FriendlyFire makes everything enemy).
  - `Clone()` — must produce an independent copy if the variant has mutable state.
  Active variants live on `Board.Variants` — adding a new variant = new subclass + register on the board.

- **`BoardManager`** wires up board + UI + networking, builds starting positions (`Build8x8StandardBoard`, `BuildSmallBoard`), and routes moves through the network layer.

- **`MinMaxRecursive`** (`Chess/ChessAI/`) — alpha-beta, parallel root searches with depth 3 (deeper in endgame), uses board cloning per node. `Evaluation` sums per-piece scores via `Piece.GetPieceScore(board, pos, team)`.

## Things to know before editing

- There is **no check / checkmate / stalemate logic** — the game ends only when a king is captured. Don't assume standard FIDE rules.
- Coordinates are `Vector2Int(x, y)` ints; `y` increases toward Black. The CLI translates to/from algebraic notation but the engine itself doesn't.
- `Board.ToString()` already produces a text rendering (starts at `y=0`; the CLI displays `y=size-1` at the top); the CLI builds on this.
- The MonoGame UI references textures via `TextureLoader` — instantiating `Piece` objects is safe headlessly as long as you don't call `Piece.GetPieceTexture(...)`.
- `Program.cs` uses top-level statements; keep CLI dispatch there.

## Adding a new variant

1. Subclass `Variant` in `Chess/Variants/`.
2. Override the relevant hooks; implement `Clone()` if it carries state.
3. Register it on the board (`board.Variants.Add(new MyVariant())`) — in `Program.cs` for the GUI default, and add a `--variant` flag mapping in `Cli/CliRunner.cs` so subagents can enable it from the command line.
