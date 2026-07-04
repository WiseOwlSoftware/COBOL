// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Validation;

/// <summary>
/// The per-edition VALIDATION pass (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.2) — the
/// syntax-side half of the four-compilers-in-one obligation: every construct carries (1) its full ISO behavior in
/// every edition that HAS it and (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced,
/// COBOLNET0900), reserves its spelling (0901), removed it (0902), or obsoleted it (0903 — see
/// <see cref="EditionCodes"/>). The validator walks the RAW parse tree — syntax-only gating lives here; gating
/// that needs bind/type information (e.g. the MOVE rows) stays binder-side — but EVERY severity decision routes
/// through <see cref="EditionContext.Removed"/> / the construct registry: one policy, several emit sites.
/// </summary>
/// <remarks>
/// The walk derives from the generated <see cref="CobolParserCoreBaseVisitor{Result}"/> (ANTLR runs
/// <c>-no-listener -visitor</c>, so no listener exists to attach to); overrides MUST return
/// <c>base.VisitChildren(ctx)</c> (or <c>base.VisitXxx(ctx)</c>) to keep descending. Hooked by
/// <see cref="CompilerDriver.Compile"/> between <see cref="EditionContext"/> construction and
/// <c>CSharpEmitter.Emit</c>, with a fail-fast on <see cref="EditionContext.HasErrors"/> BEFORE Emit — a
/// removed or not-yet-introduced construct may have no emit path at all. Validator diagnostics ride the SAME
/// <see cref="EditionContext"/> channels as binder gating (no separate outcome kind).
/// The Wave-1 construct gates (P2.6) and the §8.9 reserved-word funnel (P2.4 — <c>VisitCobolWord</c>) land on
/// this skeleton in their own change sets, each with its VERSION_CHANGE_REFERENCE row and ISO § citation.
/// </remarks>
public sealed class EditionValidator(EditionContext edition) : CobolParserCoreBaseVisitor<object?>
{
    private readonly EditionContext _edition = edition;
    // The effective reserved-word set for THIS compilation unit (P2.4/D9 seam): the generated §8.9 table is
    // only the default layer — the 2023 COBOL-WORDS directive mutates the set per unit (roadmap Phase 7).
    private readonly ReservedWordSet _reservedWords = ReservedWordSet.Default;
    // One COBOLNET0901 per distinct word per compilation (P2.4) — not one per occurrence.
    private HashSet<string>? _flaggedWords;

    /// <summary>Run the pass over a parsed compilation unit, recording diagnostics on the
    /// <see cref="EditionContext"/> passed at construction.</summary>
    public void Validate(CobolParserCore.CompilationUnitContext tree) => Visit(tree);

    // ── P2.6 removal gates: every override routes through ConstructRegistry.Check (one policy) ─────────────

    /// <summary>LABEL RECORDS (FD) — obsolete '85 element DELETED by ISO/IEC 1989:2002; the 2023 FD clause set
    /// (§13.18) has no LABEL clause. The FIRST removal gate, shipped in the SAME commit as the permissive flip
    /// (every NIST FD writes this clause — 243/459 programs).</summary>
    public override object? VisitLabelRecordsClause(CobolParserCore.LabelRecordsClauseContext ctx)
    {
        ConstructRegistry.Check(_edition, "label-records-removed-2002", "the FD LABEL RECORDS clause");
        return base.VisitChildren(ctx);
    }

    // Which cobolWord token TYPES the funnel checks (P2.4, refined — DEVLOG 585): IDENTIFIER occurrences are
    // ALWAYS genuine words (the lexer didn't tokenize them), and they carry the whole newly-reserved payload
    // (the Annex-E 2023 additions lex as IDENTIFIER). The six EC-band tokens are ALSO checked — they are
    // §8.9-reserved at 2023 and their keyword uses parse through dedicated statement rules, never a name slot.
    // The REMAINING allowlisted tokens (the screen/report band: COL, COLUMN, AUTO, …) are EXCLUDED for now:
    // the permissive grammar can bind their KEYWORD occurrences into optional entry-name slots (RW104A binds
    // the report COLUMN clause's keyword into the report-group entry-name slot), so a position-blind check
    // false-rejects conforming CCVS-85 programs. Their per-edition enforcement needs position-aware checking —
    // parked to the W2 adversarial review (P2.8).
    private static readonly HashSet<int> CheckedTokenTypes =
    [
        CobolLexer.IDENTIFIER,
        CobolLexer.RAISE, CobolLexer.RAISING, CobolLexer.RESUME,
        CobolLexer.CONDITION, CobolLexer.EC, CobolLexer.STATEMENT,
    ];

    /// <summary>
    /// The §8.9 reserved-word funnel (P2.4): every user-defined word reaches the tree through the
    /// <c>cobolWord</c> rule — IDENTIFIER plus the allowlisted context-keyword tokens — so ONE text-based check
    /// here covers 2023-new words that lex as IDENTIFIER (COMMIT, FINALLY, …) AND the EC-band tokens the 2023
    /// edition reserves (RAISE/RAISING/RESUME/CONDITION/EC). The grammar stays a permissive superset ("legal
    /// user word at every edition"); the VALIDATOR enforces per edition — restricted to
    /// <see cref="CheckedTokenTypes"/> (see the note there). Only high-confidence rows reject
    /// (<see cref="ReservedWordSet.RejectsAt"/> — the conservative policy); severity routes through
    /// <see cref="EditionContext.Removed"/> (error strict / warning permissive, the 0901 band row).
    /// </summary>
    public override object? VisitCobolWord(CobolParserCore.CobolWordContext ctx)
    {
        if (!CheckedTokenTypes.Contains(ctx.Start.Type))
            return base.VisitChildren(ctx);
        string word = ctx.Start.Text.ToUpperInvariant();
        if (_reservedWords.RejectsAt(word, _edition.DialectLevel) && (_flaggedWords ??= []).Add(word))
            _edition.Removed(EditionCodes.ReservedWord,
                $"'{word}' is a reserved word in COBOL-{_edition.DialectLevel} and cannot be used as a "
                + "user-defined word (ISO §8.9)");
        return base.VisitChildren(ctx);
    }
}
