// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;


namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ <b>THE REPORT SECTION'S COUNTERS AS REFERENCE OPERANDS</b> (DESIGN-architecture-review §8.3 (4), kb/Work PB2299): the
/// resolver half that turns a written name into a <see cref="ReportSumCounterPlace"/> (ISO §2023:13.18.54.4 GR5 — "the data-name
/// is the name of the sum counter, not the name of the associated printable item"; GR12 lets procedure division statements
/// alter it). A SUM counter is an implicitly defined VIEW over the report engine's counter, in no name table, so it is
/// resolved BEFORE ordinary name lookup like the CAPACITY register and EXCEPTION-OBJECT are (<see cref="ReferenceResolver"/>'s
/// identification step asks <see cref="SumCounterFor"/> after them).
/// <para>The other counters of the report writer — the LINAGE-COUNTER of the I-O control system (§2023:8.4.3.14) and a report's
/// LINE-COUNTER and PAGE-COUNTER (§2023:8.4.3.15) — are runtime-sourced registers, never a Place: the identification step stops
/// at them (<c>SpecialRegister</c>, <see cref="DeferredShape.UnroutedSpecialRegister"/>) and the statement binder routes
/// them to <c>BoundLinageCounterRef</c> / <c>BoundReportCounterRef</c>, so this type holds no arm for them.</para>
/// </summary>
internal sealed class CounterResolver(ReferenceResolver refs, DataBinder data)
{
    /// <summary>The <see cref="ReportSumCounterPlace"/> for a reference to a REPORT SECTION sum counter (ISO
    /// §2023:13.18.54.4 GR5 — the data-name written after the level number of an entry containing a SUM clause "is the
    /// name of the sum counter, not the name of the associated printable item"; GR12 — "It is permissible for
    /// procedure division statements to alter the content of sum counters"), or null when the name is not a sum
    /// counter. kb/Work PB840.
    /// <para>⛔ A SUM COUNTER HAS ITS REAL HIERARCHY (kb/Work PB1454). §2023:8.4.2.2.3 SR4 — "Each data-name-2 shall be the
    /// name associated with a level number to which the item being qualified is subordinate" — and a SUM entry is
    /// always subordinate to its 01 report group (and to any group entry between), so the covered spelling is
    /// <c>counter [ OF group-entry ]… [ OF report-name ]</c>: every named level above the entry, inner → outer
    /// with gaps allowed, the report-name last as §2023:8.4.2.2.2 Format 1's file-report-qualifier. The walk is the ONE
    /// <see cref="DataBinder.QualifierWalk"/> a data item's own ancestors go through, over
    /// <see cref="ReportSumModel.Qualification"/>. (The model it replaced — "a level-01-free item whose only
    /// available qualifier is its report" — was false, so `TOT OF CF1` was refused and two same-named counters
    /// of one report could not be told apart.) A reference-modified counter is a counter reference like any other
    /// and takes the ordinary reference-modification tail after its subscripts (kb/Work PB1943).</para>
    /// <para>⛔ A REPEATING ENTRY'S COUNTER IS A TABLE (kb/Work PB1271). Its occurrences are a
    /// <see cref="ReportSumFamily"/> whose register carries one OCCURS level per repetition vehicle, so the written
    /// subscripts go through the ONE <see cref="ReadSubscripts"/> — §2023:8.4.2.3.3 SR3's count, SR5's "Each table
    /// element reference shall be subscripted" (an unsubscripted reference to a multiple COLUMN counter used to
    /// alter all of its occurrences at once) and SR4's index-name association — and the place carries the rendered
    /// subscripts the engine turns into the occurrence's counter id at run time.</para>
    /// <para>⛔ TWO ENTRIES MAY LEGALLY CARRY ONE NAME (GR1, kb/Work PB882): §2023:8.4.2.2.1's uniqueness requirement
    /// is about a REFERENCE, so the collision is diagnosed HERE, where a reference exists, and never by dropping
    /// a declaration.</para></summary>
    internal RefResolution? SumCounterFor(Core.DataReferenceContext dref, string name, bool report)
    {
        if (!data.State.SumCounters.ContainsKey(name)) return null;
        var written = ReferenceResolver.ReadWritten(dref);
        if (SumCounterFamilyFor(dref, name, written.Qualifiers, report) is not ({ } rep, { } family)) return null;
        // §2023:8.4.3.3.3 SR3 — the count screen every identifier entry runs (ScreenRefModCount), asked of the counter too.
        if (!refs.RefMods.ScreenRefModCount(dref, written, name)) return RefResolution.Refused(DataBinder.WrittenText(dref));
        if (refs.Subscripts.ReadSubscripts(dref, family.Register, written.SubscriptGroup, out var indexExprs) is { } refusal)
            return refusal;
        var counter = SumCounterPlace(rep, family, indexExprs);
        if (written.RefModCount == 0) return RefResolution.Resolved(counter, "");
        // ⛔ A REFERENCE-MODIFIED COUNTER TAKES THE ONE REFERENCE-MODIFICATION TAIL (kb/Work PB1943), after its
        // subscripts like every item: §2023:8.4.3.3.3 SR1 admits "a numeric data item of usage display or national", and the
        // counter's register IS one (PicInfo.SumCounterItem — GR1 states no usage, so DISPLAY is the implementor's
        // determination, docs/CONFORMANCE.md A.4.11). The same admission (ReadScreenedRefMod) and the same view
        // (RefModView: the counter's DISPLAY character image, GR1's digits) as every other numeric item. This used
        // to return null here, which sent the reference to ordinary resolution, where a sum counter is not in
        // ByName: COBOL1639 "'CF-T(1:2)' is not defined", naming a rule the program had not broken.
        if (refs.RefMods.ReadScreenedRefMod(dref, written, counter.Item, out var refModRefusal) is not { } spec) return refModRefusal;
        return refs.RefMods.RefModView(counter.Item, counter, spec) is { } view
            ? RefResolution.Resolved(view, "")
            : RefResolution.Deferred(DeferredShape.NumericRefModSubstrate, DataBinder.WrittenText(dref));
    }

