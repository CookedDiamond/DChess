# DChess

A chess game (MonoGame) with a **bot arena**: write chess bots as single C# files and let them play against each other. You can watch the games live or replay them afterwards. There is also a headless command line interface for the engine.

Games end when a king is captured; check and checkmate are not enforced.

## Run it

Requirements: Windows and the [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer.

```bash
dotnet run --project DChess.csproj
```

The first build also restores the MonoGame content tools, which takes a minute. You can also open `DChess.sln` in Visual Studio / Rider and press Start.

Build and run the tests:

```bash
dotnet build DChess.sln -c Release
```

```bash
dotnet test DChess.sln -c Release
```

## Using the app

**Menu:** choose Player 1 and Player 2 (any bot or **Human**), the number of games, and the time per move, then press **Start Match**. Player 1 starts as White and colors swap every game. Escape quits.

**Arena:** shows the games of the match. The left panel has the score and the list of games (click one to watch it). The right panel has the moves of the selected game (click a move to jump to it).

| Key | |
|---|---|
| Space | Play / pause the replay |
| Left / Right (or mouse wheel) | Step one half-move back / forward |
| Home / End | Start / end of the game (End keeps following a running game) |
| Up / Down | Previous / next game |
| + / − | Replay speed |
| F | Flip the board |
| Esc | Back to the menu (stops the match) |

When a human plays, click a piece and then its target square.

Moves appear immediately while the opponent thinks. Bot calculation runs on a private
board in the background, so drawing, resizing, menu controls, and replay remain responsive.

**Sandbox:** a free board. Click to move pieces for both sides. `A` lets the bot move, `D` undoes a move, `S` prints the evaluation.

Sandbox bot requests also run in the background. Moving, undoing, or leaving the board
cancels the request; its result cannot overwrite a newer position.

## Tournaments

Choose **Tournaments & history** in the menu, then **New tournament**. Select 2, 4, 8,
16, 32, or 64 entrants, an even number of games per pairing, the time per move, and the
move limit. Each seed has a bot selector; duplicate entries are allowed and are identified
by their seed number. Enable any combination of the registered variants, including
promotion, castling, friendly fire, and battle royale. With none selected, games use the
base king-capture rules.

Adjacent seeds play each other in a knockout bracket. Winners advance through the
quarter-finals, semi-finals, and final as appropriate. All pairings share the same settings
and starting-position template. Every opening is played twice with the bots swapping colors:

- Games 1 and 2 use the starting position.
- Later pairs begin after three full moves from a compatible book opening. The eight
  included lines cover Italian, Ruy Lopez, Sicilian, French, Queen's Gambit, King's Indian,
  Caro-Kann, and Scotch openings. The seed gives a repeatable opening order shared by
  all pairings. Book moves remain visible in replay and do not consume bot thinking time.
- A future variant with a different setup uses a deterministic legal opening if no book
  line applies, or the starting position if six opening half-moves cannot be played.
  The recorded opening name identifies that fallback.
- A win awards 1 point, a draw 0.5, and a loss 0. A tied pairing gets extra opening pairs
  with 20% less time per move each pair, down to 1 ms. It continues until one bot scores
  at least 1.5 points in a tiebreak pair. There is no random winner selection.

The results screen shows each pairing's winner, points, W/D/L, White/Black wins, average
played half-moves, average time for completed bot moves, and game-ending reasons. Click a
pairing and then a game to replay it, or watch the active pairing. Returning from the viewer
keeps the tournament running. **Pause tournament** saves progress; **Resume tournament**
continues an interrupted game and the remaining bracket. Persistent ties can be paused.

**History** retains separate compressed archives in `%LOCALAPPDATA%\DChess\Tournaments`.
Archives are saved after games and on pause/close using atomic replacement and backups.
History writing runs on a separate worker at lower priority; completed snapshots are cached.
The history list reads compact summaries and loads full game data only for a selected event.
Results and replay remain viewable after a bot or variant is removed from the project;
resuming play requires those implementations to be available. Normal game autosaves remain
separate. A running app is not restarted or terminated by building this feature.

### Adding a variant

Implement `Variant`, then register one `VariantDefinition` in `VariantRegistry` before
starting the UI or CLI. Its stable ID, display name, construction function, and state capture
function supply tournament selection, board saves, and CLI variant selection together.
Store extra parameters in `VariantState.Parameters`. No tournament-specific switch is needed.
Implement `Clone()` for mutable state. `ConfigureInitialBoard` can customize the setup;
tournaments freeze that setup once so both colors play identical configurations.
Override `GetOutcome` for a different winning objective and set `UseStandardDrawRules` to
false when orthodox repetition, 50-move, or insufficient-material draws do not apply.
No-legal-move draws and the configured move limit still prevent unlimited individual games.

## Stockfish evaluation

Install the official Stockfish 19 Windows x64 engine once before building:

```powershell
./scripts/Install-Stockfish.ps1
dotnet build DChess.sln -c Release
```

The installer checks the release SHA256. Downloaded engine files are ignored by Git;
build/publish copies the executable, GPL license, and supplied source into the output.
Source and releases: https://github.com/official-stockfish/Stockfish/releases/tag/sf_19.

The sandbox and arena show a white/black evaluation bar for the displayed position,
including replay. Positive scores favor White; negative scores favor Black; M means mate.
The bar flips with the arena board. Searches run in a separate background process with
one engine thread and a short time budget; changing positions or closing the scene cancels
old work. Missing engines and unsupported positions show N/A without interrupting play.

This is a **standard chess estimate**: DChess uses king capture and has no en passant.
Custom boards, disabled squares, unsupported variants, missing kings, and positions where
the nonmoving king is attacked are not evaluated. The bar is an advantage display, not
a calibrated winning probability. BotBoard/BotTimer expose neither Stockfish nor scores;
analysis stays private to the UI and is excluded from game records and autosaves.
This provides bot API isolation; in-process bot code is not an OS security sandbox.

## Autosave and resume

Sandbox games and arena matches are saved after each move and when returning to the menu
or closing the app. Choose **Resume saved game** in the menu after reopening DChess.
The save includes the current turn, piece move counts and castling rights, active variants,
undo history, player names, time limits, match scores, and replay positions. An interrupted
bot turn is recalculated when the game resumes.

The latest session is stored at `%LOCALAPPDATA%\DChess\autosave.json`. Writes replace the
file atomically and retain the previous save as `autosave.json.bak`; a damaged primary save
falls back to that backup. Starting a new game replaces the latest session. CLI games are
independent of the GUI autosave.

Castling requires an unmoved king and rook and a clear, playable path. There is no rule against castling through check, consistent with king capture rules.

## Command line

**Bot matches** without a window (fast, many games):

```bash
dotnet run --project DChess.csproj -- --arena MinMaxBot GreedyBot --games 10 --time 500
```

**Engine CLI:** commands include `move e2 e4`, `moves e2`, `undo`, `ai`, `board`, `history`, and `quit`. Run `dotnet run -- cli --help` for options or `help` inside the CLI. Use `--preset small --size 6` for a custom board size. Available variants are `promotion`, `castling`, `friendlyfire`, and `battleroyale`.

```bash
dotnet run --project DChess.csproj -- cli --variant castling
```

```powershell
"move e2 e4`nundo`nquit" | dotnet run --project DChess.csproj -- cli
```

**Smoke test:** loads content, renders ten frames, and exits. It requires a Windows graphics session.

```bash
dotnet run --project DChess.csproj -c Release -- --smoke-test
dotnet run --project DChess.csproj -c Release -- --smoke-test sandbox
dotnet run --project DChess.csproj -c Release -- --smoke-test arena
```

## Writing a bot

See **[BOTS.md](BOTS.md)**. In short: add a file to `Bots/` with a class implementing `IChessBot`, then rebuild.

Included bots:

- `RandomBot`: plays random moves.
- `GreedyBot`: simple one-move lookahead that takes the most valuable piece and avoids hanging its own.
- `MinMaxBot`: alpha-beta search with iterative deepening, quiescence search and time management. It is also the engine behind the CLI `ai` command and the sandbox `A` key.

## Tests

Automated tests (`Tests/`) cover movement, captures, promotion, castling, variant undo, independent AI search copies, CLI options, and TCP framing.

They also cover save/reload and backup recovery, resuming a human match, cancellation of
stale bot results, UI-thread move commits, and immediate human-move display during bot calculation.
Tournament tests cover bracket advancement, paired openings, repeated faster tiebreaks,
variant configuration and future outcome hooks, resumable tournaments, history backup
recovery, archive replay without the original bots, and separation from Stockfish analysis.

## Multiplayer

Multiplayer is a local TCP relay on `127.0.0.1:13000`; both peers must start with matching boards and variants. Incoming moves are validated and applied on the game thread. Matchmaking, remote undo synchronization, and variant configuration negotiation are not implemented, and the menu has no multiplayer entry yet.

## Project layout

| Folder | |
|---|---|
| `Bots/` | The bots, one file each. |
| `BotApi/` | What bots see: `IChessBot`, `BotBoard`, `BotTimer`, plus the bot registry and the runner that enforces time limits. |
| `Chess/Arena/` | Matches: game rules (end conditions), recording and move notation, and the terminal arena. |
| `Chess/Playground/`, `Chess/Pieces/`, `Chess/Variants/` | The chess engine (board, moves, pieces, variants such as castling and promotion). |
| `Cli/` | Headless text interface to the engine. |
| `UI/` | Scenes (menu, arena, sandbox) and drawing helpers. |
| `Multiplayer/` | TCP client/server. |
| `Tests/` | MSTest engine, CLI and networking tests. |
