# Writing a DChess bot

A bot is **one C# file** in the `Bots/` folder. Every class that implements `IChessBot` shows up automatically in the menu after you rebuild (`dotnet run` rebuilds for you).

> **Vibe coding tip:** give your AI this file plus `BotApi/BotBoard.cs` and `Chess/Playground/Move.cs`, and ask it to write or improve `Bots/YourBot.cs`. Use `Bots/GreedyBot.cs` (simple) or `Bots/MinMaxBot.cs` (search with alpha-beta) as an example.

## Quick start

1. Create `Bots/MyBot.cs` (use your own name, e.g. `MatthiasBot.cs`).
2. Paste this template:

```csharp
using DChess.BotApi;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;

namespace DChess.Bots {
	public class MyBot : IChessBot {
		public string Name => "MyBot";

		public Move Think(BotBoard board, BotTimer timer) {
			List<Move> moves = board.GetLegalMoves();

			// Always take the king if you can: that wins the game.
			foreach (Move move in moves) {
				if (move.CapturedPiece.Type == PieceType.King) return move;
			}

			// ... your ideas here ...
			return moves[0];
		}
	}
}
```

3. Run `dotnet run`, pick your bot as Player 1 or Player 2 and press **Start Match**.

## Rules of this chess

It is normal chess with a few simplifications:

- **You win by capturing the enemy king.** There is no check or checkmate. Moves that leave your own king attacked are legal, but the opponent can then take your king and win.
- `GetLegalMoves()` therefore contains all normal piece moves, including ones that walk into check.
- **Castling:** the king moves two squares towards an unmoved rook and the rook jumps next to it. All squares between them must be empty. There are no check restrictions.
- **Promotion:** a pawn reaching the last rank automatically becomes a queen.
- **No en passant.**
- **Draws:** the side to move has no moves, 50 moves (100 half-moves) without a capture or pawn move, the same position three times, only kings plus at most one knight or bishop left, or the move limit (250 moves).
- **You lose immediately** if `Think` throws an exception, returns `null`, returns an illegal move, or takes longer than the time limit.

## Coordinates

Squares are `Vector2Int(x, y)`:

- `x` is the file: `0` = a … `7` = h
- `y` is the rank: `0` = rank 1 … `7` = rank 8

White starts on `y = 0, 1` and moves up (+y). Black starts on `y = 6, 7` and moves down (−y).
`new Vector2Int(4, 1).ToSquareName()` is `"e2"`, and `Vector2Int.FromSquareName("e2")` is `(4, 1)`.

## How your bot is called

- A **new instance** of your bot is created for every game, so you can keep state in fields (for example, an opening book or a cache) during one game.
- `Think(board, timer)` is called whenever it is your turn. It must return one of the moves from `board.GetLegalMoves()`.
- `board` is **your own private copy** of the game. You can `MakeMove` and `UndoMove` as much as you like to search ahead. The real game is not affected.
- `Console.WriteLine` output shows up in the terminal you started the game from, which is handy for debugging.

## API reference

### `IChessBot`

| Member | |
|---|---|
| `string Name` | Name shown in the menu (optional, defaults to the class name). |
| `Move Think(BotBoard board, BotTimer timer)` | Return your move. |

### `BotBoard`

| Member | |
|---|---|
| `TeamType MyTeam`, `OpponentTeam` | Your color and the opponent's. These don't change while you make or undo moves. |
| `TeamType SideToMove`, `bool IsWhiteToMove` | Whose turn it is in the current (searched) position. |
| `int Width`, `Height` | Board size (8 × 8). |
| `int PlyCount` | Half-moves played so far (including the ones you made while searching). |
| `bool IsGameOver`, `TeamType Winner` | A king was captured / who captured it (`TeamType.None` if nobody). |
| `List<Move> GetLegalMoves()` | All moves of the side to move. |
| `List<Move> GetLegalMoves(TeamType team)` | Moves a team could make, even if it is not its turn (useful for mobility). |
| `List<Move> GetLegalMovesFrom(Vector2Int square)` | Moves of one piece. |
| `List<Move> GetCaptureMoves()` | Only the captures of the side to move. |
| `Move FindMove(Vector2Int from, Vector2Int to)`, `Move FindMove("e2e4")` | Find a legal move, or `null`. |
| `void MakeMove(Move move)` | Play a move from `GetLegalMoves()` of the *current* position. |
| `void UndoMove()` | Take back your last `MakeMove`. |
| `Piece GetPiece(Vector2Int square)`, `GetPiece(x, y)` | The piece on a square; empty squares return `Piece.NULL_PIECE` (`Type == PieceType.None`). |
| `bool IsEmpty(square)`, `bool IsOnBoard(square)` | |
| `List<PieceOnSquare> GetPieces(TeamType team)`, `GetAllPieces()` | Pieces with their squares (`.Square`, `.Piece`, `.Type`, `.Team`). |
| `int CountPieces(TeamType team, PieceType type)` | |
| `Vector2Int? FindKing(TeamType team)` | |
| `bool IsSquareAttackedBy(Vector2Int square, TeamType attacker)` | Could the attacker capture on that square? Works for empty squares too. |
| `bool IsKingAttacked(TeamType team)` | "Is this team in check?" |
| `string ToString()` | The board as text, for `Console.WriteLine(board)`. |

### `Move`

| Member | |
|---|---|
| `Vector2Int From`, `To` | Start and target square (for castling: the king's squares). |
| `Piece MovingPiece`, `CapturedPiece` | `CapturedPiece` is `Piece.NULL_PIECE` if nothing is captured. |
| `bool IsCapture`, `IsCastling`, `IsPromotion` | |
| `string ToString()` | e.g. `"e2e4"`. |

### `Piece`

`Type` (`PieceType.Pawn`, `Knight`, `Bishop`, `Rook`, `Queen`, `King`, `None`), `Team` (`TeamType.White`, `Black`, `None`), `MoveCount` (how often the piece has moved), `GetPieceScore()` (1, 3, 3, 5, 9, 1000).

### `BotTimer`

`TimeLimitMilliseconds`, `ElapsedMilliseconds`, `MillisecondsRemaining`, `IsTimeUp`. The clock starts when your bot is asked to move. Check it regularly if you search deeply. `MinMaxBot` shows how to stop a search in time (iterative deepening).

## Testing your bot

- **In the window:** choose the bots, number of games and time per move in the menu. You can watch the games live or replay them move by move. Choose **Human** as a player to play against your bot yourself.
- **In the terminal** (fast, no window): play many games and print the score.

```bash
dotnet run -- --list
```

```bash
dotnet run -- --arena MyBot GreedyBot --games 20 --time 500
```

Options: `--games N`, `--time MS_PER_MOVE`, `--max-moves N`, `--moves` (prints every game's moves, which is useful for asking an AI why your bot blundered).

- Set `MinMaxBot.LogSearchInfo = true` to see how deep it searches each move.

## Fair play suggestions

For a fair bot vs. bot competition you might agree on:

- The same time per move for both bots (colors swap every game automatically).
- Single-threaded bots, so nobody wins just by using more CPU cores.
- No reading or changing the other bot's code or the game internals at runtime. Use only the `BotBoard` API.