    /// <summary>The place of one counter occurrence of <paramref name="family"/>: its rendered one-based subscripts
    /// (outermost first; empty for a non-repeating entry, whose one counter is <see cref="ReportSumFamily.BaseId"/>).
    /// The ONE construction of a procedure-division sum counter place — the ordinary reference and the table(ALL)
    /// intrinsic argument (§2023:15.3) both build through it.</summary>
    internal ReportSumCounterPlace SumCounterPlace(ReportModel report, ReportSumFamily family, IReadOnlyList<Position> indexExprs) =>
        new(report.CsIndex, family.BaseId, family.Register, data.State.Reports.Depth(report), indexExprs);

    /// <summary>The CURRENT range of level <paramref name="level"/> (outermost first) of a repeating sum counter, for
    /// a table(ALL) argument (ISO §2023:15.3): an enclosing repetition with the DEPENDING phrase ranges over
    /// §2023:13.18.38.4 GR13's count ("the range of values is determined by the object of the OCCURS DEPENDING ON
    /// clause"); every other level over its extent. The twin of <see cref="CurrentOccurrenceCount"/>, whose data
    /// division OCCURS DEPENDING has GR7's clamp and EC-BOUND-ODO instead (kb/Work PB1271). Null when data-name-1
    /// cannot be addressed.</summary>
    internal AllCount? SumCounterOccurrenceCount(ReportSumFamily family, int level) =>
        level < family.Repetitions.Count && family.Repetitions[level] is { DependingItem: { } dn } spec
            ? (refs.Places.ResolveItem(dn) is { } depending ? new AllCount.ReportDepending(depending, spec.Min, spec.Max) : null)
            : new AllCount.Fixed(family.Extents[level]);

