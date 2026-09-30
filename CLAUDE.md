# DChess

Custom chess engine + GUI written in C# / .NET 8 (Windows) on **MonoGame** with a pluggable variant system and a **bot arena**: bots are single files in `Bots/` implementing `IChessBot` (see `BOTS.md`), and the GUI lets two bots (or a human) play matches and replays them. There is also a **CLI mode** for headless play and automated testing by subagents (see `Cli/`).

## Build & run

```sh
dotnet build                    # builds DChess.exe
dotnet run                      # launches the GUI (MonoGame window)
dotnet run -- cli               # launches the text-based CLI (no window)
dotnet run -- cli --variant friendlyfire --variant promotion
dotnet run -- --arena MinMaxBot GreedyBot --games 10 --time 500   # headless bot match
dotnet run -- --smoke-test      # render ten frames and exit
dotnet test DChess.sln          # MSTest project in Tests/
```

`TargetFramework = net8.0-windows` (`RollForward = Major`, so it builds with the .NET 8/9/10 SDKs), `OutputType = Exe` (keeps a console so `Console.WriteLine` from bots and the CLI is visible; `CliConsole.AttachToParent` is only needed for WinExe and is a no-op here), `UseWindowsForms = true`. Windows-only because of MonoGame.WindowsDX.

## Top-level layout

