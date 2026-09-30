using DChess.BotApi;
using DChess.Chess.Arena;
using DChess.Chess.Variants;
using DChess.Extensions;
using DChess.Persistence;
using DChess.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DChess.UI.Scenes;

/// <summary>Configure an event, follow the knockout bracket, or browse archived pairings and games.</summary>
public sealed class SceneTournaments : Scene {
    private enum Page { Setup, History, Results }
    private static readonly int[] EntrantCounts = { 2, 4, 8, 16, 32, 64 };
    private static readonly int[] GameCounts = { 2, 4, 6, 10, 20, 50, 100 };
    private static readonly int[] Times = { 50, 100, 250, 500, 1000, 2000, 5000, 10000 };
    private static readonly int[] MoveLimits = { 100, 200, 500, 1000, 2000 };
    private readonly Game1 _game;
    private readonly IReadOnlyList<BotInfo> _bots;
    private readonly IReadOnlyList<VariantDefinition> _variants;
    private readonly HashSet<string> _enabledVariants = new() { "promotion", "castling" };
    private readonly List<(Rectangle Bounds, Action Action)> _actions = new();
    private Task<List<TournamentSummary>> _loading;
    private Task<TournamentState> _opening;
    private List<TournamentSummary> _history = new();
    private TournamentState _selected;
    private Page _page;
    private int _entrantCount, _gameCount, _time = 4, _moveLimit = 2;
    private int[] _entrants = { 0, 1 };
    private int _scroll, _gameScroll, _variantScroll, _selectedPair;
    private Rectangle _listArea, _secondaryArea;
    private float _unit;
    private string _error;

