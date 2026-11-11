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
/// its run-time meaning and the rules that keep that meaning well-defined (<see cref="MeaningKept"/>: the number's range and
/// <see cref="CheckAlter"/>), not the PERFORM / SORT / MERGE syntax rules of <see cref="CheckProcedures"/>.
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
    /// screen refuses it and no larger number is parsed). It is a section header's segment-number at every edition: from
    /// 2002 a written number is refused (COBOL0902), and under --permissive the removed construct keeps the meaning COBOL-85
    /// gives it (the migration mode), as ALTER does.</summary>
    public static int ValueOf(CobolParserCore.IntegerLiteralContext? number)
    {
        if (number is null) return 0;
        string digits = number.GetText().TrimStart('0');
        return digits.Length is 0 ? 0 : digits.Length > 2 ? int.MaxValue
            : int.Parse(digits, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>⛔ DOES A WRITTEN SECTION SEGMENT-NUMBER KEEP ITS COBOL-85 MEANING at <paramref name="edition"/>? At COBOL-85 it
    /// is the module's priority number; from 2002 it is a removed construct (COBOL0902), an error, except under --permissive,
    /// the migration mode, where it is a warning and the program runs with the meaning COBOL-85 gives it (an independent
    /// segment's initial state, <c>AlterSwitchEmitter.EmitSegmentState</c>). The rules that make that meaning well-defined
    /// are asked wherever it is kept (kb/Work PB5223, PB5224): the number's range (§1985:XVI.3.2.3 SR1 and SR3, COBOL6819) and
    /// the ALTER restriction (§1985:XVI.3.3.1, <see cref="CheckAlter"/>). A number outside 0 through 99 has no meaning to keep,
    /// and an ALTER the initial-state reset would silently undo is not a program COBOL-85 defines. Strict 2002 and later
    /// refuse the number outright, so asking again would only repeat the refusal.</summary>
    public static bool MeaningKept(EditionInfo edition) =>
        edition.Permissive || ConstructRegistry.IsAvailable(edition, Constructs.SegmentNumbersRemoved2002);

    /// <summary>Are the module's PERFORM / SORT / MERGE syntax rules (§1985:XVI.3.3.2-4, <see cref="CheckProcedures"/>) in
    /// force at the edition being compiled? Only where the segment-number is an available construct (COBOL-85): they restrict
    /// what a program may write and change nothing a kept meaning runs, so --permissive does not ask them.</summary>
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
        if (!SegmentNumber.IsIndependent(goTo) || goTo == alter || !MeaningKept(ctx.Edition.Edition)) return true;
        using (ctx.Edition.At(written))
            ctx.Edition.Error(DiagnosticCatalog.SegmentationFlowRestriction,
                $"ALTER '{asWritten}': the GO TO it refers to is in independent segment {goTo} and this ALTER statement is in "
                + $"segment {alter} — §1985:XVI.3.3.1: \"A GO TO statement in a section whose segment-number is greater than or "
                + "equal to 50 must not be referred to by an ALTER statement in a section with a different segment-number.\"");
        return false;
    }

    /// <summary>The one rule §1985:XVI.3.3.2 (PERFORM), XVI.3.3.3 (MERGE) and XVI.3.3.4 (SORT) each state: the procedures a
    /// statement names lie wholly in non-independent segments or wholly in a single independent segment, the statement's
    /// own when it is itself in an independent segment. §1985:XVI.3.3.3: "(1) Totally within non-independent segments, or
    /// (2) Wholly contained in a single independent segment."
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
            + "shall lie wholly in non-independent segments or wholly in one independent segment"
            + (SegmentNumber.IsIndependent(from) ? $", which for a {verb} statement in an independent segment is its own, {from}" : "")
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
