// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Editions;               // IDiagnosticSink / EditionDiagnostic / EditionSeverity
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog
using CobolNet.Frontend.Generated;     // CobolParserCore

namespace CobolNet.Validation;

using Core = CobolParserCore;

/// <summary>What one <c>integer-n</c> operand position of a general format admits.</summary>
internal enum IntegerSlotKind
{
    /// <summary>§5.5 1)'s default — "unsigned and nonzero". The unsigned half is the grammar's own shape
    /// (<c>integerLiteral : INTEGERLIT</c>); the nonzero half is this pass.</summary>
    NonZero,

    /// <summary>An associated rule "otherwise specifies": zero is permitted, and <see cref="IntegerSlot.Basis"/>
    /// names the rule that says so.</summary>
    ZeroPermitted,

    /// <summary>The grammar spells the operand with <c>integerLiteral</c>, but the standard does not print it as
    /// an <c>integer-n</c> of an ISO general format this pass governs — its range is its own rule, owned elsewhere
    /// (<see cref="IntegerSlot.Basis"/> says which and where).</summary>
    NotGoverned,
}

/// <summary>One operand position's classification, and the rule (or reason) that decides it.</summary>
internal readonly record struct IntegerSlot(IntegerSlotKind Kind, string Basis)
{
    internal static readonly IntegerSlot Default = new(IntegerSlotKind.NonZero, "ISO §5.5 1)");
    internal static IntegerSlot Zero(string basis) => new(IntegerSlotKind.ZeroPermitted, basis);
    internal static IntegerSlot Not(string reason) => new(IntegerSlotKind.NotGoverned, reason);
}

/// <summary>
/// ⛔ THE ONE SCREEN for ISO §5.5 1) — "When the term 'integer-n' (n = 1, 2, …) is used in a general format and
/// associated rules, it refers to a fixed-point integer literal that shall be unsigned and nonzero unless otherwise
/// specified in the associated rules." (kb/Work PB859.)
///
/// <para>The rule is GENERAL, so it is enforced generally: every <c>integerLiteral</c> in the parse tree is an
/// <c>integer-n</c> and is NONZERO by default, with no code per clause. What IS per clause is only the exception —
/// the handful of associated rules that expressly permit zero — and those live in ONE table,
/// <see cref="IntegerOperandRules.Slots"/>, keyed by the grammar rule that spells the operand, each entry citing
/// its rule. A new grammar rule that writes <c>integerLiteral</c> is screened automatically, and
/// <c>IntegerOperandSlotDriftTests</c> fails until it is classified, so the exception question is asked of every
/// new site rather than remembered.</para>
///
/// <para>Before this pass there was no site at all: <c>05 E PIC X OCCURS 0 TIMES.</c> was accepted, lowered, and
/// reached the user as two Roslyn CS0029 errors naming generated C#. It runs pre-bind beside
/// <see cref="LevelNumberPass"/> for the same reason: a pure syntax rule over the raw tree, and a zero that later
/// phases would otherwise turn into a cascade or a backend failure.</para>
/// </summary>
internal sealed class IntegerOperandPass(IDiagnosticSink sink) : CursorFollowingVisitor(sink)
{
    /// <summary>Screen every <c>integer-n</c> operand in the group's raw parse tree.</summary>
    internal static void Run(Core.CompilationUnitContext tree, IDiagnosticSink sink) =>
        new IntegerOperandPass(sink).VisitPositioned(tree);

    public override object? VisitIntegerLiteral(Core.IntegerLiteralContext ctx)
    {
        string text = ctx.GetText();
        if (text.Length > 0 && text.AsSpan().Trim('0').IsEmpty
            && IntegerOperandRules.Classify(ctx).Kind == IntegerSlotKind.NonZero)
        {
            string where = IntegerOperandRules.ConstructName(ctx);
            Sink.Report(new EditionDiagnostic(DiagnosticCatalog.IntegerOperandZero.Code, EditionSeverity.Error,
                DiagnosticCatalog.IntegerOperandZero.Id,
                $"{where}: the integer operand {text} shall be nonzero — ISO §5.5 1): an integer-n \"shall be unsigned "
                + "and nonzero unless otherwise specified in the associated rules\", and no rule of this format "
                + "permits zero here", where, DiagnosticCatalog.IntegerOperandZero.IsoSection));
        }
        return base.VisitChildren(ctx);
    }
}

/// <summary>The per-position exceptions to §5.5 1)'s NONZERO default, and nothing else (see
/// <see cref="IntegerOperandPass"/>).</summary>
internal static class IntegerOperandRules
{
    /// <summary>A classifier for the operands of ONE grammar rule: given the rule's context and the operand,
    /// the operand's slot.</summary>
    internal delegate IntegerSlot Classifier(ParserRuleContext owner, Core.IntegerLiteralContext operand);

    private static Classifier All(IntegerSlot slot) => (_, _) => slot;
    private static readonly Classifier Nonzero = All(IntegerSlot.Default);
    private static readonly Classifier ScreenDeclined = All(IntegerSlot.Not(
        "the SCREEN SECTION is a declined optional module (Annex A.4.2) refused whole by COBOLNET1560; its "
        + "clauses never bind, and a color integer may be zero"));

