// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// ⭐ THE SECOND ROUND OF THE QUESTION "IS THIS FIGURATIVE ZERO AN ARITHMETIC OPERAND?", ASKED OF THE GRAMMAR (kb/Work
/// PB3981, PB4120). <see cref="ZeroTokenRewriter.Rewrite"/> decides from the source characters, before any parse, by the
/// ZERO's neighbours: an arithmetic operator, the ref-mod colon or a paren pair around it alone. Two positions are
/// arithmetic expressions that no neighbour can show, because the position is the grammar's:
/// <list type="bullet">
///   <item><b>A figurative ZERO followed by a sign touching its literal</b> (<c>ZERO -1</c>). The rewriter's answer is the
///         OPERAND-LIST reading (<see cref="ZeroTokenRewriter"/>'s <c>IsSignOfNextLiteral</c>): the ZERO stays figurative
///         and the <c>-1</c> is the next operand. That is right in every operand list (<c>DISPLAY ZERO -1</c>) and wrong
///         where the grammar puts an ARITHMETIC EXPRESSION (<c>COMPUTE X = ZERO -1</c>, <c>IF X = ZERO -1</c>): there the
///         same characters are a subtraction written without its second space, which §8.7.1 refuses by name
///         (<c>COBOL2993</c>, <c>SeparatorRule</c>) exactly as it refuses the sibling <c>Y -1</c>. The parse shows it by
///         failing at the SIGN (kb/Work PB3981).</item>
///   <item><b>A BARE figurative ZERO in a slot that takes only an arithmetic expression</b> (<c>SET IX UP BY ZERO</c>,
///         <c>SET IX TO ZERO</c>, <c>CONTINUE AFTER ZERO SECONDS</c>, <c>ALLOCATE ZERO CHARACTERS</c>). ISO §8.8.1.1: "An
///         arithmetic expression may be [...] the figurative constant ZERO (ZEROS, ZEROES)", and the expression grammar's
///         only figurative arm is <c>ZERO_ARITH</c>, so the figurative token is refused in such a slot and the parse fails
///         AT the ZERO (kb/Work PB4120). A slot whose rule also admits the figurative (a MOVE sender, a VALUE operand, a
///         relation operand) parses the figurative and never reaches this round, so the figurative keeps its meaning
///         there (§8.3.3.6.4 GR4: the value 0 or the character '0' "depending on context").</item>
/// </list>
/// Which slots those are is never listed: the parse that fails is the grammar's own answer, so the NEXT slot typed
/// arithmetic-expression-only is covered without a change here.
/// <para>One instance serves one whole-group parse (<see cref="ReservationGateRewriter.ParseToFixpoint"/>). After each
/// pass <see cref="Revise"/> takes the pass's syntax-error offenders: a figurative ZERO offender, or a <c>+</c> or
/// <c>-</c> offender that stands right after a figurative ZERO, turns that ZERO into <c>ZERO_ARITH</c>, the operand of
/// an arithmetic expression, and the caller parses again. The arithmetic reading is a HYPOTHESIS: if the next pass
/// still fails at that ZERO or at the sign that led to it (<c>MOVE ZERO -1 TO X</c>, <c>MOVE X TO ZERO</c>, which no
/// reading makes legal), the ZERO goes back to its figurative token and is never tried again, so a source that is wrong
/// either way reports the figurative reading's error. A hypothesis made from the ZERO itself is refuted only at the
/// ZERO: a later failure at the token after it means the ZERO was taken, and the fault lies further on.</para>
/// </summary>
internal sealed class ZeroSignReading(CommonTokenStream tokenStream)
{
    /// <summary>A ZERO read as arithmetic on trial: the figurative token it replaced, and the sign whose refusal led to the
    /// trial (-1 when the refused token was the ZERO itself), the other token a failure at refutes it.</summary>
    private readonly record struct Trial(IToken Figurative, int Sign);

    private readonly Dictionary<int, Trial> _tried = [];   // ZERO token index -> its trial
    private readonly HashSet<int> _refuted = [];

    /// <summary>Adjusts the stream to the pass that just ran. Returns whether any token changed, in which case the
    /// stream is rewound and the caller parses again; false ends the caller's loop for this decision.</summary>
    public bool Revise(IReadOnlyList<IToken> offenders)
    {
        var tokens = tokenStream.GetTokens();
        bool changed = false;
        foreach (var (zero, trial) in _tried.ToList())
        {
            if (!offenders.Any(o => o.TokenIndex == zero || trial.Sign >= 0 && o.TokenIndex == trial.Sign)) continue;
            tokens[zero] = trial.Figurative;
            _tried.Remove(zero);
            _refuted.Add(zero);
            changed = true;
        }
        foreach (var offender in offenders)
        {
            int at = offender.TokenIndex;
            if ((uint)at >= (uint)tokens.Count) continue;
            if (ZeroTokenRewriter.IsFigurativeZero(tokens[at]))
                changed |= Try(tokens, at, sign: -1);
            else if (offender.Type is CobolLexer.PLUS or CobolLexer.MINUS
                     && ZeroTokenRewriter.AdjacentIndex(tokens, at, forward: false) is var zero and >= 0
                     && ZeroTokenRewriter.IsFigurativeZero(tokens[zero]))
                changed |= Try(tokens, zero, sign: at);
        }
        if (changed) tokenStream.Seek(0);
        return changed;
    }

    /// <summary>Put the figurative ZERO at <paramref name="zero"/> on trial as an arithmetic operand, unless an earlier
    /// pass refuted that reading or it is already on trial. Returns whether the stream changed.</summary>
    private bool Try(IList<IToken> tokens, int zero, int sign)
    {
        if (_refuted.Contains(zero) || _tried.ContainsKey(zero)) return false;
        _tried[zero] = new Trial(tokens[zero], sign);
        ZeroTokenRewriter.ReplaceWithZeroArith(tokens, zero);
        return true;
    }
}
