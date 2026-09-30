# DChess

A chess game (MonoGame) with a **bot arena**: write chess bots as single C# files and let them play against each other. You can watch the games live or replay them afterwards.

## Run it

Requirements: Windows and the [.NET 8 SDK](https://dotnet.microsoft.com/download) (a newer SDK works too).

```bash
dotnet run
```

The first build also restores the MonoGame content tools, which takes a minute. You can also open `DChess.sln` in Visual Studio / Rider and press Start.

## Using the app

**Menu:** choose Player 1 and Player 2 (any bot or **Human**), the number of games, and the time per move, then press **Start Match**. Player 1 starts as White and colors swap every game.

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

**Sandbox:** the free board from before. Click to move pieces for both sides. `A` lets the selected bot move, `D` undoes a move, `S` prints the evaluation.

**Terminal matches** (no window, fast):

```bash
dotnet run -- --arena MinMaxBot GreedyBot --games 10 --time 500
```

## Writing a bot

See **[BOTS.md](BOTS.md)**. In short: add a file to `Bots/` with a class implementing `IChessBot`, then rebuild.

Included bots:

- `RandomBot`: plays random moves.
- `GreedyBot`: simple one-move lookahead that takes the most valuable piece and avoids hanging its own.
- `MinMaxBot`: the revamped old AI. It uses alpha-beta search with iterative deepening, quiescence search and time management.

## Project layout

| Folder | |
|---|---|
| `Bots/` | The bots, one file each. |
| `BotApi/` | What bots see: `IChessBot`, `BotBoard`, `BotTimer`, plus the bot registry and the runner that enforces time limits. |
| `Chess/Arena/` | Matches: game rules (end conditions), recording and move notation, and the terminal arena. |
| `Chess/Playground/`, `Chess/Pieces/`, `Chess/Variants/` | The chess engine (board, moves, pieces, variants such as castling and promotion). |
| `UI/` | Scenes (menu, arena, sandbox) and drawing helpers. |
