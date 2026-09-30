# DChess

A Windows chess variant game with a MonoGame GUI, a headless command line interface,
and a MinMax AI. Games end when a king is captured; check and checkmate are not enforced.

Requires Windows and the .NET 10 SDK.

```powershell
dotnet build DChess.sln -c Release
dotnet test DChess.sln -c Release
dotnet run --project DChess.csproj
dotnet run --project DChess.csproj -- cli
```

The GUI starts with a standard 8x8 board, queen promotion and castling.
Press A for an AI move, D to undo, S to log the evaluation, and Escape to exit.
Castling requires an unmoved king and rook and a clear, playable path.
There is no rule against castling through check, consistent with king capture rules.

CLI commands include `move e2 e4`, `moves e2`, `undo`, `ai`, `board`, `history`,
and `quit`. Run `dotnet run -- cli --help` for options or `help` inside the CLI.
Use `--preset small --size 6` for a custom board size. Available variants are
`promotion`, `castling`, `friendlyfire`, and `battleroyale`.

```powershell
dotnet run -- cli --variant castling
"move e2 e4`nundo`nquit" | dotnet run -- cli
dotnet run --project DChess.csproj -c Release -- --smoke-test
```

The GUI smoke test loads content, renders ten frames, and exits. It requires a
Windows graphics session. Automated tests cover movement, captures, promotion,
castling, variant undo, independent AI search copies, CLI options, and TCP framing.

Multiplayer is a local TCP relay on `127.0.0.1:13000`; both peers must start with
matching boards and variants. Incoming moves are validated and applied on the
game thread. Matchmaking, remote undo synchronization, and variant configuration
negotiation are not implemented.
