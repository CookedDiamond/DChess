using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DChess.Persistence {
    public sealed class SessionState {
        public int Version { get; set; } = 1;
        public string Mode { get; set; }
        public BoardState Board { get; set; }
        public string HelperBot { get; set; }
        public int BotTimeLimitMilliseconds { get; set; } = 1000;
        public bool BotMovePending { get; set; }
        public MatchState Match { get; set; }

        public void Validate() {
            if (Version != 1) throw new InvalidDataException("Unsupported autosave version.");
            if (Mode == "sandbox" && Board != null) {
                Board.Restore();
                if (BotTimeLimitMilliseconds <= 0) throw new InvalidDataException("Invalid saved time limit.");
                if (HelperBot != null && BotRegistry.Find(HelperBot) == null) throw new InvalidDataException($"Saved bot '{HelperBot}' is unavailable.");
            } else if (Mode == "match" && Match != null) {
                Match.CreateSettings();
                var board = Match.Board?.Restore();
                foreach (var game in Match.Games) game.Restore();
                if (Match.Games.Count > 0 && board?.GetMoveCount() != Match.Games[^1].Positions.Count - 1)
                    throw new InvalidDataException("Saved match history does not match its current board.");
                if (Match.Games.Take(Math.Max(0, Match.Games.Count - 1)).Any(g => g.Result == GameResult.Ongoing))
                    throw new InvalidDataException("Saved match contains more than one ongoing game.");
            } else throw new InvalidDataException("Invalid autosave session.");
        }
    }

	public sealed class MatchState {
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public int GameCount { get; set; }
        public int TimeLimitMilliseconds { get; set; }
        public bool AlternateColors { get; set; }
        public int MaxPlies { get; set; }
        public GameConfiguration Configuration { get; set; } = new();
        public bool UsePairedOpenings { get; set; }
        public int OpeningSeed { get; set; }
        public BoardState InitialPosition { get; set; }
        public BoardState Board { get; set; }
        public List<SavedGame> Games { get; set; } = new();

        public MatchSettings CreateSettings() {
            var player1 = BotRegistry.Find(Player1) ?? throw new InvalidDataException($"Saved player '{Player1}' is unavailable.");
            var player2 = BotRegistry.Find(Player2) ?? throw new InvalidDataException($"Saved player '{Player2}' is unavailable.");
            if (GameCount < 1 || Games.Count > GameCount || TimeLimitMilliseconds <= 0 || MaxPlies < 1)
                throw new InvalidDataException("Invalid saved match settings.");
            if (Games.Count > 0 && Games[^1].Result == GameResult.Ongoing && Board == null)
                throw new InvalidDataException("Saved match is missing its current board.");
            Configuration.CreateBoard();
            return new MatchSettings { Player1 = player1, Player2 = player2, Games = GameCount,
                TimeLimitMilliseconds = TimeLimitMilliseconds, AlternateColors = AlternateColors, MaxPlies = MaxPlies,
                Configuration = Configuration.Copy(), UsePairedOpenings = UsePairedOpenings, OpeningSeed = OpeningSeed, InitialPosition = InitialPosition };
        }
        public Match CreateReplay() => new(new MatchSettings {
            Player1 = new BotInfo(Player1, null), Player2 = new BotInfo(Player2, null), Games = GameCount,
            TimeLimitMilliseconds = TimeLimitMilliseconds, MaxPlies = MaxPlies, AlternateColors = AlternateColors,
            Configuration = Configuration, UsePairedOpenings = UsePairedOpenings, OpeningSeed = OpeningSeed, InitialPosition = InitialPosition
        }, this, replayOnly: true);
    }

    public sealed class SavedGame {
        public int Number { get; set; }
        public string WhiteName { get; set; }
        public string BlackName { get; set; }
        public int WhitePlayerIndex { get; set; }
        public GameResult Result { get; set; }
        public string ResultReason { get; set; }
        public GameConfiguration Configuration { get; set; } = new();
        public string OpeningName { get; set; } = "Start position";
        public int OpeningPlies { get; set; }
        public int TimeLimitMilliseconds { get; set; }
        public List<SnapshotState> Positions { get; set; } = new();

        public static SavedGame Capture(GameRecord game) => new() {
            Number = game.Number, WhiteName = game.WhiteName, BlackName = game.BlackName,
            WhitePlayerIndex = game.WhitePlayerIndex, Result = game.Result, ResultReason = game.ResultReason, Configuration = game.Configuration,
            OpeningName = game.OpeningName, OpeningPlies = game.OpeningPlies, TimeLimitMilliseconds = game.TimeLimitMilliseconds,
            Positions = Enumerable.Range(0, game.PositionCount).Select(i => SnapshotState.Capture(game.GetPosition(i))).ToList()
        };
        public GameRecord Restore() {
            if (WhitePlayerIndex is not (0 or 1) || !Enum.IsDefined(Result) || Positions.Count == 0)
                throw new InvalidDataException("Invalid saved game.");
            if (OpeningPlies < 0 || OpeningPlies >= Positions.Count) throw new InvalidDataException("Invalid saved opening length.");
            var game = new GameRecord(Number, WhiteName, BlackName, WhitePlayerIndex, Configuration, OpeningName, OpeningPlies, TimeLimitMilliseconds);
            foreach (var position in Positions) game.AddPosition(position.Restore());
            if (Result != GameResult.Ongoing) game.Finish(Result, ResultReason);
            return game;
        }
    }

    public sealed class SnapshotState {
        public int Width { get; set; }
        public int Height { get; set; }
        public PieceType[] Types { get; set; }
        public TeamType[] Teams { get; set; }
        public bool[] Disabled { get; set; }
        public TeamType SideToMove { get; set; }
        public Vector2Int[] ChangedSquares { get; set; }
        public string MoveText { get; set; }
        public long ThinkMilliseconds { get; set; }

        public static SnapshotState Capture(PositionSnapshot position) {
            int count = position.Width * position.Height;
            var state = new SnapshotState { Width = position.Width, Height = position.Height,
                Types = new PieceType[count], Teams = new TeamType[count], Disabled = new bool[count],
                SideToMove = position.SideToMove, ChangedSquares = position.ChangedSquares,
                MoveText = position.MoveText, ThinkMilliseconds = position.ThinkMilliseconds };
            for (int y = 0; y < state.Height; y++) for (int x = 0; x < state.Width; x++) {
                int i = y * state.Width + x;
                state.Types[i] = position.Types[x, y]; state.Teams[i] = position.Teams[x, y]; state.Disabled[i] = position.Disabled[x, y];
            }
            return state;
        }
        public PositionSnapshot Restore() {
            if (Width < 1 || Height < 1 || Width > 256 || Height > 256 || Types?.Length != Width * Height
                || Teams?.Length != Types.Length || Disabled?.Length != Types.Length || SideToMove is not (TeamType.White or TeamType.Black))
                throw new InvalidDataException("Invalid saved replay position.");
            var board = new Board(new Vector2Int(Width, Height));
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) {
                int i = y * Width + x;
                if (!Enum.IsDefined(Types[i]) || !Enum.IsDefined(Teams[i]) || (Types[i] != PieceType.None && Teams[i] == TeamType.None))
                    throw new InvalidDataException("Invalid saved replay piece.");
                board.SquareMap[x, y] = Disabled[i] ? SquareType.Disabled : SquareType.Normal;
                if (Types[i] != PieceType.None) board.PlacePiece(new(x, y), Piece.GetPieceFromType(Types[i], Teams[i], board));
            }
            return new PositionSnapshot(board, null, MoveText, ThinkMilliseconds, SideToMove, ChangedSquares);
        }
    }
}