    /// <summary>The SUM ENTRY a reference names — <see cref="SumCounterFor"/>'s name-and-qualifier half, shared with
    /// the table(ALL) intrinsic argument so both ask it in the same order relative to ordinary lookup (a counter
    /// first, unless the qualifiers reach no counter and an ordinary item answers them). Null when the name is no
    /// counter's, when an ordinary item answers the qualifiers, or when the reference was diagnosed (no counter
    /// is subordinate to the qualifiers; two counters are; a counter and an ordinary data item of the same source
    /// element are — kb/Work PB3232, the counters and the ordinary items being ONE §2023:8.4.2.2.3 SR1 candidate set).</summary>
    internal (ReportModel Report, ReportSumFamily Family)? SumCounterFamilyFor(
        Core.DataReferenceContext dref, string name, List<string> qualifiers, bool report)
    {
        // §2023:13.18.53.3 SR4 — inside a report's own clauses the only sum counters a SOURCE operand may name are those "defined in
        // the current report", so a homonym of another report is no candidate: the reference is unambiguous by the rule
        // and never needs a qualifier to say so (kb/Work PB1292). §2023:8.4.6.2.1 rule 3 — a counter of a report this source
        // element declares hides a same-named counter of a container's GLOBAL report; only candidates of the NEAREST
        // declaring element can be ambiguous. Both are the ONE lookup's (ReportScope.SumCountersNamed).
        if (!data.State.SumCounters.ContainsKey(name)
            || data.State.Reports.SumCountersNamed(name, qualifiers, refs.EnclosingReport) is not { } matches) return null;
        if (matches.Count == 0)
        {
            // The qualifiers reach no counter of this name. When an ORDINARY item of the name does answer them
            // (`TOT OF WS-GROUP`, the counter's name reused as a data item), the reference is that item's and
            // resolution carries on; otherwise the name IS a sum counter and the ordinary resolver would only say
            // "not defined", so name the real rule (§2023:8.4.2.2.2 / §2023:8.4.2.2.3 SR4).
            if (refs.Names.ResolveQualified(name, qualifiers) is not null) return null;
            if (report && refs.Diagnosed.Add(dref))
                data.Edition.Error(DiagnosticCatalog.ReportSumCounterReference, $"'{DataBinder.WrittenText(dref)}': '{name}' is a sum counter "
                    + $"(ISO §2023:13.18.54.4 GR5), but none is subordinate to the qualifiers written — a sum counter is "
                    + "qualified by the data-names of the report group description entries above it, innermost "
                    + "first, and by the report-name last (ISO §2023:8.4.2.2.3 SR4, §2023:8.4.2.2.2 Format 1).");
            return null;
        }
        // §2023:8.4.6.2.1 3) b) ACROSS NAME CLASSES (kb/Work PB3154 review R3-1): a counter of a container's GLOBAL report
        // is a global name of THAT container (§2023:13.18.27.4 GR1), so a same-spelled name a NEARER source element
        // declares — a nearer container's GLOBAL data item, condition-name or index-name — is the referenced item, and
        // ordinary resolution takes it. (This element's own declarations never reach here: InheritGlobalReport keeps
        // a counter its own data hides out of SumCounters.) Every candidate left shares one report depth.
        int counterDepth = data.State.Reports.Depth(matches[0].Report);
        if (counterDepth > 0
            && (qualifiers.Count == 0
                ? data.DeclaredNearerThan(name, counterDepth)
                : refs.Names.ResolveQualified(name, qualifiers) is { } nearer && data.State.Forest.DeclaringDepth(nearer) < counterDepth))
            return null;
        // ⛔ THE COUNTER IS ONE CANDIDATE AMONG ALL THE DATA-NAMES OF ITS SPELLING (kb/Work PB3232). §2023:13.18.54.4 GR5 makes a
        // SUM entry's data-name the name of its sum counter, so the counter is a data-name like the ordinary data items
        // of the same source element, and §2023:8.4.2.2.3 SR1 — "For each non unique user-defined name that is explicitly
        // referenced, uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of
        // reference" — counts them TOGETHER. The ordinary items asked about are the ones the SAME written qualifiers
        // reach in the SAME source element as the counters (§2023:8.4.6.2.1 3): the one candidate set
        // DataBinder.QualifiedCandidates, which the ordinary lookup (ResolveUnqualified / ResolveQualified) reads too.
        // Asking only the counters, as this method did, let the counter win every collision with a WORKING-STORAGE item
        // that had no qualifier to tell them apart: a MOVE to the name altered the report total, silently.
        int sameSpelling = matches.Count + data.QualifiedCandidates(name, qualifiers, data.ActiveScope)
            .Where(item => data.State.Forest.DeclaringDepth(item) == counterDepth).Count;
        if (sameSpelling > 1)
        {
            if (report && refs.Diagnosed.Add(dref))
            {
                int ordinary = sameSpelling - matches.Count;
                data.Edition.Error(DiagnosticCatalog.ReportSumCounterReference, $"'{DataBinder.WrittenText(dref)}': the reference is ambiguous — "
                    + (ordinary == 0
                        ? $"{matches.Count} entries establish a sum counter named '{name}' (ISO §2023:13.18.54.4 GR1 gives each entry its own counter)."
                        : $"'{name}' is the name of {(matches.Count == 1 ? "a sum counter" : $"{matches.Count} sum counters")} "
                          + $"(ISO §2023:13.18.54.4 GR5) and of {(ordinary == 1 ? "a data item" : $"{ordinary} data items")} "
                          + "the written qualifiers also reach.")
                    + " A reference shall uniquely identify one resource (ISO §2023:8.4.2.2.1, §2023:8.4.2.2.3 SR1); qualify it by "
                    + "report group entry, group or report-name, or give the entries distinct data-names.");
            }
            return null;
        }
        return matches[0];
    }

