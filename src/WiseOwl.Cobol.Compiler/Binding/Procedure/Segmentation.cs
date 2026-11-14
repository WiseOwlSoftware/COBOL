// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;

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
    /// screen refuses it, no larger number is parsed, and the flow rules skip it, <see cref="SegmentNumber.HasMeaning"/>). It is a section header's segment-number at every edition: from
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
        // A number out of the module's range is already COBOL6819 and classifies neither segment (SegmentNumber.HasMeaning).
        if (!SegmentNumber.HasMeaning(goTo) || !SegmentNumber.HasMeaning(alter)) return true;
        if (!SegmentNumber.IsIndependent(goTo) || goTo == alter || !MeaningKept(ctx.Edition.Edition)) return true;
        using (ctx.Edition.At(written))
            ctx.Edition.Error(DiagnosticCatalog.SegmentationFlowRestriction,
                $"ALTER '{asWritten}': the GO TO it refers to is in independent segment {goTo} and this ALTER statement is in "
                + $"segment {alter} — §1985:XVI.3.3.1: \"A GO TO statement in a section whose segment-number is greater than or "
                + "equal to 50 must not be referred to by an ALTER statement in a section with a different segment-number.\"");
        return false;
    }

    /// <summary>The one rule §1985:XVI.3.3.2 (PERFORM), XVI.3.3.3 (MERGE) and XVI.3.3.4 (SORT) each state: the procedures a
    /// statement's range holds lie wholly in non-independent segments or wholly in a single independent segment, the
    /// statement's own when it is itself in an independent segment. §1985:XVI.3.3.3: "(1) Totally within non-independent
    /// segments, or (2) Wholly contained in a single independent segment."
    /// <para>⛔ THE RANGE IS THE EXECUTED ONE, AND WHAT IS DIAGNOSED IS A CHOICE (kb/Work PB5442; docs/CONFORMANCE.md §3.8). "The
    /// range includes all statements that are executed as the result of a transfer of control by CALL, EXIT, GO TO, and
    /// PERFORM statements in the range of the PERFORM statement" (§1985:VI.6.21.4 GR11), and a program that breaks the rule
    /// has an undefined result (§1985:I.1.6), so the compiler chooses which violations it diagnoses: the procedures the
    /// statement names (<paramref name="range"/>, procedure-name-1 through procedure-name-2, either physical order) and
    /// what of them execution reaches by fall-through and GO TO (<see cref="ReachedPcs"/>). The procedures a PERFORM, SORT
    /// or MERGE inside the range transfers to are asked by that statement's own check, never added here (CCVS-85 SG102A and
    /// SG103A perform the fixed-portion report paragraphs from inside an independent segment's performed range). A
    /// section of the span that no path of the range reaches is not in the range, so it is not asked (s1: PERFORM A THRU C,
    /// A in segment 60 going to C in 60 over B in 10, is legal). The walk runs only when the physical span breaks the rule,
    /// and it only narrows that span.</para>
    /// Reports COBOL6877 at the current position, citing <paramref name="rule"/>.</summary>
    public static void CheckProcedures(BinderContext ctx, PcRange range, string verb, string rule)
    {
        if (!RulesInForce(ctx)) return;
        int from = ctx.CurrentSection?.Segment ?? 0;
        if (!SegmentNumber.HasMeaning(from) || RangeAllowed(from, SegmentNumbersOf(ctx, range.SpannedPcs()))) return;
        var reached = SegmentNumbersOf(ctx, ReachedPcs(ctx, range));
        if (RangeAllowed(from, reached)) return;
        ctx.Edition.Error(DiagnosticCatalog.SegmentationFlowRestriction,
            $"the procedures this {verb} statement in segment {from} names lie in segments {string.Join(", ", reached)}: they "
            + "shall lie wholly in non-independent segments or wholly in one independent segment"
            + (SegmentNumber.IsIndependent(from) ? $", which for a {verb} statement in an independent segment is its own, {from}" : "")
            + $" ({rule})");
    }

    /// <summary>The distinct meaningful segment-numbers of the sections owning <paramref name="pcs"/>, lowest first (a number out
    /// of range is already COBOL6819 and lies in no segment).</summary>
    private static List<int> SegmentNumbersOf(BinderContext ctx, IEnumerable<int> pcs)
    {
        var sections = ctx.Table.ParaSections;
        return pcs.Select(pc => (uint)pc < (uint)sections.Count ? sections[pc]?.Segment ?? 0 : 0)
            .Where(SegmentNumber.HasMeaning).Distinct().Order().ToList();
    }

    /// <summary>The paragraphs of <paramref name="range"/>'s span that execution of the range reaches from its first paragraph:
    /// by fall-through to the next paragraph of the span, unless a paragraph's last statement is a GO TO without the DEPENDING
    /// phrase or the paragraph ends the range, and by every GO TO in a reached paragraph (each procedure-name of a DEPENDING
    /// one too) whose target lies in the span. An alterable GO TO also reaches each procedure an ALTER statement of the
    /// program names for it (§1985:VI.6.7.4 GR1). PERFORM, SORT and MERGE ranges are their own statements' (see
    /// <see cref="CheckProcedures"/>). The procedure-names resolve as the GO TO resolves them, implicitly qualified by the
    /// paragraph's own section, through the quiet resolution: each GO TO's own bind
    /// reports a name that resolves to nothing.</summary>
    private static IEnumerable<int> ReachedPcs(BinderContext ctx, PcRange range)
    {
        if (range.IsEmpty) return [];
        var paragraphs = ctx.Table.Paragraphs;
        var saved = ctx.CurrentSection;
        Dictionary<int, List<int>>? altered = null;
        var reached = new HashSet<int>();
        var work = new Stack<int>();
        void Reach(int pc)
        {
            if (range.Spans(pc) && (uint)pc < (uint)paragraphs.Count && reached.Add(pc)) work.Push(pc);
        }
        try
        {
            Reach(range.Start);
            while (work.TryPop(out int pc))
            {
                ctx.CurrentSection = ctx.Table.ParaSections[pc];
                var sentences = paragraphs[pc].Sentences;
                foreach (var goTo in sentences.SelectMany(ParseTreeWalk.SelfAndDescendants).OfType<CobolParserCore.GoToStatementContext>())
                {
                    foreach (var name in goTo.procedureName())
                        if (ctx.Table.ResolveProcedureQuiet(name) is { Range.IsEmpty: false } target) Reach(target.Range.Start);
                    if (GoToFormats.Of(goTo) is not GoToFormat.Depending)
                        foreach (int destination in (altered ??= AlterDestinations(ctx)).GetValueOrDefault(pc) ?? [])
                            Reach(destination);
                }
                bool transfersAway = sentences.Length > 0 && sentences[^1].statement() is { Length: > 0 } last
                    && last[^1].goToStatement() is { } final && GoToFormats.Of(final) is not GoToFormat.Depending;
                if (!transfersAway && pc != range.End) Reach(pc + 1);
            }
        }
        finally { ctx.CurrentSection = saved; }
        return reached;
    }

    /// <summary>For each paragraph an ALTER statement of the program names as procedure-name-1, the first paragraph of every
    /// procedure-name-2 it is altered to proceed to (§1985:VI.6.7.4 GR1: "Execution of the ALTER statement modifies the GO TO
    /// statement in the paragraph named procedure-name-1 so that subsequent executions of the modified GO TO statement
    /// cause transfer of control to procedure-name-2"). Read off the text, as the ALTER switch prescan reads it.</summary>
    private static Dictionary<int, List<int>> AlterDestinations(BinderContext ctx)
    {
        var map = new Dictionary<int, List<int>>();
        var paragraphs = ctx.Table.Paragraphs;
        for (int pc = 0; pc < paragraphs.Count; pc++)
        {
            ctx.CurrentSection = ctx.Table.ParaSections[pc];
            foreach (var entry in paragraphs[pc].Sentences.SelectMany(ParseTreeWalk.SelfAndDescendants)
                         .OfType<CobolParserCore.AlterStatementContext>().SelectMany(a => a.alterEntry()))
                if (entry.procedureName() is { Length: >= 2 } names
                    && ctx.Table.ResolveProcedureQuiet(names[0]) is { IsSection: false, Range.IsEmpty: false } altered
                    && ctx.Table.ResolveProcedureQuiet(names[1]) is { Range.IsEmpty: false } destination)
                {
                    if (!map.TryGetValue(altered.Range.Start, out var list)) map[altered.Range.Start] = list = [];
                    list.Add(destination.Range.Start);
                }
        }
        return map;
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