    public SceneTournaments(Game1 game, TournamentState selected = null, bool history = false, int? pairingNumber = null) {
        _game = game; _bots = BotRegistry.Bots; _variants = VariantRegistry.All;
        _selected = selected; _page = selected != null ? Page.Results : history ? Page.History : Page.Setup;
        if (selected != null) _selectedPair = pairingNumber.HasValue ? Math.Max(0, selected.Pairings.FindIndex(p => p.Number == pairingNumber))
            : selected.Champion.HasValue ? selected.Pairings.Count - 1 : 0;
        BackGroundColor = Theme.Background;
        ReloadHistory();
    }
    private void ReloadHistory() { _loading = Task.Run(() => _game.TournamentHistory.ListSummaries()); }
    private void OpenArchive(Guid id) { _error = null; _opening = Task.Run(() => _game.TournamentHistory.Load(id)); }
    public override void Update(GameTime gameTime) {
        if (_loading?.IsCompleted == true) {
            if (_loading.IsCompletedSuccessfully) _history = _loading.Result;
            else { _ = _loading.Exception; _error = "Could not load tournament history."; }
            _loading = null;
        }
        if (_opening?.IsCompleted == true) {
            if (_opening.IsCompletedSuccessfully && _opening.Result != null) Select(_opening.Result);
            else { _ = _opening.Exception; _error = "Could not open tournament archive."; }
            _opening = null;
        }
        var live = _game.Tournament?.View();
        if (live?.Id == _selected?.Id) _selected = live;
    }
    public override void MouseClick(Vector2Int position) {
        foreach (var action in _actions.ToArray()) if (action.Bounds.Contains(position.x, position.y)) { action.Action(); break; }
    }
    public override void MouseScroll(Vector2Int position, int delta) {
        int direction = -Math.Sign(delta);
        if (_secondaryArea.Contains(position.x, position.y)) {
            if (_page == Page.Setup) _variantScroll = Math.Max(0, _variantScroll + direction);
            else _gameScroll = Math.Max(0, _gameScroll + direction);
        } else if (_listArea.Contains(position.x, position.y)) _scroll = Math.Max(0, _scroll + direction);
    }
    public override void KeyPressed(Keys key) {
        if (key == Keys.Escape) _game.OpenMenu();
        if (key == Keys.Enter && _page == Page.Setup) Start();
    }
    private void Show(Page page) { _page = page; _scroll = _gameScroll = 0; _error = null; if (page == Page.History) ReloadHistory(); }
    private void Select(TournamentState state) { _selected = state; _selectedPair = state.Champion.HasValue ? state.Pairings.Count - 1 : 0; Show(Page.Results); }
    private void ResizeEntrants(int direction) {
        _entrantCount = Cycle(_entrantCount, direction, EntrantCounts.Length);
        var entrants = new int[EntrantCounts[_entrantCount]];
        for (int i = 0; i < entrants.Length; i++) entrants[i] = i < _entrants.Length ? _entrants[i] : i % Math.Max(1, _bots.Count);
        _entrants = entrants; _scroll = 0;
    }
    private static int Cycle(int current, int direction, int count) => (current + direction + count) % count;
    private bool CanStart => _bots.Count > 0 && _game.Tournament?.View().Status != TournamentStatus.Running;
    private void Start() {
        if (!CanStart) return;
        try {
            _game.StartTournament(new TournamentSettings {
                Bots = _entrants.Select(i => _bots[i % _bots.Count].Name).ToList(), GamesPerPairing = GameCounts[_gameCount],
                TimeLimitMilliseconds = Times[_time], MaxPlies = MoveLimits[_moveLimit], Configuration = new GameConfiguration {
                    Variants = _variants.Where(v => _enabledVariants.Contains(v.Id)).Select(v => VariantState.Capture(v.Restore(new() { Kind = v.Id }))).ToList()
                }
            });
        } catch (Exception e) { _error = e.Message; }
    }
    private void Resume() {
        try { _game.StartTournament(_selected.Settings, _selected); }
        catch (Exception e) { _error = e.Message; }
    }
    private void Label(SpriteBatch batch, string text, Rectangle rect, float size = .72f, Color? color = null) =>
        batch.DrawTextLine(SpriteBatchExtensions.FitText(text, _unit * size, rect.Width), new Vector2(rect.X, rect.Y), _unit * size, color ?? Theme.Text);
    private void ActionButton(SpriteBatch batch, string text, Rectangle rect, Action action, bool enabled = true, bool selected = false) {
        batch.DrawRectangle(rect, enabled ? selected ? Theme.Accent : Theme.Button : Theme.PanelLight);
        batch.DrawTextCentered(text, rect, Math.Min(_unit * .8f, rect.Height * .52f), enabled ? Theme.Text : Theme.TextDim);
        if (enabled) _actions.Add((rect, action));
    }
    private void Selector(SpriteBatch batch, string label, string value, Rectangle row, Action decrease, Action increase) {
        int button = row.Height;
        Label(batch, label, new(row.X, row.Y + row.Height / 4, (int)(row.Width * .42), row.Height));
        int x = row.X + (int)(row.Width * .42);
        ActionButton(batch, "<", new(x, row.Y, button, button), decrease);
        batch.DrawTextCentered(value, new(x + button, row.Y, row.Right - x - 2 * button, row.Height), _unit * .75f, Theme.Text);
        ActionButton(batch, ">", new(row.Right - button, row.Y, button, button), increase);
    }
    public override void Draw(SpriteBatch batch) {
        _actions.Clear(); _unit = Game1.ScreenSize.Y / 32f;
        int pad = (int)_unit, row = (int)(_unit * 1.65f), width = Game1.ScreenSize.X - 2 * pad;
        Label(batch, "Tournaments", new(pad, pad / 2, width, row), 1.5f);
        int navY = (int)(_unit * 2.4f), navWidth = Math.Max(100, width / 5 - pad);
        ActionButton(batch, "New tournament", new(pad, navY, navWidth, row), () => Show(Page.Setup), selected: _page == Page.Setup);
        ActionButton(batch, "History", new(pad * 2 + navWidth, navY, navWidth, row), () => Show(Page.History), selected: _page == Page.History);
        var live = _game.Tournament?.View();
        ActionButton(batch, "Active tournament", new(pad * 3 + navWidth * 2, navY, navWidth, row), () => Select(_game.Tournament.View()), live != null);
        ActionButton(batch, "Back to menu", new(Game1.ScreenSize.X - pad - navWidth, navY, navWidth, row), () => _game.OpenMenu());
        var area = new Rectangle(pad, navY + row + pad, width, Game1.ScreenSize.Y - navY - row - 3 * pad);
        if (_page == Page.Setup) DrawSetup(batch, area);
        else if (_page == Page.History) DrawHistory(batch, area);
        else DrawResults(batch, area);
        string message = _error ?? (_page == Page.Results ? _selected?.Error : null) ?? _game.TournamentHistory.LastError;
        if (message != null) Label(batch, message, new(pad, Game1.ScreenSize.Y - pad, width, pad), .58f, Theme.Warning);
    }
    private void DrawSetup(SpriteBatch batch, Rectangle area) {
        int pad = (int)_unit, row = (int)(_unit * 1.6f), gap = (int)(_unit * .35f);
        var left = new Rectangle(area.X, area.Y, (area.Width - pad) / 2, area.Height - row - pad);
        var right = new Rectangle(left.Right + pad, left.Y, area.Right - left.Right - pad, left.Height);
        batch.DrawRectangle(left, Theme.Panel); batch.DrawRectangle(right, Theme.Panel);
        var first = new Rectangle(left.X + pad, left.Y + pad, left.Width - 2 * pad, row);
        Selector(batch, "Entrants", _entrants.Length.ToString(), first, () => ResizeEntrants(-1), () => ResizeEntrants(1));
        _listArea = new(left.X + pad, first.Bottom + pad, left.Width - 2 * pad, left.Bottom - first.Bottom - 3 * pad);
        int visible = Math.Max(1, _listArea.Height / (row + gap));
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _entrants.Length - visible));
        for (int i = _scroll; i < Math.Min(_entrants.Length, _scroll + visible); i++) {
            int slot = i;
            var rect = new Rectangle(_listArea.X, _listArea.Y + (i - _scroll) * (row + gap), _listArea.Width, row);
            Selector(batch, $"Seed #{i + 1}", _bots.Count == 0 ? "No bots" : _bots[_entrants[i] % _bots.Count].Name, rect,
                () => { if (_bots.Count > 0) _entrants[slot] = Cycle(_entrants[slot], -1, _bots.Count); },
                () => { if (_bots.Count > 0) _entrants[slot] = Cycle(_entrants[slot], 1, _bots.Count); });
        }
        Label(batch, "Scroll for more entrants. Duplicate bot entries are allowed.", new(left.X + pad, left.Bottom - pad, left.Width - 2 * pad, pad), .55f, Theme.TextDim);
        var setting = new Rectangle(right.X + pad, right.Y + pad, right.Width - 2 * pad, row);
        Selector(batch, "Games per pairing", GameCounts[_gameCount].ToString(), setting, () => _gameCount = Cycle(_gameCount, -1, GameCounts.Length), () => _gameCount = Cycle(_gameCount, 1, GameCounts.Length));
        setting.Y += row + gap;
        Selector(batch, "Time per move", $"{Times[_time]} ms", setting, () => _time = Cycle(_time, -1, Times.Length), () => _time = Cycle(_time, 1, Times.Length));
        setting.Y += row + gap;
        Selector(batch, "Half-move limit", MoveLimits[_moveLimit].ToString(), setting, () => _moveLimit = Cycle(_moveLimit, -1, MoveLimits.Length), () => _moveLimit = Cycle(_moveLimit, 1, MoveLimits.Length));
        setting.Y += row + gap;
        Label(batch, "Variants (combine any; none = king capture)", setting, .7f, Theme.TextDim);
        _secondaryArea = new(setting.X, setting.Bottom, setting.Width, Math.Max(row, right.Bottom - setting.Bottom - (int)(_unit * 5.7f)));
        int variantRows = Math.Max(1, _secondaryArea.Height / (row + gap));
        _variantScroll = Math.Clamp(_variantScroll, 0, Math.Max(0, _variants.Count - variantRows));
        for (int i = _variantScroll; i < Math.Min(_variants.Count, _variantScroll + variantRows); i++) {
            var variant = _variants[i]; bool enabled = _enabledVariants.Contains(variant.Id);
            ActionButton(batch, (enabled ? "[x] " : "[ ] ") + variant.Name,
                new(_secondaryArea.X, _secondaryArea.Y + (i - _variantScroll) * (row + gap), _secondaryArea.Width, row),
                () => { if (!_enabledVariants.Remove(variant.Id)) _enabledVariants.Add(variant.Id); }, selected: enabled);
        }
        int y = right.Bottom - (int)(_unit * 5.3f);
        foreach (string line in new[] { "Each opening is played with both colors.", "First pair: start position. Then: 3-move book openings.", "Win = 1 point. Draw = 0.5. Loss = 0.", "Ties: extra pairs, 20% less time each pair (minimum 1 ms).", "Advance after scoring at least 1.5 points in a tiebreak pair." }) {
            Label(batch, line, new(right.X + pad, y, right.Width - 2 * pad, pad), .59f, Theme.TextDim); y += pad;
        }
        ActionButton(batch, CanStart ? "Start tournament" : "Pause the active tournament to start another",
            new(area.X, area.Bottom - row, area.Width, row), Start, CanStart, selected: CanStart);
    }
    private void DrawHistory(SpriteBatch batch, Rectangle area) {
        batch.DrawRectangle(area, Theme.Panel); int pad = (int)_unit;
        Label(batch, _opening != null ? "Opening tournament..." : _loading != null ? "Loading tournament history..." : "Previous tournaments - click to see the bracket and games", new(area.X + pad, area.Y + pad, area.Width - 2 * pad, pad), .8f);
        _listArea = new(area.X + pad, area.Y + 3 * pad, area.Width - 2 * pad, area.Height - 4 * pad); _secondaryArea = Rectangle.Empty;
        int row = (int)(_unit * 3), visible = Math.Max(1, _listArea.Height / row);
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _history.Count - visible));
        if (_loading == null && _history.Count == 0) Label(batch, "No tournaments yet. Choose New tournament to start one.", _listArea, .85f, Theme.TextDim);
        for (int i = _scroll; i < Math.Min(_history.Count, _scroll + visible); i++) {
            var state = _history[i]; var rect = new Rectangle(_listArea.X, _listArea.Y + (i - _scroll) * row, _listArea.Width, row - 5);
            batch.DrawRectangle(rect, Theme.PanelLight); _actions.Add((rect, () => OpenArchive(state.Id)));
            string status = state.Champion != null ? "Winner: " + state.Champion : state.Status == TournamentStatus.Running
                ? _game.Tournament?.View().Id == state.Id ? "Running" : "Interrupted - resumable" : state.Status.ToString();
            Label(batch, $"{state.Started.ToLocalTime():yyyy-MM-dd HH:mm}  |  {state.Entrants} entrants  |  {status}", new(rect.X + pad, rect.Y + 5, rect.Width - 2 * pad, pad), .8f);
            Label(batch, $"{state.Configuration}  |  {state.GamesPerPairing} games per pairing  |  {state.TimeLimitMilliseconds} ms/move", new(rect.X + pad, rect.Y + (int)(_unit * 1.4f), rect.Width - 2 * pad, pad), .65f, Theme.TextDim);
        }
    }
    private void DrawResults(SpriteBatch batch, Rectangle area) {
        if (_selected == null) return;
        int pad = (int)_unit, row = (int)(_unit * 1.65f);
        string title = _selected.Champion.HasValue ? "Champion: " + _selected.EntrantName(_selected.Champion.Value) : "Tournament: " + _selected.Status;
        Label(batch, title, new(area.X, area.Y, area.Width, row), 1.1f, Theme.Good);
        string roundPath = string.Join(" > ", Enumerable.Range(0, (int)Math.Log2(_selected.Settings.Bots.Count)).Select(_selected.RoundName));
        Label(batch, $"{roundPath}  |  {_selected.Settings.Configuration.Description}", new(area.X, area.Y + row, area.Width, row), .72f, Theme.TextDim);
        var left = new Rectangle(area.X, area.Y + 2 * row + pad, (int)(area.Width * .43), area.Height - 3 * row - 2 * pad);
        var right = new Rectangle(left.Right + pad, left.Y, area.Right - left.Right - pad, left.Height);
        batch.DrawRectangle(left, Theme.Panel); batch.DrawRectangle(right, Theme.Panel);
        _listArea = new(left.X + pad, left.Y + pad, left.Width - 2 * pad, left.Height - 2 * pad);
        int pairRow = (int)(_unit * 3.1), visible = Math.Max(1, _listArea.Height / pairRow);
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _selected.Pairings.Count - visible));
        _selectedPair = Math.Clamp(_selectedPair, 0, Math.Max(0, _selected.Pairings.Count - 1));
        for (int i = _scroll; i < Math.Min(_selected.Pairings.Count, _scroll + visible); i++) {
            int index = i; var p = _selected.Pairings[i];
            var rect = new Rectangle(_listArea.X, _listArea.Y + (i - _scroll) * pairRow, _listArea.Width, pairRow - 5);
            batch.DrawRectangle(rect, index == _selectedPair ? Theme.Accent : Theme.PanelLight);
            _actions.Add((rect, () => { _selectedPair = index; _gameScroll = 0; }));
            var active = _game.Tournament?.View().Id == _selected.Id ? _game.Tournament.MatchForPairing(p.Number) : null;
            string result = p.Winner.HasValue ? "Winner: " + _selected.EntrantName(p.Winner.Value) : active != null
                ? $"Playing game {Math.Max(1, active.Games.Count)} of {active.Settings.Games} ({active.Settings.TimeLimitMilliseconds} ms/move)"
                : p.Match != null ? "In progress" : "Waiting";
            Label(batch, $"{_selected.RoundName(p.Round)}  |  {p.Statistics.ScoreA:0.#} : {p.Statistics.ScoreB:0.#}", new(rect.X + 5, rect.Y + 4, rect.Width - 10, pad), .7f);
            Label(batch, $"{_selected.EntrantName(p.EntrantA)} vs {_selected.EntrantName(p.EntrantB)}", new(rect.X + 5, rect.Y + pad, rect.Width - 10, pad), .65f);
            Label(batch, result, new(rect.X + 5, rect.Y + 2 * pad, rect.Width - 10, pad), .57f);
        }
        if (_selected.Pairings.Count > 0) DrawPairing(batch, right, _selected.Pairings[_selectedPair]);
        bool isLive = _game.Tournament?.View().Id == _selected.Id;
        bool running = isLive && _selected.Status == TournamentStatus.Running;
        var footer = new Rectangle(area.X, area.Bottom - row, area.Width, row);
        ActionButton(batch, running ? "Pause tournament (save progress)" : _selected.Status == TournamentStatus.Completed ? "Tournament complete" : "Resume tournament",
            footer, running ? () => _game.Tournament.Pause() : Resume,
            running || (_selected.Status != TournamentStatus.Completed && CanStart));
    }
    private void DrawPairing(SpriteBatch batch, Rectangle area, TournamentPairing pairing) {
        int pad = (int)_unit, row = (int)(_unit * 1.5f), x = area.X + pad, width = area.Width - 2 * pad, y = area.Y + pad;
        var stats = pairing.Statistics;
        foreach (string line in new[] {
            $"Pairing {pairing.Number}: {stats.ScoreA:0.#} : {stats.ScoreB:0.#} points",
            $"A: W/D/L {stats.WinsA}/{stats.Draws}/{stats.WinsB}  |  {_selected.EntrantName(pairing.EntrantA)}",
            $"B: W/D/L {stats.WinsB}/{stats.Draws}/{stats.WinsA}  |  {_selected.EntrantName(pairing.EntrantB)}",
            $"{stats.Games} finished games; White wins {stats.WhiteWins}, Black wins {stats.BlackWins}",
            $"Avg. played half-moves: {stats.AveragePlies:0.0}   Think A/B: {stats.AverageThinkA:0.0}/{stats.AverageThinkB:0.0} ms",
            pairing.Decision ?? "Waiting for a decisive score"
        }) { Label(batch, line, new(x, y, width, pad), .65f); y += pad; }
        var reason = stats.EndReasons.OrderByDescending(p => p.Value).Take(2).Select(p => $"{p.Value}x {p.Key}");
        Label(batch, string.Join("; ", reason), new(x, y, width, pad), .57f, Theme.TextDim); y += row;
        var activeMatch = _game.Tournament?.View().Id == _selected.Id ? _game.Tournament.MatchForPairing(pairing.Number) : null;
        bool live = activeMatch != null;
        ActionButton(batch, "Watch / analyse pairing", new(x, y, width, row), () => _game.OpenTournamentPairing(_selected, pairing), live || pairing.Match?.Games.Count > 0); y += row + pad / 2;
        Label(batch, "Games - click for replay (scroll for more)", new(x, y, width, pad), .7f, Theme.TextDim); y += row;
        _secondaryArea = new(x, y, width, Math.Max(0, area.Bottom - pad - y));
        var games = activeMatch?.Games;
        var saved = pairing.Match?.Games;
        int count = games?.Count ?? saved?.Count ?? 0, gameRow = (int)(_unit * 2.1), visible = Math.Max(1, _secondaryArea.Height / gameRow);
        _gameScroll = Math.Clamp(_gameScroll, 0, Math.Max(0, count - visible));
        for (int i = _gameScroll; i < Math.Min(count, _gameScroll + visible); i++) {
            int index = i;
            var liveGame = games?[i]; var savedGame = saved != null && i < saved.Count ? saved[i] : null;
            int number = liveGame?.Number ?? savedGame.Number;
            string whiteName = liveGame?.WhiteName ?? savedGame.WhiteName, blackName = liveGame?.BlackName ?? savedGame.BlackName;
            var gameResult = liveGame?.Result ?? savedGame.Result;
            string openingName = liveGame?.OpeningName ?? savedGame.OpeningName;
            int milliseconds = liveGame?.TimeLimitMilliseconds ?? savedGame.TimeLimitMilliseconds;
            var rect = new Rectangle(x, y + (i - _gameScroll) * gameRow, width, gameRow - 4);
            batch.DrawRectangle(rect, Theme.PanelLight); _actions.Add((rect, () => _game.OpenTournamentPairing(_selected, pairing, index)));
            string result = gameResult switch { GameResult.WhiteWins => "1-0", GameResult.BlackWins => "0-1", GameResult.Draw => "0.5-0.5", _ => "..." };
            Label(batch, $"{number}. {result}   {whiteName} - {blackName}", new(x + 5, rect.Y + 3, width - 10, pad), .6f);
            Label(batch, $"{openingName}  |  {milliseconds} ms/move", new(x + 5, rect.Y + pad, width - 10, pad), .55f, Theme.TextDim);
        }
    }
}