```
Program.cs                 entrypoint — dispatches to GUI (Game1), CLI ("cli") or headless arena ("--arena"/"--list")
Game1.cs                   MonoGame Game subclass (GUI lifecycle, scene switching)
Cli/                       headless console UI (added for agent testing)
BotApi/                    IChessBot, BotBoard (what bots see), BotTimer, BotRegistry (reflection discovery), BotRunner (time limit + move validation), HumanPlayer
Bots/                      bots, one file each (RandomBot, GreedyBot, MinMaxBot)
Chess/
  Arena/                   Match (runs games on a thread, draw rules), GameRecord/PositionSnapshot (replay), MoveNotation, BoardSetup, ConsoleArena
  Playground/              Board, Move, BoardChange, BoardManager, BoardUI/Networking
  Pieces/                  Piece (abstract) + one file per type (Pawn/Bishop/Knight/Rook/Queen/King/Null)
  Variants/                Variant (abstract) + variant rules (Promotion, FriendlyFire, BattleRoyale, Castling)
  ChessAI/                 Evaluation (used by the CLI "eval" and the sandbox S key)
UI/                        MonoGame UI primitives + Scenes (Menu = bot selection, Arena = match viewer/replay, Board = sandbox)
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
  - `MakeMove(Move)` applies a **copy** of the move (the caller's object is never mutated; moves from another board are remapped to this board's pieces), then runs every `Variant.AfterTurnUpdate(board, move)`. Variants extend the played move with `AddToLastMove` (undo, add changes, re-apply), so one undo reverts everything.
  - `UndoLastMove()` reverses via `Move.Undo`.
  - `CloneBoard()` deep-copies pieces and history, binding every cloned piece to the clone (bots search on clones).
  - `FindEquivalentLegalMove(move)` maps a move from another board (e.g. a bot's clone) to this board's legal move; `IsSquareAttackedBy` and `GetPositionKey` (repetition detection) support bots and the arena.
  - Coordinates: **`x` is file (0..size.x-1)**, **`y` is rank**, `y=0` is White's back rank, `y=size-1` is Black's. White moves `+y`. The engine works with `(x,y)` ints; `Vector2Int.ToSquareName()` / `FromSquareName()` convert to and from "e2".
  - `HasTeamWon()` returns the team whose opponent has no king (this codebase uses king-capture, **not checkmate** — there is no check detection).

- **`Piece`** (`Chess/Pieces/Piece.cs`) abstract base. Subclasses override `GetAllLegalMoves` and **must call** `base.GetAllLegalMoves(...)` so variant-supplied moves are appended. `Piece.NULL_PIECE` represents an empty square. `Piece.GetPieceFromType(...)` is the factory used by cloning.

- **`Move`** (`Chess/Playground/Move.cs`) is a list of `BoardChange`s (`(position, oldPiece, newPiece, oldSquare?, newSquare?)`). A normal move is two changes: clear source, set destination. Captures are encoded as the destination's `oldPiece` being the captured piece. Square changes let variants disable squares undoably (BattleRoyale). `MoveCount` is incremented for pieces that leave a square and land elsewhere in the same move. Convenience properties for bots: `From`, `To`, `MovingPiece`, `CapturedPiece`, `IsCapture`, `IsCastling`, `IsPromotion`; `ToString()` gives "e2e4".

- **`Variant`** (`Chess/Variants/Variant.cs`) — extension hooks:
  - `AdditionalMoves(board, piece, position)` — append moves (e.g. castling).
  - `AfterTurnUpdate(board, move)` — mutate state after each move (e.g. promotion replaces pawns on the back rank; BattleRoyale removes squares).
  - `IsPieceEnemyTeam(normalResult, piece)` — override friend/foe logic (FriendlyFire makes everything enemy).
  - `Clone()` — must produce an independent copy if the variant has mutable state.
  Active variants live on `Board.Variants` — adding a new variant = new subclass + register on the board.

- **`BoardManager`** wires up board + UI + networking, builds starting positions (`Build8x8StandardBoard`, `BuildSmallBoard`), and routes moves through the network layer.
  - GUI code uses `BeginComputerMove` and polls `UpdateComputerMove`; it must never call the blocking `MakeComputerMove` used by the CLI. Results are calculated on a snapshot, checked against `Board.Revision`, and committed only on the UI thread. Cancel pending work on undo or scene exit.
  - `Persistence/` stores the latest GUI session at `%LOCALAPPDATA%/DChess/autosave.json`, with atomic replacement and a backup. Board saves preserve piece identity and complete undo history; match saves retain settings, scores, and replay positions. The menu's Resume button loads the saved session.

- **Bots** (`BotApi/`, `Bots/`) — `BotRunner.RequestMove` clones the board, calls `IChessBot.Think` on a separate thread with a time limit and validates the answer with `FindEquivalentLegalMove`. `MinMaxBot` (iterative deepening alpha-beta with quiescence search, make/undo on one board) replaced the old `MinMaxRecursive`; `BoardManager.MakeComputerMove` (CLI `ai`, sandbox `A`) uses it by default. `Evaluation` sums per-piece scores via `Piece.GetPieceScore(board, pos, team)`.

## Things to know before editing

- There is **no check / checkmate / stalemate logic** — the game ends only when a king is captured. Don't assume standard FIDE rules.
- Coordinates are `Vector2Int(x, y)` ints; `y` increases toward Black.
- `Board.ToString()` already produces a text rendering (starts at `y=0`; the CLI displays `y=size-1` at the top); the CLI builds on this.
- The MonoGame UI references textures via `TextureLoader` — instantiating `Piece` objects is safe headlessly as long as you don't call `Piece.GetPieceTexture(...)`.
- `Program.cs` uses top-level statements; keep CLI dispatch there.

## Adding a new variant

1. Subclass `Variant` in `Chess/Variants/`.
2. Override the relevant hooks; implement `Clone()` if it carries state.
3. Register one `VariantDefinition` in `Chess/Variants/VariantRegistry.cs` before startup, with a stable ID, display name, restore factory and state capture function. This supplies tournament selection, persistence, and CLI selection together. Extra parameters can use `VariantState.Parameters`.
4. Override `ConfigureInitialBoard` for a different setup and `GetOutcome` for a different objective. Set `UseStandardDrawRules` to false when orthodox draw rules do not apply. Tournaments freeze the initial setup and validate book openings against it.

## Tournaments

- `Chess/Arena/Tournament.cs` runs sequential knockout pairings on a background worker, with an even game count and alternating colors. The first opening pair uses the initial position; later pairs use six book half-moves from `OpeningBook`. Ties get color-paired extra games with 20% less time each pair (minimum 1 ms) until one side scores at least 1.5 points in that pair. No lottery winner.
- `Persistence/TournamentStore.cs` keeps one compressed, atomic archive per tournament, with a backup. A worker at lower priority coalesces writes; completed replay snapshots are cached. Pause/close saves an ongoing game before cancellation and exit flushes pending history writes.
- `UI/Scenes/SceneTournaments.cs` supplies setup, bracket results, statistics, history and replay links. The tournament is owned by `Game1`, independently of the current scene. `SceneArena.OwnsMatch` is false for tournament viewers, so leaving one must not cancel the tournament or overwrite the normal autosave.
- Archive replay must work without the original bot or variant. Construct it through `MatchState.CreateReplay`, which does not restore a live rules board or start a bot.
- During parallel bot development, leave existing processes and `Bots/` changes untouched. Use `dotnet build DChess.sln --artifacts-path bin/tournament-verification` for an isolated build. GUI smoke checks can use `--smoke-test tournaments`, `tournament-live`, `tournament-history`, `tournament-results`, or `tournament-replay`; archive screens accept `--smoke-archive-dir`, and `--smoke-capture` exports the rendered frame.

## Adding a bot

Add one file to `Bots/` with a public class implementing `IChessBot` (parameterless constructor). It is discovered by reflection and appears in the menu and in `--arena`. Bots only use the `BotBoard` API; see `BOTS.md`.
