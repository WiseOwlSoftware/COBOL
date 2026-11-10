// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Procedure;

/// <summary>
/// ⛔ THE ONE MODEL OF THE COBOL-85 SEGMENTATION MODULE (kb/Work PB5084; VERSION_CHANGE_REFERENCE row 7.18; CONTROL_FLOW_DESIGN
/// D4). §1985:XVI.3.2.3 SR2: "If the segment-number is omitted from the section header, the segment-number is assumed to be
/// 0." §1985:XVI.3.2.4 GR3: "Segments with segment-number 50 through 99 are independent segments." The module exists only at
/// COBOL-85: from 2002 the segment-number is removed (COBOL0902), and --permissive, which accepts it with a warning, keeps
/// its run-time meaning without asking the module's syntax rules.
/// <para>Every reader asks here: the range screen (<c>VersionConformancePass.ParseArm.ScreenSegmentNumber</c>, COBOL6819),
/// the section's own number (<see cref="SectionInfo.Segment"/>), the restrictions on program flow of §1985:XVI.3.3
/// (<see cref="CheckAlter"/> and <see cref="CheckProcedures"/>, COBOL6877, asked where each statement binds, because
/// every section's number is collected before any statement binds) and the run-time initial state of an independent
/// segment (<c>AlterSwitchEmitter.EmitSegmentState</c>, over <see cref="ProcedureTableBuilder.SegmentsPerPc"/>); the fixed /
/// independent classification is the model's (<see cref="SegmentNumber"/>).</para>
/// </summary>
internal static class Segmentation
{
    /// <summary>The value a written segment-number literal denotes: 0 when it is omitted (§1985:XVI.3.2.3 SR2), and
    /// <see cref="int.MaxValue"/> for more than two significant digits (out of every range of the module, so the range
    /// screen refuses it and no larger number is parsed).</summary>
    public static int ValueOf(CobolParserCore.IntegerLiteralContext? number)
    {
        if (number is null) return 0;
        string digits = number.GetText().TrimStart('0');
        return digits.Length is 0 ? 0 : digits.Length > 2 ? int.MaxValue
            : int.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>The segment-number of a section header, at every edition: from 2002 a written number is refused (COBOL0902),
    /// and under --permissive the removed construct keeps the meaning COBOL-85 gives it (the migration mode), as ALTER does.</summary>
    public static int OfHeader(CobolParserCore.IntegerLiteralContext? number) => ValueOf(number);

    /// <summary>Are the module's syntax rules in force at the edition being compiled? Only where the segment-number is an
    /// available construct (COBOL-85), as for the range screen (COBOL6819); under --permissive at a later edition the number
    /// is a warned, removed construct whose run-time meaning is kept and whose 1985 restrictions are not asked.</summary>
    private static bool RulesInForce(BinderContext ctx) =>
        ConstructRegistry.IsAvailable(ctx.Edition.Edition, Constructs.SegmentNumbersRemoved2002);

    /// <summary>§1985:XVI.3.3.1: "A GO TO statement in a section whose segment-number is greater than or equal to 50 must not be
    /// referred to by an ALTER statement in a section with a different segment-number." An independent segment's GO TO is
    /// returned to its initial state when control re-enters the segment (§1985:XVI.1.4.3), so only an ALTER of its own
    /// segment may set it. Reports COBOL6877 at <paramref name="written"/> (the ALTER's procedure-name-1) and returns false
    /// when <paramref name="target"/> breaks the rule.</summary>
    public static bool CheckAlter(BinderContext ctx, ResolvedProcedure target, Antlr4.Runtime.ParserRuleContext written, string asWritten)
    {
        int goTo = target.Section?.Segment ?? 0, alter = ctx.CurrentSection?.Segment ?? 0;
        if (!SegmentNumber.IsIndependent(goTo) || goTo == alter || !RulesInForce(ctx)) return true;
        using (ctx.Edition.At(written))
            ctx.Edition.Error(DiagnosticCatalog.SegmentationFlowRestriction,
                $"ALTER '{asWritten}': the GO TO it refers to is in independent segment {goTo} and this ALTER statement is in "
                + $"segment {alter} — §1985:XVI.3.3.1: \"A GO TO statement in a section whose segment-number is greater than or "
                + "equal to 50 must not be referred to by an ALTER statement in a section with a different segment-number.\"");
        return false;
    }

    /// <summary>§1985:XVI.3.3.2 (PERFORM), XVI.3.3.3 (MERGE) and XVI.3.3.4 (SORT): the procedures a statement names lie
    /// "Totally within non-independent segments" or "Wholly contained in a single independent segment", the statement's own
    /// when it is itself in an independent segment ("Wholly within the same independent segment as that" statement).
    /// <paramref name="range"/> is the procedures the statement names (procedure-name-1 through procedure-name-2, either
    /// physical order): each PERFORM, SORT and MERGE that those procedures contain is asked about its own procedures, and
    /// the procedures they perform are not added to the outer statement's (docs/CONFORMANCE.md §3.8: the CCVS-85 segmentation
    /// programs SG102A and SG103A perform the fixed-portion report paragraphs from inside an independent segment's
    /// performed range). Reports COBOL6877 at the current position, citing <paramref name="rule"/>.</summary>
    public static void CheckProcedures(BinderContext ctx, PcRange range, string verb, string rule)
    {
        if (!RulesInForce(ctx)) return;
        var sections = ctx.Table.ParaSections;
        var spanned = range.SpannedPcs()
            .Select(pc => (uint)pc < (uint)sections.Count ? sections[pc]?.Segment ?? 0 : 0)
            .Distinct().Order().ToList();
        int from = ctx.CurrentSection?.Segment ?? 0;
        if (RangeAllowed(from, spanned)) return;
        ctx.Edition.Error(DiagnosticCatalog.SegmentationFlowRestriction,
            $"the procedures this {verb} statement in segment {from} names lie in segments {string.Join(", ", spanned)}: they "
            + "shall be \"Totally within non-independent segments\" or \"Wholly contained in a single independent segment\""
            + (SegmentNumber.IsIndependent(from) ? $", and for a {verb} statement in an independent segment that segment is its own, {from}" : "")
            + $" ({rule})");
    }

    /// <summary>The one rule of <see cref="CheckProcedures"/> over the segment-numbers <paramref name="spanned"/> of the
    /// procedures a statement in segment <paramref name="from"/> names.</summary>
    private static bool RangeAllowed(int from, IEnumerable<int> spanned)
    {
        int? independent = null;
        bool fixedPortion = false;
        foreach (int segment in spanned)
        {
            if (!SegmentNumber.IsIndependent(segment)) { fixedPortion = true; continue; }
            if (independent is { } other && other != segment) return false;   // two independent segments
            independent = segment;
        }
        if (independent is not { } only) return true;                          // totally non-independent (or empty)
        return !fixedPortion && (!SegmentNumber.IsIndependent(from) || only == from);
    }
}
