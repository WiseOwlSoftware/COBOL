// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>The four mutually-exclusive options of a <c>&gt;&gt;COBOL-WORDS</c> directive (ISO §7.3.10.2).</summary>
public enum CobolWordsAction
{
    /// <summary>EQUATE literal-1 WITH literal-2 — literal-2 becomes a synonym for literal-1 (GR2).</summary>
    Equate,
    /// <summary>UNDEFINE literal-3 — literal-3 is no longer reserved/restricted (GR3).</summary>
    Undefine,
    /// <summary>SUBSTITUTE literal-4 BY literal-5 — literal-5 takes over literal-4's role; literal-4 becomes a
    /// user word (GR4).</summary>
    Substitute,
    /// <summary>RESERVE literal-6 — literal-6 shall not be used as a user-defined word (GR5).</summary>
    Reserve,
}

/// <summary>
/// One parsed <c>&gt;&gt;COBOL-WORDS</c> operation (ISO §7.3.10). Both words are stored UPPER-CASE (SR2:
/// case-insensitive) with the enclosing literal quotes stripped. <see cref="Existing"/> is the
/// reserved/context-sensitive/intrinsic word SR3 governs (present for EQUATE/UNDEFINE/SUBSTITUTE);
/// <see cref="New"/> is the fresh user-defined word SR4 governs (present for EQUATE/SUBSTITUTE/RESERVE).
/// </summary>
/// <param name="Action">Which option.</param>
/// <param name="Existing">literal-1 (EQUATE) / literal-3 (UNDEFINE) / literal-4 (SUBSTITUTE); null for RESERVE.</param>
/// <param name="New">literal-2 (EQUATE) / literal-5 (SUBSTITUTE) / literal-6 (RESERVE); null for UNDEFINE.</param>
/// <param name="Line">The directive's 0-based line index in the final preprocessed text (diagnostics anchor).</param>
public sealed record CobolWordsOp(CobolWordsAction Action, string? Existing, string? New, int Line);

/// <summary>
/// The per-compilation-group <c>&gt;&gt;COBOL-WORDS</c> override layer (ISO §7.3.10; Annex D.12; Annex E.3.3
/// item 12) — the ONE runtime carrier the owner's recorded direction names: built in the Frontend
/// (<c>CobolWordsDirectiveProcessor</c>) from the directive text, then consulted by the post-lex
/// <c>CobolWordsRewriter</c> (token retyping), the map-aware lexer data-name gate, the composed
/// <see cref="ReservedWordSet"/> (RESERVE/UNDEFINE/SUBSTITUTE), and the binder's intrinsic-function-name
/// resolution. A pure-string data carrier so it crosses the frontend→compiler boundary like
/// <c>FlagState</c>/<c>RefModZeroLengthState</c>. <see cref="Empty"/> ⇒ every consumer is a no-op (the
/// zero-overhead invariant). Design SSOT: <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.
/// </summary>
public sealed class CobolWordsMap
{
    /// <summary>The no-directive map — every consumer short-circuits on <see cref="IsEmpty"/>.</summary>
    public static readonly CobolWordsMap Empty = new([]);

    private readonly Dictionary<string, string> _synonyms;
    private readonly HashSet<string> _deReserved;
    private readonly HashSet<string> _reserved;

    public CobolWordsMap(IReadOnlyList<CobolWordsOp> ops)
    {
        Ops = ops;
        _synonyms = new(StringComparer.OrdinalIgnoreCase);
        _deReserved = new(StringComparer.OrdinalIgnoreCase);
        _reserved = new(StringComparer.OrdinalIgnoreCase);
        foreach (var op in ops)
        {
            switch (op.Action)
            {
                case CobolWordsAction.Equate when op is { Existing: { } e, New: { } n }:
                    _synonyms[n] = e;   // the new word acts as the existing word
                    break;
                case CobolWordsAction.Substitute when op is { Existing: { } e, New: { } n }:
                    _synonyms[n] = e;   // the new word takes over the existing word's role
                    _deReserved.Add(e); // and the existing word becomes a user word
                    break;
                case CobolWordsAction.Undefine when op.Existing is { } e:
                    _deReserved.Add(e);
                    break;
                case CobolWordsAction.Reserve when op.New is { } n:
                    _reserved.Add(n);
                    break;
            }
        }
    }

    /// <summary>The parsed operations, in source order (SR3/SR4 validation + the token rewriter walk these).</summary>
    public IReadOnlyList<CobolWordsOp> Ops { get; }

    /// <summary>True when no <c>&gt;&gt;COBOL-WORDS</c> directive was present — every consumer short-circuits.</summary>
    public bool IsEmpty => Ops.Count == 0;

    /// <summary>A fresh user word (EQUATE literal-2 / SUBSTITUTE literal-5) → the existing word it stands in for
    /// (literal-1 / literal-4). The identifier→keyword rewriter and the intrinsic-synonym binder read this.</summary>
    public IReadOnlyDictionary<string, string> Synonyms => _synonyms;

    /// <summary>Words that LOSE their reserved/context/intrinsic status for this group (UNDEFINE literal-3 +
    /// SUBSTITUTE literal-4): the keyword→identifier rewriter, the <see cref="ReservedWordSet"/> suppress set,
    /// and the map-aware lexer data-name gate read this.</summary>
    public IReadOnlySet<string> DeReserved => _deReserved;

    /// <summary>Words newly reserved for this group (RESERVE literal-6): the <see cref="ReservedWordSet"/> reserve
    /// overlay reads this so a use as a user-defined word is COBOLNET0901-rejected.</summary>
    public IReadOnlySet<string> Reserved => _reserved;
}
