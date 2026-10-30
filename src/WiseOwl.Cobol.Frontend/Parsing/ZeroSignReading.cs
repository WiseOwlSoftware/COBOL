// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⭐ THE SECOND ROUND OF THE SIGN-VERSUS-OPERATOR QUESTION, ASKED OF THE GRAMMAR (kb/Work PB3981).
/// <see cref="ZeroTokenRewriter.Rewrite"/> decides from the source characters, before any parse, and its answer for a
/// figurative ZERO followed by a sign touching its literal (<c>ZERO -1</c>) is the OPERAND-LIST reading
/// (<see cref="ZeroTokenRewriter"/>'s <c>IsSignOfNextLiteral</c>): the ZERO stays figurative and the <c>-1</c> is the next
/// operand. That is the right reading in every operand list (<c>DISPLAY ZERO -1</c>) and the wrong one where the grammar
/// puts an ARITHMETIC EXPRESSION (<c>COMPUTE X = ZERO -1</c>, <c>IF X = ZERO -1</c>): there the same characters are a
/// subtraction written without its second space, which §8.7.1 refuses by name (<c>COBOL2993</c>,
/// <c>SeparatorRule</c>) exactly as it refuses the sibling <c>Y -1</c>. No token test can tell the two apart, because
/// the position is the grammar's, so the parse tells: an operand-list reading that FAILS to parse at the sign (or at
/// the ZERO) has met an expression slot.
/// <para>One instance serves one whole-group parse (<see cref="ReservationGateRewriter.ParseToFixpoint"/>). After each
/// pass <see cref="Revise"/> takes the pass's syntax-error offenders: a <c>+</c> or <c>-</c> offender that stands right
/// after a figurative ZERO turns that ZERO into <c>ZERO_ARITH</c>, the operand of an operator, and the caller parses
/// again. The arithmetic reading is a HYPOTHESIS: if the next pass still fails at that ZERO or its sign (<c>MOVE ZERO -1
/// TO X</c>, which no reading makes legal), the ZERO goes back to its figurative token and is never tried again, so a
/// source that is wrong either way reports the operand-list reading's error, at the sign.</para>
/// </summary>
internal sealed class ZeroSignReading(CommonTokenStream tokenStream)
{
    private readonly Dictionary<int, IToken> _tried = [];   // ZERO token index -> the figurative token it replaced
    private readonly HashSet<int> _refuted = [];

    /// <summary>Adjusts the stream to the pass that just ran. Returns whether any token changed, in which case the
    /// stream is rewound and the caller parses again; false ends the caller's loop for this decision.</summary>
    public bool Revise(IReadOnlyList<IToken> offenders)
    {
        var tokens = tokenStream.GetTokens();
        bool changed = false;
        foreach (var (zero, figurative) in _tried.ToList())
        {
            int sign = ZeroTokenRewriter.AdjacentIndex(tokens, zero, forward: true);
            if (!offenders.Any(o => o.TokenIndex == zero || o.TokenIndex == sign)) continue;
            tokens[zero] = figurative;
            _tried.Remove(zero);
            _refuted.Add(zero);
            changed = true;
        }
        foreach (var offender in offenders)
        {
            int sign = offender.TokenIndex;
            if (offender.Type is not (CobolLexer.PLUS or CobolLexer.MINUS) || (uint)sign >= (uint)tokens.Count) continue;
            int zero = ZeroTokenRewriter.AdjacentIndex(tokens, sign, forward: false);
            if (zero < 0 || !ZeroTokenRewriter.IsFigurativeZero(tokens[zero]) || _refuted.Contains(zero)) continue;
            _tried[zero] = tokens[zero];
            ZeroTokenRewriter.ReplaceWithZeroArith(tokens, zero);
            changed = true;
        }
        if (changed) tokenStream.Seek(0);
        return changed;
    }
}
