// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// WHY an expression is being bound, which decides whether an ALPHANUMERIC operand is admissible in it (DA6).
/// <para>
/// The COBOL grammar production is named <c>arithmeticExpression</c> but is REUSED as the generic argument
/// expression, so one spine serves two rule sets and the leaf cannot tell them apart on its own:
/// </para>
/// <list type="bullet">
///   <item><see cref="Arithmetic"/> — ISO §8.8.1.1: "An arithmetic expression may be an identifier referencing a
///         NUMERIC data item, a numeric literal, the figurative constant ZERO …". A group (class alphanumeric,
///         §8.5), an elementary alphanumeric/national item, and a reference-modified slice (§8.4.3.3.4 GR6) are all
///         inadmissible. This is the default and by far the common case: COMPUTE, the arithmetic verbs,
///         subscripts, reference-modifier offsets, relation conditions, SET, PERFORM VARYING.</item>
///   <item><see cref="FunctionArgument"/> — governed instead by the individual function's §15.x ARGUMENT RULE,
///         which for the string functions explicitly admits alphanumeric data: <c>FUNCTION TRIM(S)</c>,
///         <c>SUBSTITUTE</c>, <c>FIND-STRING</c> and <c>CONVERT</c> over a <c>PIC X</c> item are all legal.</item>
///   <item><see cref="CallByValue"/> — governed by ISO §14.9.4.3 SR22 ("identifier-4 shall be of class numeric,
///         object, or pointer"), a NARROWER rule than §8.8.1.1 rather than a wider one. Binding it as arithmetic
///         happened to reject the right programs while citing the wrong rule.</item>
/// </list>
/// <para>
/// ⛔ This is an ENUM and not a <c>bool</c> deliberately. A boolean would read as <c>BindExpr(node, true)</c> at the
/// call site, would not survive the arrival of a third context, and — as an optional parameter — silently breaks
/// the method-group conversions this spine is used through (<c>Select(host.Expr.BindExpr)</c>). The public surface
/// is therefore three intention-revealing entry points over one private core, so no caller ever passes a flag.
/// </para></summary>
internal enum OperandContext
{
    /// <summary>An ISO §8.8.1.1 arithmetic expression: numeric operands only.</summary>
    Arithmetic,

    /// <summary>An intrinsic-function argument: the function's own §15.x argument rule governs, so an alphanumeric
    /// operand may be perfectly legal here.</summary>
    FunctionArgument,

    /// <summary>
    /// A <c>CALL … USING BY VALUE</c> operand: ISO §14.9.4.3 SR22 governs, NOT §8.8.1.1.
    /// </summary>
    /// <remarks>
    /// ⛔ The grammar production is named <c>arithmeticExpression</c>, and the binder took that at its word and
    /// bound the operand as arithmetic — so DA6's §8.8.1.1 screen fired on it and
    /// <c>CALL "PROG2" USING BY VALUE X</c> (X alphanumeric) was refused with a message about *arithmetic
    /// expressions* and a "digit-decoding extension". The VERDICT was right by accident — SR22 requires class
    /// numeric, object or pointer, so an alphanumeric operand is indeed illegal — but the rule quoted was not the
    /// rule broken, which tells the programmer to look in the wrong place. Caught by the pre-merge GnuCOBOL
    /// differential as a two-case AGREE_ACCEPT→WE_REJECT flip, then traced rather than waved through.
    /// <para>
    /// A production's NAME is not its operand's rule. This is the same shape DA6 recorded for itself: a rule
    /// enforced at a site that could not know its own context.
    /// </para>
    /// </remarks>
    CallByValue,

    /// <summary>An arithmetic-expression position INSIDE one of §13.18.38.3 r7's index-name windows — a
    /// subscript, the VARYING phrase of PERFORM or SEARCH, the SET statement, or a relation-condition operand
    /// (kb/Work R29). The §8.8.1.1 class screening is identical to <see cref="Arithmetic"/>; ONLY the
    /// index-name interception differs: r7 admits an index-name here, so <c>T(IX + 1)</c>,
    /// <c>SET IX UP BY N</c>, <c>PERFORM … VARYING V FROM IX</c> and <c>IF IX = 2</c> stay legal while
    /// <c>COMPUTE N = IX + 1</c> does not.</summary>
    ArithmeticIndexWindow,
}

