// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Misc;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// The token types that ARE a literal of class alphanumeric, boolean or national: the four DEFAULT-mode literal tokens
/// and their SUBSCRIPT-mode twins. ONE set, read by both rules that are asked of every literal token — the literal's
/// own §8.3.3 syntax rules (<c>LiteralScreen</c>) and its §8.3.5 5) delimiter separation
/// (<see cref="SeparatorRule"/>). Derived-and-pinned: <c>LiteralScreenDriftTests</c> reads the lexer grammar and
/// requires this set to equal the tokens defined over a literal body fragment, so a new literal token joins both
/// rules or fails there.
/// </summary>
public static class LiteralTokens
{
    /// <summary>Every literal token type the lexer emits for a delimited (quoted) literal.</summary>
    public static IReadOnlySet<int> Types { get; } = new HashSet<int>
    {
        CobolLexer.STRINGLIT, CobolLexer.HEXLIT, CobolLexer.NATLIT, CobolLexer.BOOLLIT,
    };

    /// <summary>Every token type that IS a floating-point numeric literal (ISO §8.3.3.3.3): the dot-decimal
    /// <c>FLOATLIT</c> and its DECIMAL-POINT IS COMMA twin <c>COMMA_FLOATLIT</c>. The sign-adjacent lexer twins
    /// (<c>FN_SIGNED_FLOATLIT</c>, <c>FN_SIGNED_COMMA_FLOATLIT</c>) are RE-TYPED to these two by the lexer action
    /// <c>OnSignedLiteral</c>, so the parser, and the screen, only ever see these. Read by <c>LiteralScreen</c> for the
    /// literal FORMAT's edition gate (the floating-point-literal-2002 construct, kb/Work PB674); a separate set from
    /// <see cref="Types"/> because those are the DELIMITED literals, whose length, grouping, repertoire and separation
    /// rules do not apply to a numeric literal. <c>LiteralScreenDriftTests</c> derives it from the lexer grammar
    /// (every token defined over a <c>FLOAT*_BODY</c> fragment).</summary>
    public static IReadOnlySet<int> FloatingPointTypes { get; } = new HashSet<int>
    {
        CobolLexer.FLOATLIT, CobolLexer.COMMA_FLOATLIT,
    };

    /// <summary>Every token that is a WHOLE literal by itself, of any category: FIRST(<c>residueLiteral</c>), which is
    /// every one-token literal but the unsigned integer, and the unsigned integer — together every single-token
    /// alternative of <c>nonNumericLiteral</c> and <c>numericLiteralCore</c> (<c>ClosedFormatDriftTests</c> holds the
    /// equality). Read from the ATN once: the set the parse-error hints ask "is this token a literal?" of.</summary>
    public static IntervalSet Whole => _whole ??= BuildWhole();

    private static IntervalSet? _whole;

    private static IntervalSet BuildWhole()
    {
        var atn = CobolParserCore._ATN;
        var set = new IntervalSet();
        set.AddAll(atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_residueLiteral]));
        set.Add(CobolLexer.INTEGERLIT);
        set.SetReadonly(true);
        return set;
    }

    /// <summary>The category of the literal token <paramref name="type"/> as ISO §8.3.3.1 names it ("Each literal
    /// possesses a class and category: alphanumeric, boolean, national, or numeric"), a numeric one by its §8.3.3.3
    /// form (the integer literal of §8.3.3.3.2, a fixed-point literal with a decimal point, a floating-point literal),
    /// as a noun phrase without its article; null for a token that is no literal. ⛔ Every token of
    /// <see cref="Whole"/> has one (<c>WrittenKindRefusalTests.EveryWholeLiteralToken_HasACategory</c>), so a literal
    /// token the lexer gains tomorrow is red until it is named here (kb/Work PB3861).</summary>
    public static string? CategoryOf(int type) => type switch
    {
        CobolLexer.INTEGERLIT or CobolLexer.SIGNED_INTEGERLIT => "integer literal",
        CobolLexer.DECIMALLIT or CobolLexer.SIGNED_DECIMALLIT => "fixed-point numeric literal",
        CobolLexer.FLOATLIT or CobolLexer.COMMA_FLOATLIT => "floating-point numeric literal",
        CobolLexer.STRINGLIT or CobolLexer.HEXLIT => "alphanumeric literal",
        CobolLexer.NATLIT => "national literal",
        CobolLexer.BOOLLIT => "boolean literal",
        _ => null,
    };
}