    /// <summary>⛔ EVERY grammar rule that writes <c>integerLiteral</c>, classified. Enumerated from the generated
    /// parser by <c>IntegerOperandSlotDriftTests</c> in both directions — a rule missing here, or a row naming a
    /// rule that no longer writes <c>integerLiteral</c>, is red. Only the ZERO-PERMITTED and NOT-GOVERNED rows
    /// carry a decision; a <see cref="Nonzero"/> row records that the rule was read and found silent.</summary>
    internal static readonly IReadOnlyDictionary<Type, Classifier> Slots = new Dictionary<Type, Classifier>
    {
        // ── IDENTIFICATION / ENVIRONMENT ────────────────────────────────────────────────────────────────────
        [typeof(Core.MemorySizeClauseContext)] = Nonzero,      // OBJECT-COMPUTER MEMORY SIZE integer (1985)
        [typeof(Core.SegmentLimitClauseContext)] = Nonzero,    // SEGMENT-LIMIT IS segment-number (1985)
        [typeof(Core.FileReserveClauseContext)] = Nonzero,     // §12.4.5.14 RESERVE integer-1 AREAS
        [typeof(Core.MultipleFileTapeEntryContext)] = Nonzero, // I-O-CONTROL MULTIPLE FILE … POSITION (1985)
        [typeof(Core.RerunEveryContext)] = Nonzero,            // I-O-CONTROL RERUN EVERY integer RECORDS (1985)
        [typeof(Core.SymbolicCharacterEntryContext)] = All(IntegerSlot.Not(
            "SYMBOLIC CHARACTERS integer-1 is an ORDINAL whose range, one through the size of the character set, is "
            + "§12.3.7.3 SR16's own rule, reported by DataBinder.SwitchBindSymbolic")),
        [typeof(Core.ChannelClauseContext)] = All(IntegerSlot.Not(
            "CHANNEL integer IS … is a vendor extension, not an ISO general format; §5.5 governs general formats")),
        [typeof(Core.ReserveClauseContext)] = All(IntegerSlot.Not(
            "SPECIAL-NAMES RESERVE integer CHANNELS is a vendor extension, not an ISO general format")),

        // ── PROCEDURE DIVISION HEADERS ──────────────────────────────────────────────────────────────────────
        [typeof(Core.SectionDefinitionContext)] = All(IntegerSlot.Not(
            "the section header's segment-number is the 1985 Segmentation module's priority-number (0 through 99), "
            + "not an integer-n; its range is that module's rule")),
        [typeof(Core.DeclarativeSectionContext)] = All(IntegerSlot.Not(
            "the declarative section header's segment-number is the 1985 Segmentation module's priority-number")),

        // ── DATA DIVISION ───────────────────────────────────────────────────────────────────────────────────
        [typeof(Core.BlockContainsClauseContext)] = Nonzero,   // §13.18.10 — SR1 orders them; nothing permits 0
        [typeof(Core.RecordClauseContext)] = RecordClause,
        [typeof(Core.LinageClauseContext)] = Nonzero,          // §13.18.34 integer-1
        [typeof(Core.LinageFootingPhraseContext)] = Nonzero,   // §13.18.34 integer-2
        [typeof(Core.LinageLinesAtTopPhraseContext)] = All(IntegerSlot.Zero("ISO §13.18.34.3 SR4")),
        [typeof(Core.LinageLinesAtBottomPhraseContext)] = All(IntegerSlot.Zero("ISO §13.18.34.3 SR4")),
        [typeof(Core.DynamicLengthClauseContext)] = Nonzero,   // §13.18.19 LIMIT IS integer-1
        [typeof(Core.PictureLocalePhraseContext)] = Nonzero,   // §13.18.40 PICTURE … LOCALE … SIZE integer-1
        [typeof(Core.OccursBoundContext)] = OccursBound,
        [typeof(Core.OccursStepPhraseContext)] = Nonzero,      // §13.18.38 Format 3 STEP integer-3
        [typeof(Core.OccursDynamicPhraseContext)] = OccursDynamic,

        // ── REPORT WRITER ───────────────────────────────────────────────────────────────────────────────────
        [typeof(Core.ReportPageClauseContext)] = Nonzero,      // §13.18.39.3 SR6 — "shall be greater than zero"
        [typeof(Core.ReportPageSubclauseContext)] = Nonzero,   // ditto
        [typeof(Core.ReportLineOperandContext)] = ReportLine,
        [typeof(Core.ReportNextGroupClauseContext)] = Nonzero, // §13.18.37 NEXT GROUP integer-1 / PLUS integer-2
        [typeof(Core.ReportColumnOperandContext)] = Nonzero,   // §13.18.14 Format 1 COLUMN integer-1 / PLUS

        // ── SCREEN SECTION (Annex A.4.2 — declined, refused whole by COBOLNET1560) ──────────────────────────
        [typeof(Core.ScreenLineClauseContext)] = ScreenDeclined,
        [typeof(Core.ScreenColumnClauseContext)] = ScreenDeclined,
        [typeof(Core.ScreenForegroundColorClauseContext)] = ScreenDeclined,
        [typeof(Core.ScreenBackgroundColorClauseContext)] = ScreenDeclined,
        [typeof(Core.ScreenPositionLegContext)] = ScreenDeclined,

        // ── STATEMENTS ──────────────────────────────────────────────────────────────────────────────────────
        // §14.9.28 PERFORM integer-1 TIMES: GR9's "equal to zero or is negative" speaks only of identifier-1's
        // run-time VALUE; no syntax rule otherwise-specifies integer-1, so the §5.5 default holds.
        [typeof(Core.PerformTimesContext)] = Nonzero,
        [typeof(Core.WriteBeforeAfterContext)] = All(IntegerSlot.Zero("ISO §14.9.51.3 SR15")),
    };