/// <summary>
/// The shared operand/expression/receiving spine (P7 Step 10q — the expression-spine flip; the plan’s
/// `Binding/Procedure/ExpressionBinder`, a SIBLING of <see cref="BinderContext"/>/<see cref="PhraseBlocks"/>,
/// deliberately NOT `Verbs/` — every verb binder consumes it). Three families over <see cref="BinderContext"/>:
/// the OPERAND family (LiteralOperand / national §8.3.3.5 / boolean §8.3.3.4 / figurative §8.3.3.6.4 /
/// FieldOperand with the keyword-omitted-function, LINAGE-COUNTER §8.4.3.14, RW-counter §8.4.3.15 and
/// index-name §13.18.38 interceptions), the EXPRESSION spine (<see cref="BindExpr"/> / BindChain / BindPower /
/// BindPrimary / the breadth-first <see cref="BindOperandExpr"/> wrapper descent / NumLiteral, with
/// <see cref="CheckLiteral"/> carrying its DECIMAL-POINT normalization + edition digit-cap window AS-IS — the
/// sanctioned binder-side pattern), and the RECEIVING family (<see cref="ResolveReceiving"/> — THE one
/// receiving-side chokepoint — ResolveTargets / Receivers×3 / RoundingOf §14.7.4). Host edges that remain
/// (host.Intrinsic / host.Rw) flip at 10t; the generic tree statics DataRefs/Children STAY on
/// <see cref="StatementBinder"/> (they are not expression-specific).
/// </summary>
internal sealed class ExpressionBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The C# <c>long</c> index field when <paramref name="dref"/> is a bare INDEXED BY index-name
    /// (ISO §13.18.38 — index-names are a separate name class living in <see cref="DataBinder.IndexFields"/>,
    /// not the data-item tree), else <see langword="null"/>.</summary>
    public string? IndexFieldOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord()?.GetText() is { } w
        && ctx.Symbols.TryResolveIndex(w, ctx.ActiveScope, out var f) ? f : null;

    // ── Operands & expressions ─────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE ONE <c>nonNumericLiteral</c> → <see cref="BoundOperand"/> MAPPING (ISO §8.3.3). Returns
    /// <see langword="null"/> when the node is absent or is not a non-numeric literal, so each caller supplies its
    /// own fallback — a numeric literal for <see cref="LiteralOperand"/>, a named error for an intrinsic argument,
    /// the expression path for a comparison operand.
    /// <para>
    /// ⛔ WHY THIS IS EXTRACTED. This chain existed in THREE hand-maintained copies — here, in
    /// <c>IntrinsicBinder.NonNumericOperand</c>, and inline in <c>ConditionBinder</c>'s comparison-operand binder —
    /// and that duplication IS the defect DA3 reported. A hexadecimal-alphanumeric literal was simply MISSING from
    /// one copy, so <c>IF G = X"6162"</c> (conforming source) staged loud as a "comparison operand" at run time
    /// while the same literal worked in a MOVE. Three copies of one dispatch guarantee that the next literal form
    /// added will be wired into two of them; one copy makes the next form a one-line change.
    /// </para></summary>
    public BoundOperand? NonNumericLiteralOperand(Core.NonNumericLiteralContext? nn)
    {
        if (nn is null) return null;
        // A concatenation expression folds to its equivalent single literal at COMPILE time (ISO §8.8.3.3
        // GR3 — "equivalent to a literal of the same class and value"): no BoundConcat node, no emitter leg;
        // the folded value rides the same operand shapes the plain literals produce (P10 Step 14).
        if (nn.concatenationExpression() is { } ce) return ConcatOperand(ce);
        if (nn.figurativeConstant() is { } fig) return FigurativeOperand(fig);
        if (nn.STRINGLIT() is { } s) return new BoundStringLiteral(CobolLiteral.Decode(s.GetText()));
        // ⛔ DA3 — X"…" is an ALPHANUMERIC literal, not a separate species: §8.3.3.2 Format 2 IS the
        // hexadecimal-alphanumeric FORMAT of the alphanumeric literal, and §8.3.3.2.1 makes every format of it "of
        // the class and category alphanumeric" (both cite.py-verified). So it belongs wherever an alphanumeric
        // literal belongs — a relation condition (§8.8.4.1.1 admits a literal on either side), MOVE, STRING, an
        // intrinsic argument. CobolLiteral.DecodeHex is the ONE hex codec (landed by DA1 for the §12.3.7 ALPHABET
        // path); this reuses it rather than decoding again.
        if (nn.HEXLIT() is { } hx) return new BoundStringLiteral(CobolLiteral.DecodeHex(hx.GetText()));
        // National N"…" (§8.3.3.5) / boolean B"…" (§8.3.3.4) literals — LIVE (Phase 4a): the introduction
        // gate rides every occurrence (0900 below 2002); content/size guards are the 0814 band. The lexer
        // already restricts a BOOLLIT's content to [01]+ (CobolLexer.g4).
        if (nn.NATLIT() is { } nat) return NationalLiteralOperand(nat.GetText());
        if (nn.BOOLLIT() is { } b) return BooleanLiteralOperand(b.GetText());
        return null;
    }

    /// <summary>A <c>literal</c> node as an operand: the non-numeric forms through the ONE
    /// <see cref="NonNumericLiteralOperand"/> mapping, else a numeric literal under the edition digit cap
    /// (ISO §8.3.1.2).</summary>
    public BoundOperand LiteralOperand(Core.LiteralContext lit) =>
        NonNumericLiteralOperand(lit.nonNumericLiteral())
        ?? new BoundNumericLiteral(CheckLiteral(lit.GetText()));

    /// <summary>Bind a §8.8.3 concatenation expression as a literal operand: fold to the equivalent single
    /// literal (§8.8.3.3 GR2/GR3 — the ONE ConcatFolder chokepoint enforces the §8.8.3.2 SRs) and produce the
    /// SAME bound shape the equivalent plain literal would have produced ("may be used anywhere a literal of
    /// that class may be used"). The introduction gate (concat-operator-2002 → 0900 below 2002) rides the
    /// VersionConformancePass parse arm on recognition, not this bind path.</summary>
    public BoundStringLiteral ConcatOperand(Core.ConcatenationExpressionContext ce)
    {
        var folded = ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.Collating, ctx.Data.NationalCollating);
        return folded.Category switch
        {
            PicCategory.National => new BoundStringLiteral(folded.Value) { Category = PicCategory.National },
            PicCategory.Boolean => new BoundStringLiteral(folded.Value) { Category = PicCategory.Boolean },
            _ => new BoundStringLiteral(folded.Value),
        };
    }

    /// <summary>Bind an <c>N"…"</c> national literal (ISO §8.3.3.5): SR1 caps the length at 8,191 national
    /// positions. The content repertoire is the FULL national character set — one UTF-16 char per position
    /// (D-N1, §8.1.2 NOTE 2) — including characters above U+00FF: the alphanumeric↔national correspondence
    /// (Annex A.1 item 33 — the TOTAL UTF-16 identity, both directions, PB59) is live through
    /// FUNCTION DISPLAY-OF / NATIONAL-OF (§15.26/§15.66), so the former staged-loud Latin-1-only guard is
    /// lifted (P10 national wave).</summary>
    public BoundStringLiteral NationalLiteralOperand(string raw)
    {
        // NationalData2002 (the N"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
        string value = CobolLiteral.Decode(raw);
        if (value.Length > 8191)
            ctx.Edition.Error("COBOLNET0814", $"national literal of {value.Length} positions exceeds the "
                + "8,191-position maximum (ISO §8.3.3.5 SR1)");
        return new BoundStringLiteral(value) { Category = PicCategory.National };
    }

    /// <summary>Bind a <c>B"…"</c> boolean literal (ISO §8.3.3.4): SR1 caps the length at 8,191 boolean
    /// positions; SR2 ('0'/'1' only) is lexer-enforced.</summary>
    public BoundStringLiteral BooleanLiteralOperand(string raw)
    {
        // BooleanData2002 (the B"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
        string value = CobolLiteral.Decode(raw);
        if (value.Length > 8191)
            ctx.Edition.Error("COBOLNET0814", $"boolean literal of {value.Length} positions exceeds the "
                + "8,191-position maximum (ISO §8.3.3.4 SR1)");
        return new BoundStringLiteral(value) { Category = PicCategory.Boolean };
    }

    /// <summary>Bind a figurative constant to a bound operand. <c>ALL "literal"</c> / <c>ALL X"…"</c> (a
    /// multi-character figurative, ISO §8.3.3.6.4 Format 6) → <see cref="BoundAllLiteral"/>; <c>ALL ZEROS</c> etc.
    /// are the single-character figurative repeated to width, identical to the bare word.</summary>
    /// <remarks>
    /// ⛔ The HEXLIT arm was the FIFTH site of the hexadecimal-literal defect (fix-queue PB4), and the one the
    /// grammar had been ready for all along — <c>figurativeConstant</c> lists <c>ALL HEXLIT</c>, and this method
    /// tested only <c>STRINGLIT</c>, so <c>MOVE ALL X"41" TO X</c> parsed and then died at RUN time with
    /// "figurative constant 'ALLX\"41\"'". §8.3.3.2 makes a hexadecimal literal one form of an ALPHANUMERIC
    /// literal, so Format 6's literal-1 admits it and the two arms are the same case.
    /// </remarks>
    public static BoundOperand FigurativeOperand(Core.FigurativeConstantContext fig)
    {
        // ⛔ ONE ARM FOR EVERY LITERAL-1 KIND (kb/Work PB71): §8.3.3.6.3 SR2 admits an alphanumeric (plain or
        // hexadecimal), boolean or national literal-1, and the category rides on the literal's class through
        // BoundAllLiteral.Of. Two arms of four were written here: `ALL B"1"` (the grammar HAD the token) parsed and
        // died at RUN time exactly as `ALL X"41"` once did — the PB4 shape, one arm later, under the remark that
        // records it — and `ALL N"…"` had no grammar arm at all.
        if (fig.allLiteral() is { } al)
            return BoundAllLiteral.Of(al.allLiteralOperand().Select(o => o.GetText()).ToArray());
        if (fig.ZERO() is not null) return new BoundFigurative('Z');
        if (fig.SPACE() is not null) return new BoundFigurative('S');
        if (fig.HIGH_VALUE() is not null) return new BoundFigurative('H');
        if (fig.LOW_VALUE() is not null) return new BoundFigurative('L');
        if (fig.QUOTE_() is not null) return new BoundFigurative('Q');
        if (fig.NULL_() is not null) return new BoundFigurative('N');
        return new BoundOperandError($"figurative constant '{fig.GetText()}'");
    }

    public BoundOperand FieldOperand(Core.DataReferenceContext dref) =>
        host.Intrinsic.KeywordOmittedFunction(dref) is { } kof ? IntrinsicBinder.OperandOf(kof)   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundComputedOperand(new BoundLinageCounterRef(lcf))
                : new BoundOperandError($"LINAGE-COUNTER reference '{dref.GetText()}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15) — RWCS registers, intercepted ahead of name resolution
        // (the LINAGE-COUNTER idiom); a BoundExprError inside the computed wrapper stays loud (§1.4).
        : host.Rw.CounterExpr(dref) is { } rcx ? new BoundComputedOperand(rcx)
        : IndexFieldOf(dref) is { } ix ? new BoundComputedOperand(new BoundIndexRef(ix))
        : ConstantOperand(dref) is { } konst ? konst   // a constant-name substitutes its literal (§13.10.3 SR2)
        : ctx.Refs.Resolve(dref) is { } p ? new BoundFieldOperand(p) : new BoundOperandError(RefFailure(dref));

    /// <summary>The §13.18.38.3 r7 screen for an operand slot OUTSIDE the five contexts that may reference an
    /// index-name (a subscript · PERFORM VARYING · SEARCH VARYING · SET · a relation-condition operand) — kb/Work
    /// R16. <see cref="FieldOperand"/> deliberately still BINDS the index-name (the r7-legal consumers share it),
    /// so each ILLEGAL slot screens the result: before this, DISPLAY/STRING/MOVE-to-string aborted at RUN TIME
    /// ("computed expression in a string context") and MOVE-to-numeric silently computed — against the same
    /// judgment COBOLNET0809 already applies to class-index DATA ITEMS in MOVE. Returns true when the operand
    /// was an index-name (the caller substitutes its own error operand or continues; the diagnostic is
    /// emitted here, once).</summary>
    public bool ScreenIndexNameOperand(BoundOperand op, string sourceText, string where)
    {
        if (op is not BoundComputedOperand { Expr: BoundIndexRef }) return false;
        ctx.Edition.Error(DiagnosticCatalog.IndexNameContext,
            $"{where} references the index-name '{sourceText}', which is not an identifier "
            + "(ISO §8.4.3.1.2); §13.18.38.3 r7 admits an index-name only as a subscript, in PERFORM/SEARCH "
            + "VARYING, in SET, or in a relation condition. SET a data item to the index first "
            + $"(SET data-item TO {sourceText}) and reference the data item");
        return true;
    }

    /// <summary>The bound literal a bare constant-name reference substitutes, or <see langword="null"/> when
    /// <paramref name="dref"/> names no constant (ISO §13.10.3 SR2 / §13.10.4 GR1 — "as if [the] literal were
    /// written where constant-name-1 is written"): the SAME bound shape the equivalent plain literal would
    /// produce (the <see cref="ConcatOperand"/> precedent). A numeric constant rides <see cref="CheckLiteral"/>
    /// so the edition digit-cap window applies to the substituted literal exactly as to a written one.</summary>
    private BoundOperand? ConstantOperand(Core.DataReferenceContext dref) =>
        ctx.Data.ConstantOf(dref) is not { } k ? null
        : k.Category switch
        {
            PicCategory.Numeric => new BoundNumericLiteral(CheckLiteral(k.Text)),
            PicCategory.National => new BoundStringLiteral(k.Text) { Category = PicCategory.National },
            PicCategory.Boolean => new BoundStringLiteral(k.Text) { Category = PicCategory.Boolean },
            _ => new BoundStringLiteral(k.Text),
        };

    /// <summary>The loud-failure text for an unresolvable data reference — when the name belongs to a REJECTED
    /// shared-storage class (a Tier-C / national REDEFINES, an unsupported cell shape), the class's
    /// <c>RejectReason</c> rides along so the runtime loud names WHY, not just the reference (the
    /// design's "references then fail loud" contract, made self-explanatory).</summary>
    private string RefFailure(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        // kb/Work R32 — a name declared in the (documented-unsupported) SCREEN SECTION: the staged loud
        // names the actual cause, not a bare unresolved reference. R38 — the same honesty for a declared
        // ALPHABET-NAME in a data position (the INSPECT CONVERTING alphabet extension adjudication).
        if (ctx.Data.ScreenNames.Contains(name))
            return $"reference '{dref.GetText()}' — declared in the SCREEN SECTION, an optional facility "
                 + "COBOL.NET does not support (COBOLNET1560; docs/CONFORMANCE.md §4)";
        if (ctx.Data.Alphabets.ContainsKey(name) || ctx.Data.NationalAlphabets.ContainsKey(name))
            return $"reference '{dref.GetText()}' — declared as an ALPHABET-name (SPECIAL-NAMES), which this "
                 + "position does not reference as a data item (kb/Work R38 adjudicates the vendor "
                 + "alphabet-operand extension)";
        string? reason = ctx.Symbols.TryResolve(name, ctx.ActiveScope, out var named)
            ? named.Select(i => i.Class)
                .FirstOrDefault(c => c is { Tier: RedefinesTier.Rejected, RejectReason: not null })
                ?.RejectReason
            : null;
        return reason is null ? $"reference '{dref.GetText()}'" : $"reference '{dref.GetText()}' — {reason}";
    }

    /// <summary>Bind a data reference in a numeric-expression position: an INDEXED BY index-name reads its
    /// occurrence number (valid in SET/SEARCH/relations, ISO §13.18.38); the LINAGE-COUNTER register reads its
    /// file's runtime counter (ISO §8.4.3.14 GR1 — an unsigned integer); otherwise the resolved item's value.
    /// The ONE dataReference→<see cref="BoundExpr"/> mapping, used by every expression path.</summary>
    private BoundExpr RefExpr(Core.DataReferenceContext dref, OperandContext context) =>
        host.Intrinsic.KeywordOmittedFunction(dref) is { } kof ? kof   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundLinageCounterRef(lcf)
                : new BoundExprError($"LINAGE-COUNTER reference '{dref.GetText()}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15): in the PROCEDURE DIVISION the registers may appear
        // wherever an integer item may (SR1) — read from the report's engine instance, never storage.
        : host.Rw.CounterExpr(dref) is { } rcx ? rcx
        : IndexFieldOf(dref) is { } ix ? IndexNameExpr(dref, ix, context)
        // A constant-name substitutes its literal (§13.10.3 SR2 / §13.10.4 GR1) — in a numeric-expression
        // position only a NUMERIC constant is legal, exactly as for a written literal (§8.8.1.1).
        : ctx.Data.ConstantOf(dref) is { } k
            ? k.Category is PicCategory.Numeric ? new BoundNumLiteral(CheckLiteral(k.Text))
                : NonNumericConstantExpr(dref.GetText(), k.Category)
        : ctx.Refs.Resolve(dref) is { } p ? OperandRef(dref, p, context)
        : new BoundExprError(RefFailure(dref));

    /// <summary>The §13.18.38.3 r7 screen for an INDEX-NAME reached as an expression operand (kb/Work R29 —
    /// the arithmetic sibling of R16's statement-slot screen). r7's closed context list admits an index-name
    /// in a subscript, PERFORM/SEARCH VARYING, SET, and a relation condition — those positions bind under
    /// <see cref="OperandContext.ArithmeticIndexWindow"/> and pass through. A true arithmetic position
    /// (COMPUTE and the arithmetic verbs, RETRY, CONTINUE AFTER, STOP STATUS …) or a compound
    /// function-argument expression is NOT in the list and §8.8.1.1 does not name index-names among
    /// arithmetic operands — the classic vendor extension (GnuCOBOL computes the occurrence number), so the
    /// disposition is the DA6/PB1 shape: strict = reject with the r7 citation, <c>--permissive</c> = the
    /// documented coercion (the occurrence number computes, with a warning). CallByValue keeps its own
    /// §14.9.4.3 SR22 screen and is deliberately not intercepted here.</summary>
    private BoundExpr IndexNameExpr(Core.DataReferenceContext dref, string ix, OperandContext context)
    {
        if (context is OperandContext.Arithmetic or OperandContext.FunctionArgument)
        {
            if (ctx.Edition.Permissive)
                ctx.Edition.Warning(DiagnosticCatalog.IndexNameContext,
                    $"the index-name '{dref.GetText()}' is used as an arithmetic operand; §13.18.38.3 r7 "
                    + "admits an index-name only in a subscript, PERFORM/SEARCH VARYING, SET, or a relation "
                    + "condition — accepted under --permissive, computing the occurrence number");
            else
            {
                ctx.Edition.Error(DiagnosticCatalog.IndexNameContext,
                    $"the index-name '{dref.GetText()}' is not an arithmetic operand (ISO §8.8.1.1 names no "
                    + "index-names; §13.18.38.3 r7 admits an index-name only in a subscript, PERFORM/SEARCH "
                    + "VARYING, SET, or a relation condition). SET a data item to the index first "
                    + $"(SET data-item TO {dref.GetText()}) — or --permissive accepts it as the occurrence "
                    + "number");
                return new BoundExprError($"index-name '{dref.GetText()}' in an arithmetic expression");
            }
        }
        return new BoundIndexRef(ix);
    }

    /// <summary>The §8.8.1.1 class screen for a resolved data reference used as an expression operand (DA6).
    /// <para>
    /// COBOL.NET accepted every alphanumeric shape here and decoded its digit characters — and did so
    /// INCONSISTENTLY: a group of <c>PIC X</c> leaves computed, while a group of <c>PIC 9</c> leaves compiled and
    /// then THREW at run time, so the operand whose digits were unambiguous failed and the merely-textual one
    /// succeeded. Owner decision 2026-07-29: reject under strict conformance, keep the leniency DIALECT-GATED
    /// behind <c>--permissive</c> (the standing rule that every leniency is dialect-gated).
    /// </para>
    /// <para>
    /// Reuses <c>COBOLNET0844</c> rather than minting a code: 0844 already IS "not a numeric operand (ISO
    /// §8.8.1.1)", raised by <see cref="NonNumericConstantExpr"/> for a non-numeric constant-name and for a
    /// national/boolean literal in this same position. A data item is the third shape of ONE rule, not a new rule.
    /// </para></summary>
    private BoundExpr OperandRef(Core.DataReferenceContext dref, Place p, OperandContext context)
    {
        if (context is OperandContext.Arithmetic && NonNumericOperandKind(p) is { } what)
        {
            if (ctx.Edition.Permissive)
                ctx.Edition.Warning("COBOLNET0844", $"{what} is not a numeric operand (ISO §8.8.1.1); accepted "
                    + "under --permissive, decoding its digit characters as an unsigned integer");
            else
            {
                ctx.Edition.Error("COBOLNET0844", $"{what} is not a numeric operand: ISO §8.8.1.1 admits only an "
                    + "identifier referencing a NUMERIC data item, a numeric literal, or the figurative constant "
                    + "ZERO in an arithmetic expression. --permissive accepts it as a digit-decoding extension");
                return new BoundExprError($"{what} in an arithmetic expression (ISO §8.8.1.1)");
            }
        }
        return new BoundNumRef(p);
    }

    /// <summary>A human-readable description of WHY a place is not a numeric operand, or <see langword="null"/>
    /// when it is one. Class BOOLEAN carries its own §8.8.1 rejection in the renderer; INDEX / POINTER never
    /// resolve to a <c>Pic</c> here.
    /// <para>
    /// ⛔ <b>A NUMERIC-EDITED ITEM IS REJECTED (owner decision 2026-08-02, reversing DA6's admission).</b> This
    /// arm previously read "a numeric-EDITED item de-edits to a defined numeric value and is admissible", and it
    /// was derived from the wrong place — from what de-editing CAN do, not from what §8.8.1.1 ADMITS. The
    /// standard decides it three ways, all validated with <c>cite.py --check</c>:
    /// <list type="bullet">
    ///   <item>§8.8.1.1 admits "an identifier referencing a <b>numeric data item</b>", and §8.5.2.13 says such an
    ///         item "is referred to as a <b>numeric-edited data item</b>" — a DISTINCT defined term. Table 2
    ///         (§8.5.2.1) puts category numeric-edited in class ALPHANUMERIC (usage display) or NATIONAL, never
    ///         class numeric. It is neither the class nor the category §8.8.1.1 names.</item>
    ///   <item>Every de-editing rule in the standard is a MOVE/editing rule — §14.9.25.4 GR6d1 ("de-editing
    ///         establishes the operand's numeric value"), the CURRENCY SIGN and LOCALE clauses. GR6d1 has to
    ///         GRANT de-editing for the MOVE, which it would not need to do if de-editing were generally
    ///         available to any numeric context.</item>
    ///   <item>§15.3's integer type 6 offers "an arithmetic expression … or an <b>integer data item</b>" as its
    ///         only two alternatives; a numeric-edited item is neither.</item>
    /// </list>
    /// The sibling <c>IntrinsicArgumentRules</c> 'n' arm had ALREADY refuted this same reading (its negative
    /// fixture <c>pb1-numeric-arg-numeric-edited</c> pins it), so the two screens rested on readings of §8.8.1.1
    /// that could not both be right. They now agree. ⚠ This changes ONLY what an ARITHMETIC operand may be — the
    /// 's' string family still admits a numeric-edited item, because Table 2 genuinely makes it class
    /// alphanumeric and a CLASS rule means what it says.
    /// </para></summary>
    private static string? NonNumericOperandKind(Place p) => p switch
    {
        RefModPlace => "a reference-modified operand (class alphanumeric, ISO §8.4.3.3.4 GR6)",
        _ when p.Item.IsGroup => $"group item '{p.Item.CobolName}' (class alphanumeric, ISO §8.5)",
        _ when p.Item.Pic is { Category: PicCategory.NumericEdited } =>
            $"item '{p.Item.CobolName}' of category numeric-edited (a numeric-edited data item is not a NUMERIC "
            + "data item — ISO §8.5.2.13 + §8.5.2.1 Table 2; de-editing is a MOVE rule, §14.9.25.4 GR6d1)",
        _ when p.Item.Pic is { Category: PicCategory.Alphanumeric or PicCategory.National, EditMask: null } pic =>
            $"item '{p.Item.CobolName}' of category {pic.Category}",
        _ => null,
    };

    /// <summary>Reject a non-numeric constant-name in a numeric-expression position (ISO §8.8.1.1 — arithmetic
    /// operands shall be numeric; the constant stands for its literal, §13.10.3 SR2), mirroring the written
    /// national/boolean-literal rejection above (COBOLNET0844).</summary>
    private BoundExprError NonNumericConstantExpr(string name, PicCategory category)
    {
        ctx.Edition.Error("COBOLNET0844", $"constant-name '{name}' substitutes a literal of category "
            + $"{category} and is not a numeric operand (ISO §8.8.1.1 / §13.10.3 SR2)");
        return new BoundExprError($"constant-name '{name}' in a numeric context");
    }

    /// <summary>Resolve a LINAGE-COUNTER reference to its file (ISO §8.4.3.14): in the grammar alternative
    /// <c>LINAGE_COUNTER ((OF|IN) cobolWord)?</c> the cobolWord IS the file-name qualifier. Unqualified, the
    /// register resolves only when exactly ONE file has a LINAGE clause — with several, qualification is
    /// required (§8.4.3.14 SR3 / §8.4.2.2). Null (the caller binds a loud error) for no/an ambiguous match,
    /// with a bind-time diagnostic naming the rule.</summary>
    private FileModel? LinageFileOf(Core.DataReferenceContext dref)
    {
        if (dref.cobolWord() is { } q)   // qualified: LINAGE-COUNTER OF/IN file-name
        {
            if (ctx.Data.FilesByName.TryGetValue(q.GetText(), out var named) && named.Linage is not null) return named;
            ctx.Edition.Error("COBOLNET0863", $"LINAGE-COUNTER OF '{q.GetText()}': the qualifier shall name a "
                + "file whose file description entry contains a LINAGE clause (ISO §8.4.3.14 / §13.18.34 GR7a)");
            return null;
        }
        var linageFiles = ctx.Data.Files.Where(f => f.Linage is not null).ToList();
        if (linageFiles.Count == 1) return linageFiles[0];
        ctx.Edition.Error("COBOLNET0864", linageFiles.Count == 0
            ? "LINAGE-COUNTER referenced, but no file description entry contains a LINAGE clause (ISO §8.4.3.14 — "
              + "the register is generated by the presence of a LINAGE clause)"
            : "unqualified LINAGE-COUNTER with more than one LINAGE file: qualify by file-name (ISO §8.4.3.14 "
              + "SR3 / §8.4.2.2 Qualification)");
        return null;
    }

    // Receiving references resolve through ResolveReceiving below — the ONE
    // receiving-side chokepoint: a report counter receiver is rejected at bind (LINE-COUNTER illegal per ISO
    // §8.4.3.15 SR3; PAGE-COUNTER staged loud) instead of being SILENTLY dropped by .OfType<Place>() (§1.4).
    public List<Place> ResolveTargets(IEnumerable<Core.DataReferenceContext> targets) =>
        targets.Select(ResolveReceiving).OfType<Place>().ToList();

    // ── ROUNDED phrase → rounding mode + receiver resolution (ISO §14.7.4) ───────────────────────────────────

    /// <summary>The rounding mode a (possibly absent) ROUNDED phrase selects (ISO §14.7.4.3). No phrase → TRUNCATION
    /// (rule 2); a bare <c>ROUNDED</c> → the program's DEFAULT ROUNDED mode (rule 1 / §11.9.6 — the OPTIONS
    /// <c>DEFAULT ROUNDED MODE IS x</c> clause, defaulting to NEAREST-AWAY-FROM-ZERO when absent); an explicit
    /// <c>MODE IS x</c> → the named mode (via the shared <see cref="RoundingModes"/> mapping).</summary>
    public CobolRounding RoundingOf(Core.RoundedPhraseContext? phrase)
    {
        if (phrase is null) return CobolRounding.Truncation;
        if (phrase.roundingModeName() is { } mode)
        {
            // The explicit MODE IS phrase (and the 8-mode set) is ISO 2014+ (§14.7.4); at 85/2002 a bare ROUNDED
            // means the single nearest-away-from-zero rounding. The RoundedModeIs2014 introduction gate fires on
            // RECOGNITION in the VersionConformancePass parse-arm (VisitRoundedPhrase, roundingModeName != null); 14h.4a.
            return RoundingModes.Map(mode);
        }
        return ctx.Options.DefaultRounding;
    }

    // ── The RECEIVING chokepoint (hoisted from the ReportWriter partial at 10f; HOME here since 10q —
    //    the shared receiving spine the arithmetic/MOVE/SET pipelines consume). ──

    /// <summary>Resolve a RECEIVING data reference to its <see cref="Place"/> — the ONE receiving-side
    /// chokepoint (MOVE targets, arithmetic resultants, SET receivers). A report counter here is rejected at
    /// bind time: LINE-COUNTER shall not be a receiving operand (ISO §8.4.3.15 SR3 — illegal); PAGE-COUNTER as a
    /// receiver is legal but not yet implemented (staged loud). Without this guard the
    /// <c>.OfType&lt;Place&gt;()</c> receiver pipelines would DROP the counter silently — a silent-miscompile
    /// hazard (§1.4).</summary>
    public Place? ResolveReceiving(Core.DataReferenceContext dref)
    {
        if (dref.LINE_COUNTER() is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ReportLineCounterReceiving,
                "LINE-COUNTER shall not be referenced as a receiving operand (ISO §8.4.3.15.3 SR3)");
            return null;
        }
        // A constant-name substitutes a LITERAL (ISO §13.10.3 SR2 / §13.10.4 GR1) — a literal can never be a
        // receiving operand; without this the name would fall to Refs.Resolve and fail as merely "unresolved".
        if (ctx.Data.ConstantOf(dref) is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ConstantAsReceiver, $"constant-name '{dref.GetText()}' shall "
                + "not be specified as a receiving operand — it substitutes a literal (ISO §13.10.3 SR2)");
            return null;
        }
        if (dref.PAGE_COUNTER() is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ReportPageCounterReceiving, "PAGE-COUNTER as a receiving operand (ISO §8.4.3.15 — legal; the "
                + "program assigns page numbers) is not yet implemented");
            return null;
        }
        var place = ctx.Refs.Resolve(dref);
        // ⛔ THE ONE RECEIVING CHOKEPOINT NEVER DROPS A RECEIVER SILENTLY (kb/Work PB70): `MOVE "Z" TO OK1 TB(2:1) OK2`
        // used to move into OK1 and OK2 and skip TB without a word — the resolver's unsupported-shape null fell
        // through .OfType<Place>() in ResolveTargets / Receivers. An undefined name or a rejected shape was already
        // reported by the resolver (WasDiagnosed); a name that RESOLVED but whose reference shape this compiler does
        // not implement as a receiver is reported HERE, recognized-not-implemented, so the compilation fails instead
        // of the statement running one receiver short.
        if (place is null)
        {
            if (!ctx.Refs.WasDiagnosed(dref))
                ctx.Edition.Error(DiagnosticCatalog.ReceivingReferenceNotImplemented,
                    $"receiving operand '{dref.GetText()}' names a declared item in a reference shape COBOL.NET does "
                    + "not yet implement as a receiver (COBOLNET_DESIGN §1.4 — rejected rather than dropped)");
            return null;
        }
        // The OCCURS DYNAMIC CAPACITY register (§13.18.38 SR30–32; D9) is set ONLY by a SET Format 14 statement
        // (which reroutes BEFORE this chokepoint). Any other receiving use — MOVE/arithmetic resultant/ordinary SET
        // receiver — is illegal; reject it here rather than reach CapacityRegisterPlace.Write (an internal throw).
        if (place is CapacityRegisterPlace cap)
        {
            ctx.Edition.Error("COBOLNET1523", $"the CAPACITY register '{cap.RegisterItem.CobolName}' shall not be a "
                + "receiving operand except in a SET statement Format 14 (ISO §13.18.38 SR30–32)");
            return null;
        }
        // A CONSTANT RECORD's content cannot be modified — neither the record nor any subordinate may be a
        // receiving operand (ISO §13.18.15.3 SR2 → COBOLNET1548; DataBinder.RejectConstantStore).
        if (ctx.Data.RejectConstantStore(place, $"receiving operand '{dref.GetText()}'")) return null;
        return place;
    }

    /// <summary>Resolve <c>receivingArithmeticOperand</c>s (the GIVING / TO / FROM / INTO resultants) to
    /// <see cref="Receiver"/>s, each carrying its own ROUNDED mode; an unresolvable reference is dropped.</summary>
    public List<Receiver> Receivers(IEnumerable<Core.ReceivingArithmeticOperandContext> ops) =>
        ops.Select(o => ResolveReceiving(o.dataReference()) is { } p ? new Receiver(p, RoundingOf(o.roundedPhrase())) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the in-place <c>MULTIPLY … BY</c> receivers (<c>multiplyByOperand</c> = receiving operand +
    /// optional ROUNDED), each carrying its own mode; a literal BY operand (only valid in a GIVING form) is dropped.</summary>
    public List<Receiver> Receivers(IEnumerable<Core.MultiplyByOperandContext> ops) =>
        ops.Select(o => o.receivingOperand()?.dataReference() is { } d && ResolveReceiving(d) is { } p
                ? new Receiver(p, RoundingOf(o.roundedPhrase())) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the <c>COMPUTE</c> resultants (<c>computeStore</c> = data reference + optional ROUNDED).</summary>
    public List<Receiver> Receivers(IEnumerable<Core.ComputeStoreContext> stores) =>
        stores.Select(s => ResolveReceiving(s.dataReference()) is { } p ? new Receiver(p, RoundingOf(s.roundedPhrase())) : null)
              .OfType<Receiver>().ToList();

    /// <summary>Bind any numeric node (expression, operand wrapper, literal, or data reference) as an ISO §8.8.1.1
    /// ARITHMETIC expression — numeric operands only. THE entry for COMPUTE, the arithmetic verbs, subscripts,
    /// reference-modifier offsets, relation conditions, SET and PERFORM VARYING, i.e. every ordinary caller.
    /// <para>Deliberately takes NO context parameter: an optional one would break the method-group conversions this
    /// spine is used through (<c>Select(host.Expr.BindExpr)</c>) and would put a bare <c>true</c> at a call site.
    /// The contexts that differ have their own named entries, <see cref="BindFunctionArgumentExpr"/> and
    /// <see cref="BindByValueExpr"/>.</para></summary>
    public BoundExpr BindExpr(IParseTree node) => BindExprCore(node, OperandContext.Arithmetic);

    /// <summary>Bind an arithmetic expression sitting INSIDE one of §13.18.38.3 r7's index-name windows —
    /// subscripts, SET amounts/values, PERFORM/SEARCH (and RW) VARYING operands, relation/EVALUATE operands —
    /// where an index-name is a legal operand (kb/Work R29). Identical to <see cref="BindExpr"/> otherwise.</summary>
    public BoundExpr BindIndexWindowExpr(IParseTree node) => BindExprCore(node, OperandContext.ArithmeticIndexWindow);

    /// <summary>Bind a <c>CALL … USING BY VALUE</c> operand. Identical to <see cref="BindExpr"/> except that the
    /// §8.8.1.1 numeric-operand screen does not apply: the operand's legality is ISO §14.9.4.3 SR22's business
    /// ("identifier-4 shall be of class numeric, object, or pointer"), which the CALL binder enforces with its own
    /// diagnostic. Binding it as arithmetic quoted §8.8.1.1 at a programmer who had broken SR22.</summary>
    public BoundExpr BindByValueExpr(IParseTree node) => BindExprCore(node, OperandContext.CallByValue);

    /// <summary>Bind an INTRINSIC-FUNCTION ARGUMENT expression. Identical to <see cref="BindExpr"/> except that the
    /// §8.8.1.1 numeric-operand screen does not apply: an argument's legality is governed by the individual
    /// function's §15.x argument rule, which for the string functions admits alphanumeric data
    /// (<c>FUNCTION TRIM(S)</c> over a <c>PIC X</c> item is legal). The grammar reuses the
    /// <c>arithmeticExpression</c> production for arguments, which is why the distinction has to be made by the
    /// CALLER and cannot be inferred at the leaf.</summary>
    public BoundExpr BindFunctionArgumentExpr(IParseTree node) => BindExprCore(node, OperandContext.FunctionArgument);

    /// <summary>The ONE recursive expression spine. <paramref name="context"/> rides every recursive call rather
    /// than living in a field — the same discipline the render-side receiver follows (P7 Step 3: "travels by
    /// parameter into every public entry, never mutable context state"), and for the same reason: ambient state
    /// goes stale across a re-entrant descent.</summary>
    private BoundExpr BindExprCore(IParseTree node, OperandContext context) => node switch
    {
        Core.ArithmeticExpressionContext a => BindExprCore(a.GetChild(0), context),
        Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext => BindChain(node, context),
        Core.PowerExpressionContext p => BindPower(p, context),
        Core.UnaryExpressionContext u => u.primaryExpression() is { } pr ? BindExprCore(pr, context)
            : u.addOp().GetText() == "-" ? new BoundNegate(BindExprCore(u.unaryExpression(), context))
                : BindExprCore(u.unaryExpression(), context),
        Core.PrimaryExpressionContext pe => BindPrimary(pe, context),
        Core.LiteralContext l => NumLiteral(l),
        Core.DataReferenceContext d => RefExpr(d, context),
        _ => BindOperandExprCore(node, context),   // operand wrappers (addOperand, multiplyByOperand, …)
    };

    /// <summary>A numeric literal expression from a <c>literal</c> node, mapping a figurative ZERO (incl. <c>ALL ZEROS</c>)
    /// to <c>0</c> (ISO §8.3.1.2 — ZERO is a valid numeric operand); a non-numeric figurative (SPACE / HIGH-VALUE / …)
    /// in a numeric context is a loud error rather than the raw word rendered as an identifier. A national or
    /// boolean literal is NOT a numeric operand (§8.8.1.1 — arithmetic operands shall be numeric): COBOLNET0844
    /// at bind, never raw literal text spliced into the generated expression.</summary>
    private BoundExpr NumLiteral(Core.LiteralContext lit)
    {
        if (lit.nonNumericLiteral()?.concatenationExpression() is not null)
        {
            // A concatenation expression is of class alphanumeric, boolean, or national (ISO §8.8.3.2 SR1) —
            // never numeric, so it is not an arithmetic operand (§8.8.1.1): the same 0844 posture as a bare
            // national/boolean literal in a numeric context.
            ctx.Edition.Error("COBOLNET0844", "a concatenation expression is not a numeric operand "
                + "(ISO §8.8.3.2 SR1 — class alphanumeric/boolean/national; §8.8.1.1 — arithmetic operands "
                + "shall be numeric)");
            return new BoundExprError($"concatenation expression '{lit.GetText()}' in a numeric context");
        }
        if (lit.nonNumericLiteral()?.figurativeConstant() is { } fig)
            return fig.ZERO() is not null ? new BoundNumLiteral("0")
                : new BoundExprError($"figurative constant '{fig.GetText()}' in a numeric context");
        if (lit.nonNumericLiteral() is { } nn && (nn.NATLIT() ?? nn.BOOLLIT()) is not null)
        {
            ctx.Edition.Error("COBOLNET0844", $"a {(nn.NATLIT() is not null ? "national" : "boolean")} "
                + "literal is not a numeric operand (ISO §8.8.1.1 — arithmetic operands shall be numeric)");
            return new BoundExprError($"literal '{lit.GetText()}' in a numeric context");
        }
        return new BoundNumLiteral(CheckLiteral(lit.GetText()));
    }

    /// <summary>Normalize the decimal separator (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a — the comma form
    /// canonicalizes to dot-decimal so every emit-side decoder sees one shape) and edition-gate the digit count
    /// (ISO §8.3.1.2 — 1..18 at COBOL-85, 1..31 at 2002+). The ONE literal chokepoint for the expression paths.</summary>
    public string CheckLiteral(string text)
    {
        text = ctx.Data.NormalizeNumericLiteral(text);
        int digits = text.Count(char.IsAsciiDigit);
        ctx.Edition.CheckDigitCapacity(digits, $"numeric literal '{text}'");
        return text;
    }

    private BoundExpr BindChain(IParseTree node, OperandContext context)
    {
        BoundExpr? acc = null;
        char op = '+';
        foreach (var child in StatementBinder.Children(node))
        {
            if (child is Core.AddOpContext or Core.MulOpContext) op = child.GetText()[0];
            else { var x = BindExprCore(child, context); acc = acc is null ? x : new BoundBinary(acc, op, x); }
        }
        return acc ?? new BoundNumLiteral("0");
    }

    private BoundExpr BindPower(Core.PowerExpressionContext p, OperandContext context)
    {
        var bases = p.unaryExpression();
        BoundExpr acc = BindExprCore(bases[0], context);
        for (int i = 1; i < bases.Length; i++) acc = new BoundPower(acc, BindExprCore(bases[i], context));
        return acc;
    }

    private BoundExpr BindPrimary(Core.PrimaryExpressionContext pe, OperandContext context)
    {
        if (pe.numericLiteral() is { } num) return new BoundNumLiteral(CheckLiteral(num.GetText()));
        if (pe.ZERO_ARITH() is not null) return new BoundNumLiteral("0");
        if (pe.dataReference() is { } dref) return RefExpr(dref, context);
        if (pe.arithmeticExpression() is { } paren) return BindExprCore(paren, context);
        // FUNCTION call (ISO §15; the 1989 Intrinsic Function Module) — StatementBinder.Intrinsics.cs.
        if (pe.functionCall() is { } fc)
        {
            var call = host.Intrinsic.BindIntrinsic(fc);
            // §8.8.1.1's class screen for a FUNCTION-IDENTIFIER operand (kb/Work PB68 — the fourth site of the
            // class-boolean rule): a function-identifier references a temporary data item (§8.4.3.2.4 GR1) whose
            // class is the function's type (§15.2), so an alphanumeric, national or BOOLEAN function is not "an
            // identifier referencing a numeric data item" — the same DA6 rule OperandRef applies to a data item,
            // with the same dialect gate (strict rejects; --permissive decodes the digit characters). Before this,
            // `COMPUTE N = FUNCTION BOOLEAN-OF-INTEGER(5, 8) + 1` compiled clean and died at run time with an
            // unhandled NotImplemented — a crash on legal-shaped source, the wrong stage.
            if (context is OperandContext.Arithmetic
                && call is BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } sc)
            {
                string what = $"FUNCTION {sc.Sig.Name} (a {sc.ResultCategory.ToString().ToLowerInvariant()} function, ISO §15.2)";
                if (ctx.Edition.Permissive)
                    ctx.Edition.Warning("COBOLNET0844", $"{what} is not a numeric operand (ISO §8.8.1.1); accepted "
                        + "under --permissive, decoding its digit characters as an unsigned integer");
                else
                {
                    ctx.Edition.Error("COBOLNET0844", $"{what} is not a numeric operand: ISO §8.8.1.1 admits only an "
                        + "identifier referencing a NUMERIC data item, a numeric literal, or the figurative constant "
                        + "ZERO in an arithmetic expression. --permissive accepts it as a digit-decoding extension");
                    return new BoundExprError($"{what} in an arithmetic expression (ISO §8.8.1.1)");
                }
            }
            return call;
        }
        return new BoundExprError("primary-expression operand");
    }

    /// <summary>Descend an operand-wrapper node to its inner arithmetic expression, or its leaf literal / data
    /// ref. The wrapper chain can nest the expression MORE than one level deep (<c>comparisonOperand →
    /// valueOperand → arithmeticExpression</c>, CobolExpressions.g4), so the walk is BREADTH-FIRST to the
    /// shallowest match — a depth-first leaf grab would collapse a multi-term operand to its first data
    /// reference (a sign condition's operand is the WHOLE expression, ISO §8.8.4.3 — NC250A IF--TEST-55/56).</summary>
    public BoundExpr BindOperandExpr(IParseTree node) => BindOperandExprCore(node, OperandContext.Arithmetic);

    /// <inheritdoc cref="BindOperandExpr"/>
    private BoundExpr BindOperandExprCore(IParseTree node, OperandContext context)
    {
        var queue = new Queue<IParseTree>();
        queue.Enqueue(node);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            for (int i = 0; i < n.ChildCount; i++)
            {
                var c = n.GetChild(i);
                if (c is Core.ArithmeticExpressionContext ae) return BindExprCore(ae, context);
                // ⛔ A FUNCTION CALL MUST BE CAUGHT HERE, BEFORE THE WALK CAN DESCEND INTO IT (fix-queue PB45).
                // This walk is breadth-first over the wrapper's subtree looking for something bindable, and a
                // functionCall CONTAINS a dataReference — its argument. Without this arm the walk fell through to
                // the arm below and bound `FUNCTION SQRT(W-Z)` as plain `W-Z`: `ADD FUNCTION SQRT(W-Z) TO W-R`
                // with W-Z = 4 added FOUR instead of TWO. Silent, and it survives "does it compile" entirely —
                // it was caught only by checking the VALUE against the spec-derived answer.
                if (c is Core.FunctionCallContext fc) return host.Intrinsic.BindIntrinsic(fc);
                if (c is Core.LiteralContext l) return NumLiteral(l);
                if (c is Core.DataReferenceContext d) return RefExpr(d, context);
                queue.Enqueue(c);
            }
        }
        // ⚠ A WRAPPER HOLDING NOTHING BINDABLE SILENTLY BECOMES ZERO, which is the same silent-wrong-answer shape
        // as the defect above: a new operand alternative that nobody adds an arm for degrades to `0` rather than
        // failing. Kept as a literal zero ONLY because an errored parse can reach here; every REACHABLE shape must
        // have an arm above, and ArithmeticSendingOperandDriftTests exists to keep the grammar from growing one
        // this walk cannot see.
        return new BoundNumLiteral("0");
    }
}
