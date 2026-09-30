using DChess.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DChess.Chess.Variants;

/// <summary>One registration supplies selection, construction and persistence for every game mode.</summary>
public sealed record VariantDefinition(string Id, string Name, Type Type,
    Func<VariantState, Variant> Restore, Func<Variant, VariantState> Capture);

public static class VariantRegistry {
    private static readonly List<VariantDefinition> _definitions = new() {
        new("promotion", "Queen promotion", typeof(VariantPawnQueenPromotion), _ => new VariantPawnQueenPromotion(), _ => new() { Kind = "promotion" }),
        new("castling", "Castling", typeof(VariantCastling), s => new VariantCastling(s.Parameter == 0 ? 2 : s.Parameter), v => new() { Kind = "castling", Parameter = ((VariantCastling)v).CastlingDistance }),
        new("friendlyfire", "Friendly fire", typeof(VariantFriendlyFire), _ => new VariantFriendlyFire(), _ => new() { Kind = "friendlyfire" }),
        new("battleroyale", "Battle royale", typeof(VariantBattleRoyale), s => new VariantBattleRoyale(s.Parameter == 0 ? 15 : s.Parameter, s.Strength == 0 ? 1 : s.Strength), v => new() { Kind = "battleroyale", Parameter = ((VariantBattleRoyale)v).Interval, Strength = ((VariantBattleRoyale)v).Strength })
    };
    public static IReadOnlyList<VariantDefinition> All => _definitions.ToArray();
    public static void Register(VariantDefinition definition) {
        if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Name) ||
            definition.Type == null || !typeof(Variant).IsAssignableFrom(definition.Type) || definition.Type.IsAbstract ||
            definition.Restore == null || definition.Capture == null ||
            _definitions.Any(d => string.Equals(d.Id, definition.Id, StringComparison.OrdinalIgnoreCase) || d.Type == definition.Type))
            throw new ArgumentException("Variant IDs and types must be unique.");
        _definitions.Add(definition);
    }
    public static VariantState Capture(Variant variant) {
        var definition = _definitions.SingleOrDefault(d => d.Type == variant.GetType()) ?? throw new ArgumentException($"Register variant {variant.GetType().Name} before saving it.");
        var state = definition.Capture(variant);
        state.Kind = definition.Id;
        return state;
    }
    public static Variant Restore(VariantState state) =>
        (_definitions.SingleOrDefault(d => string.Equals(d.Id, state.Kind, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException($"Unknown variant '{state.Kind}'.")).Restore(state);
}