    /// <summary>§13.18.43.2's three formats: Format 1 <c>RECORD CONTAINS integer-1</c>; Format 2 <c>RECORD IS
    /// VARYING … [FROM integer-2] [TO integer-3]</c>; Format 3 <c>RECORD CONTAINS integer-4 TO integer-5</c>.
    /// Integer-2 (SR7) and integer-4 (SR8) "shall be greater than or equal to zero".</summary>
    private static IntegerSlot RecordClause(ParserRuleContext owner, Core.IntegerLiteralContext op)
    {
        var rc = (Core.RecordClauseContext)owner;
        if (PrecededBy(owner, op, Core.TO)) return IntegerSlot.Default;              // integer-3 / integer-5
        if (rc.VARYING() is not null) return IntegerSlot.Zero("ISO §13.18.43.3 SR7"); // integer-2
        return rc.TO() is not null ? IntegerSlot.Zero("ISO §13.18.43.3 SR8")         // integer-4
            : IntegerSlot.Default;                                                     // integer-1
    }

    /// <summary>§13.18.38 OCCURS: in <c>integer-1 TO integer-2</c> integer-1 "shall be greater than or equal to
    /// zero" (SR16); a lone bound is integer-2 and keeps the default.</summary>
    private static IntegerSlot OccursBound(ParserRuleContext owner, Core.IntegerLiteralContext op) =>
        owner.Parent is Core.OccursClauseContext oc && oc.TO() is not null && oc.occursBound(0) == owner
            ? IntegerSlot.Zero("ISO §13.18.38.3 SR16")
            : IntegerSlot.Default;

    /// <summary>§13.18.38 Format 4 (DYNAMIC): FROM integer-4 "shall be nonnegative" (SR28); TO integer-5 keeps the
    /// default.</summary>
    private static IntegerSlot OccursDynamic(ParserRuleContext owner, Core.IntegerLiteralContext op) =>
        ((Core.OccursDynamicPhraseContext)owner).FROM() is not null
            ? IntegerSlot.Zero("ISO §13.18.38.3 SR28")
            : IntegerSlot.Default;

    /// <summary>§13.18.35 LINE: integer-1 is an absolute line number (default); integer-2, written after PLUS, is a
    /// relative one and "may be zero" (SR3).</summary>
    private static IntegerSlot ReportLine(ParserRuleContext owner, Core.IntegerLiteralContext op) =>
        ((Core.ReportLineOperandContext)owner).reportRelativeSign() is not null
            ? IntegerSlot.Zero("ISO §13.18.35.3 SR3")
            : IntegerSlot.Default;

    /// <summary>The slot of <paramref name="operand"/>, from the grammar rule that spells it. A rule the table
    /// does not name takes §5.5 1)'s default — the drift test keeps that from being silent.</summary>
    internal static IntegerSlot Classify(Core.IntegerLiteralContext operand) =>
        operand.Parent is ParserRuleContext owner && Slots.TryGetValue(owner.GetType(), out var classify)
            ? classify(owner, operand)
            : IntegerSlot.Default;

    /// <summary>The construct named in the diagnostic: the source text of the clause or phrase that spells the
    /// operand (an OCCURS bound names its whole OCCURS clause).</summary>
    internal static string ConstructName(Core.IntegerLiteralContext operand)
    {
        var owner = operand.Parent as ParserRuleContext;
        if (owner is Core.OccursBoundContext) owner = owner.Parent as ParserRuleContext;
        if (owner is null) return "integer operand";
        string text = string.Join(' ', Tokens(owner));
        return text.Length <= 60 ? text : text[..57] + "...";
    }

    private static IEnumerable<string> Tokens(IParseTree node)
    {
        if (node is ITerminalNode t) { yield return t.GetText(); yield break; }
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var s in Tokens(node.GetChild(i))) yield return s;
    }

    private static bool PrecededBy(ParserRuleContext owner, IParseTree operand, int tokenType)
    {
        for (int i = 1; i < owner.ChildCount; i++)
            if (owner.GetChild(i) == operand)
                return owner.GetChild(i - 1) is ITerminalNode t && t.Symbol.Type == tokenType;
        return false;
    }
}
