// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
}