    /// <summary>Resolve a LINAGE-COUNTER reference to its file (ISO §2023:8.4.3.14): in the grammar alternative
    /// <c>LINAGE_COUNTER ((OF|IN) cobolWord)?</c> the cobolWord IS the file-name qualifier. Unqualified, the
    /// register resolves only when exactly ONE file has a LINAGE clause — with several, qualification is
    /// required (§2023:8.4.3.14 SR3 / §2023:8.4.2.2). Null (the caller binds a loud error) for no/an ambiguous match,
    /// with a bind-time diagnostic naming the rule.</summary>
    internal FileModel? LinageFileOf(Core.DataReferenceContext dref)
    {
        // ⛔ §2023:8.4.3.14.3 SR1 — "LINAGE-COUNTER may be referenced only in procedure division statements". The report
        // section's clause expressions (SOURCE, PRESENT WHEN, SUM, VARYING) bind through THIS binder with the report in
        // scope, and this is the ONE producer of the LINAGE-COUNTER operand, so one test here refuses every such position
        // — present and future — by BINDING CONTEXT; before it, `SOURCE LINAGE-COUNTER + 1` and `PRESENT WHEN
        // LINAGE-COUNTER > 1` bound and read the live counter (kb/Work PB1431).
        if (refs.EnclosingReport is { } report)
        {
            DataBinder.ReportLinageCounterOutsideProcedure(data.Edition, dref, $"RD '{report.Name}': clause operand", DataBinder.WrittenText(dref));
            return null;
        }
        if (dref.cobolWord() is { } q)   // qualified: LINAGE-COUNTER OF/IN file-name
        {
            if (data.State.VisibleFiles.TryGetValue(q.GetText(), out var named) && named.Linage is not null) return named;
            data.Edition.Error(DiagnosticCatalog.FileKeyClauseRule, $"LINAGE-COUNTER OF '{q.GetText()}': the qualifier shall name a "
                + "file whose file description entry contains a LINAGE clause (ISO §2023:8.4.3.14 / §2023:13.18.34.4 GR7a)");
            return null;
        }
        // The VISIBLE set, not the program's own FD list (kb/Work PB123's sweep): VisibleFiles carries the
        // containers' GLOBAL FDs too (§2023:13.18.27.4 GR1), so a contained program whose only LINAGE file is the
        // container's GLOBAL one resolves the unqualified register instead of drawing COBOL0864; two
        // visible LINAGE files — own plus inherited — still require qualification (§2023:8.4.3.14 SR3).
        var linageFiles = data.State.VisibleFiles.Values.Where(f => f.Linage is not null).Distinct().ToList();
        if (linageFiles.Count == 1) return linageFiles[0];
        data.Edition.Error(DiagnosticCatalog.LinageCounterWithoutLinage, linageFiles.Count == 0
            ? "LINAGE-COUNTER referenced, but no file description entry contains a LINAGE clause (ISO §2023:8.4.3.14 — "
              + "the register is generated by the presence of a LINAGE clause)"
            : "unqualified LINAGE-COUNTER with more than one LINAGE file: qualify by file-name (ISO §2023:8.4.3.14 "
              + "SR3 / §2023:8.4.2.2 Qualification)");
        return null;
    }

    /// <summary>⛔ THE ONE RESOLUTION OF A COUNTER REFERENCE TO ITS REPORT, read by the SENDING
    /// (<c>ReportWriterBinder.CounterExpr</c>) and RECEIVING (<c>ReportWriterBinder.CounterPlace</c>) directions alike: the OF/IN
    /// <c>cobolWord</c> is the report-name qualifier (ISO §2023:8.4.3.15 SR2 / §2023:8.4.2.2), and an unqualified counter
    /// resolves only when the program has exactly one report. Null — with the diagnostic already raised — when
    /// the qualifier names no report, when no report description entry exists, or when an unqualified reference
    /// is ambiguous. Written once so a program cannot be told the qualification rule on one side of a statement
    /// and a different one on the other (kb/Work PB429).</summary>
    internal ReportModel? ReportOf(Core.DataReferenceContext dref, string reg)
    {
        if (dref.cobolWord() is { } q)   // qualified: COUNTER OF/IN report-name
        {
            if (data.State.Reports.Named(q.GetText()) is { } named) return named;
            data.Edition.Error(DiagnosticCatalog.ReportCounterQualification, $"{reg} OF '{q.GetText()}': the qualifier shall name a report "
                + $"description entry — a counter of a DIFFERENT report is referenced \"qualified explicitly by the report-name "
                + $"associated with the different report\" (ISO {CounterQualificationRule(reg)}; the qualification requirements "
                + "for the counters are those of §2023:8.4.2.2, §2023:8.4.3.15.3 SR2)");
            return null;
        }
        // §2023:8.4.2.2.3 SR9/SR10 — "In the report section, an unqualified reference to the [LINE-COUNTER | PAGE-COUNTER] is
        // qualified implicitly by the name of the report in whose report description entry the reference is made": the
        // enclosing RD answers, whatever number of reports the program has (kb/Work PB1456).
        if (refs.EnclosingReport is { } enclosing) return enclosing;
        // §2023:8.4.6.2.5 — the counters of a GLOBAL report are global; §2023:8.4.6.2.1 rule 3 — the nearest declaring
        // source element's reports hide a container's, so "exactly one report" is asked of the nearest ones.
        var nearest = data.State.Reports.NearestInScope(data.State.Reports.Visible, r => r);
        if (nearest.Count == 1) return nearest[0];
        data.Edition.Error(DiagnosticCatalog.ReportCounterQualification, nearest.Count == 0
            ? $"{reg} referenced, but the program has no report description entry (ISO §2023:8.4.3.15.1 — the "
              + "counters are generated per report)"
            : $"unqualified {reg} with more than one report description entry: {reg} shall be qualified each time it is "
              + $"referenced in the procedure division if more than one report description entry is specified in the "
              + $"source element (ISO {CounterQualificationRule(reg)})");
        return null;
    }

    /// <summary>The syntax rule that qualifies the register <paramref name="reg"/> — one rule per counter, written as
    /// the standard writes it (§2023:8.4.2.2.3 SR9 for LINE-COUNTER, SR10 for PAGE-COUNTER).</summary>
    private static string CounterQualificationRule(string reg) =>
        reg == "PAGE-COUNTER" ? "§2023:8.4.2.2.3 SR10" : "§2023:8.4.2.2.3 SR9";
}
